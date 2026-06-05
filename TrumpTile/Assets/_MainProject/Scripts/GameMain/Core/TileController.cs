using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;
namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 개별 타일 제어
	///
	/// [변경사항]
	/// 1. Sorting Order 로직 수정 - 레이어 기반으로 통일
	/// 2. 레이어 높을수록 앞에 표시 (Layer 4 = 500, Layer 0 = 420)
	/// </summary>
	public enum ESpawnAnimType
	{
		PopIn,
		FadeIn,
		DropDown,
		FlipIn,
		BounceIn,
		Random
	}

	[RequireComponent(typeof(SpriteRenderer))]
	[RequireComponent(typeof(BoxCollider2D))]
	public class TileController : MonoBehaviour
	{
		#region Serialized Fields

		[Header("Tile Data")]
		[SerializeField] private TileData mTileData;

		[Header("Visual")]
		[SerializeField] private SpriteRenderer mBackgroundRenderer;
		[SerializeField] private SpriteRenderer mSpriteRenderer;
		[SerializeField] private SpriteRenderer mHighlightRenderer;
		[SerializeField] private ParticleSystem mSelectParticle;
		[SerializeField] private GameObject mBlockedOverlay;
		[SerializeField] private GameObject mFrozenOverlay;
		[SerializeField] private GameObject mLockedOverlay;

		[Header("Animation Settings")]
		[SerializeField] private float mMoveSpeed = 15F;
		[SerializeField] private float mShakeDuration = 0.15F;
		[SerializeField] private float mShakeIntensity = 0.05F;
		[SerializeField] private float mFlyArcHeight = 0.3F;
		[SerializeField] private float mFlyRotation = 360F;

		[Header("Spawn Animation")]
		[SerializeField] private float mSpawnDuration = 0.25F;

		[Header("Debug Info")]
		[SerializeField] private int mGridX;
		[SerializeField] private int mGridY;
		[SerializeField] private int mLayerIndex;
		[SerializeField] private int mCurrentSortingOrder;
		[SerializeField] private bool mIsSelectable = true;
		[SerializeField] private bool mIsInSlot;
		[SerializeField] private int mSlotIndex = -1;
		[SerializeField] private bool mIsFrozen;
		[SerializeField] private int mFrozenCount;
		[SerializeField] private bool mIsLocked;
		[SerializeField] private bool mIsAnimating;

		#endregion

		#region Sorting Constants

		private const int BASE_SORTING_ORDER = 420;
		private const int SORTING_STEP = 20;
		private const int SLOT_BASE_SORTING = 1000;
		private const int SLOT_SORTING_STEP = 10;
		private const int MOVING_SORTING_ORDER = 1099;
		public const int SPAWN_ANIM_COUNT = 5;

		#endregion

		#region Private Fields

		private Coroutine mCurrentAnimation;
		private Sequence mActiveTween;
		private BoxCollider2D mBoxCollider;
		private Vector3 mOriginalScale = Vector3.one;
		private Vector3 mSpawnTargetPos;
		private bool mbIsJewerly;
		private bool mbIsShake;
		private bool mbIsArrivedSlot;
		private enum EFinalScaleMode
		{
			InSlot,
			OnBoard,
		}

		#endregion

		#region Properties

		public TileData Data => mTileData;
		public string TileTypeId => mTileData != null ? mTileData.tileTypeId : "";
		public int GridX => mGridX;
		public int GridY => mGridY;
		public int LayerIndex => mLayerIndex;
		public int Layer => mLayerIndex;
		public bool IsSelectable => mIsSelectable && !mIsLocked && !mIsAnimating && !mIsInSlot;
		public bool IsInSlot => mIsInSlot;
		public int SlotIndex => mSlotIndex;
		public bool IsFrozen => mIsFrozen;
		public bool IsLocked => mIsLocked;
		public bool IsAnimating => mIsAnimating;
		public bool IsArrivedSlot {get => mbIsArrivedSlot; set => mbIsArrivedSlot = value;}

		#endregion

		#region Events

		public event Action<TileController> OnTileClicked;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (mSpriteRenderer == null)
			{
				mSpriteRenderer = transform.Find("Icon").GetComponent<SpriteRenderer>();
			}
			mBackgroundRenderer = GetComponent<SpriteRenderer>();
			mBoxCollider = GetComponent<BoxCollider2D>();
			//mSelectParticle = 
		}

		private void OnDestroy()
		{
			StopAllCoroutines();
			OnTileClicked = null;
		}

		#endregion

		#region Initialize

		//public void Initialize(TileData data, int layer)
		//{
		//	Initialize(data, 0, 0, layer);
		//}

		public void Initialize(TileData data, float x, float y, int layer, Sprite tileBackground)
		{
			mTileData = data;
			mGridX = (int)x;
			mGridY = (int)y;
			mLayerIndex = layer;

			mIsSelectable = true;
			mIsInSlot = false;
			mSlotIndex = -1;
			mIsFrozen = false;
			mFrozenCount = 0;
			mIsLocked = false;
			mIsAnimating = false;

			SetupVisual(tileBackground);
			UpdateSortingOrder();
			SetOverlaysActive(false);
			EnableCollider(true);

			mOriginalScale = transform.localScale;
			mSpawnTargetPos = transform.position;
		}

		private void SetupVisual(Sprite tileBackground)
		{
			if (mSpriteRenderer != null && mTileData != null)
			{
				mBackgroundRenderer.sprite = tileBackground;
				mSpriteRenderer.sprite = mTileData.sprite;
				mSpriteRenderer.color = Color.white;
			}

			transform.rotation = Quaternion.identity;
		}

		private void SetOverlaysActive(bool active)
		{
			if (mBlockedOverlay != null)
			{
				mBlockedOverlay.SetActive(active);
			}
			if (mFrozenOverlay != null)
			{
				mFrozenOverlay.SetActive(false);
			}
			if (mLockedOverlay != null)
			{
				mLockedOverlay.SetActive(false);
			}
		}

		#endregion

		#region Sorting

		public void UpdateSortingOrder()
		{
			if (mSpriteRenderer == null)
			{
				return;
			}

			if (mIsInSlot)
			{
				mCurrentSortingOrder = SLOT_BASE_SORTING + (mSlotIndex * SLOT_SORTING_STEP);
			}
			else
			{
				mCurrentSortingOrder = BASE_SORTING_ORDER + (mLayerIndex * SORTING_STEP);
			}

			mBackgroundRenderer.sortingOrder = mCurrentSortingOrder;

			SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer sr in childRenderers)
			{
				if (sr != mBackgroundRenderer)
				{
					sr.sortingOrder = mCurrentSortingOrder + 1;
				}
			}
		}

		private void SetMovingToSlotSorting()
		{
			mCurrentSortingOrder = MOVING_SORTING_ORDER;

			if (mBackgroundRenderer != null)
			{
				mBackgroundRenderer.sortingOrder = mCurrentSortingOrder;
			}

			SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer sr in childRenderers)
			{
				if (sr != mBackgroundRenderer)
				{
					sr.sortingOrder = mCurrentSortingOrder + 1;
				}
			}
		}

		public static int GetLayerSortingOrder(int layer)
		{
			return BASE_SORTING_ORDER + (layer * SORTING_STEP);
		}

		#endregion

		#region Input

		private void OnMouseDown()
		{		
			if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.EGameState.Playing)
			{
				return;
			}

			if (IsTouchBlockedByUI())
			{
				return;
			}

			if (mIsAnimating)
			{
				return;
			}

			if (mIsInSlot)
			{
				return;
			}

			if (!IsSelectable)
			{
				if(mbIsShake) return;
				PlayShakeAnimation();
				Debug.Log($"mIsSelectable : {mIsSelectable}\n mIsLocked : {mIsLocked}\n mIsAnimating : {mIsAnimating}\n mIsInSlot : {mIsInSlot}");
				AudioEvent.Play(EAudioKey.SFX_Reject);
				return;
			}

			if (mIsFrozen && mFrozenCount > 0)
			{
				BreakIce();
				return;
			}

			if (BoardManager.Instance != null && BoardManager.Instance.IsTileBlocked(this))
			{
				return;
			}

			HandleTileSelected();
		}

		private void HandleTileSelected()
		{
			EnableCollider(false);

			int savedX = mGridX;
			int savedY = mGridY;
			int savedLayer = mLayerIndex;

			if (BoardManager.Instance != null)
			{
				mIsInSlot = true;
				BoardManager.Instance.RemoveTileFromBoardBeforeAdd(this);
			}

			bool bAdded = SlotManager.Instance != null && SlotManager.Instance.AddTile(this);
			if (!bAdded)
			{
				if (BoardManager.Instance != null)
				{
					mIsInSlot = false;
					BoardManager.Instance.ReturnTileToBoard(this, savedX, savedY, savedLayer);
				}
				EnableCollider(true);
			}
		}

		private void OnMouseEnter()
		{
			if (mIsInSlot || !IsSelectable)
			{
				return;
			}
			SetHighlight(true);
		}

		private void OnMouseExit()
		{
			SetHighlight(false);
		}

		private void BreakIce()
		{
			mFrozenCount--;
			if (mFrozenCount <= 0)
			{
				mIsFrozen = false;
				if (mFrozenOverlay != null)
				{
					mFrozenOverlay.SetActive(false);
				}
			}
			AudioEvent.Play(EAudioKey.SFX_TileMove);
		}

		private bool IsTouchBlockedByUI()
		{
			return TrumpTile.GameMain.UI.PopupBase.IsAnyPopupOpen;
		}

		#endregion

		#region Visual State

		public void SetHighlight(bool active)
		{
			if (mHighlightRenderer != null)
			{
				mHighlightRenderer.enabled = active;
			}
		}

		public void SetSelectable(bool selectable)
		{
			mIsSelectable = selectable;

			if (mBlockedOverlay != null)
			{
				mBlockedOverlay.SetActive(!selectable && !mIsInSlot);
			}
		}

		public void SetFrozen(bool frozen, int count = 1)
		{
			mIsFrozen = frozen;
			mFrozenCount = count;

			if (mFrozenOverlay != null)
			{
				mFrozenOverlay.SetActive(frozen);
			}
		}

		public void SetLocked(bool locked)
		{
			mIsLocked = locked;

			if (mLockedOverlay != null)
			{
				mLockedOverlay.SetActive(locked);
			}
		}

		public void EnableCollider(bool enable)
		{
			if (mBoxCollider != null)
			{
				mBoxCollider.enabled = enable;
			}
		}

		#endregion

		#region Data
		public void SetTileDataOnly(TileData data)
		{
			mTileData = data;
			mbIsJewerly = true;
		}
		public void SetTileData(TileData data)
		{
			mTileData = data;

			if (mSpriteRenderer != null && mTileData != null)
			{
				mSpriteRenderer.sprite = mTileData.sprite;
			}
		}

		public void UpdateLayerInfo(int x, int y, int layer)
		{
			mGridX = x;
			mGridY = y;
			mLayerIndex = layer;
			UpdateSortingOrder();
		}

		#endregion

		#region Movement - To Slot
		public void MoveToSlot(Vector3 slotPosition, int index, Action onComplete = null)
		{
			//bool bWasInSlot = mIsInSlot;
	
			mSlotIndex = index;

			if (mBlockedOverlay != null)
			{
				mBlockedOverlay.SetActive(false);
			}

			SetMovingToSlotSorting();
			AudioEvent.Play(EAudioKey.SFX_TileMove);

			if (mSelectParticle != null)
			{
				mSelectParticle.Play();
			}

			StartMoveTween(
				slotPosition,
				bUseArc: true,//!bWasInSlot,
				EFinalScaleMode.InSlot,
				() =>
				{
					UpdateSortingOrder();
					onComplete?.Invoke();
				});

			if(mbIsJewerly)
			{
				mSpriteRenderer.sprite = mTileData.sprite;
			}
		}
		public void MoveToSlot(Vector3 slotPosition)
		{
			MoveToSlot(slotPosition, mSlotIndex, null);
		}

		public void AdjustSlotPosition(Vector3 newPosition, int newIndex, Action onComplete = null)
		{
			mSlotIndex = newIndex;
			UpdateSortingOrder();

			StartMoveTween(newPosition, bUseArc: false, EFinalScaleMode.InSlot, onComplete);
		}

		#endregion

		#region Movement - To Board

		private void InitBoardReturn()
		{
			mIsInSlot = false;
			mSlotIndex = -1;

			if (mBlockedOverlay != null)
			{
				mBlockedOverlay.SetActive(false);
			}

			UpdateSortingOrder();
			StopCurrentAnimation();
		}

		private void ReturnToBoard(Vector3 boardPosition)
		{
			InitBoardReturn();
			StartMoveTween(boardPosition, bUseArc: false, EFinalScaleMode.OnBoard, () => EnableCollider(true));
		}
		public void ReturnToBoard()
		{
			ReturnToBoard(mSpawnTargetPos);
		}
		public void ReturnToBoard(Vector3 boardPosition, int x, int y, int layer)
		{
			mGridX = x;
			mGridY = y;
			mLayerIndex = layer;

			ReturnToBoard(boardPosition);
		}
		public void FlyToBoard(Action onComplete = null)
		{
			InitBoardReturn();
			StartMoveTween(mSpawnTargetPos, bUseArc: true, EFinalScaleMode.OnBoard, () =>
			{
				mCurrentAnimation = StartCoroutine(BounceOnLandCoroutine(() => EnableCollider(true)));
			});
		}

		public void FlyToBoard(Vector3 boardPosition, int x, int y, int layer, Action onComplete = null)
		{
			mGridX = x;
			mGridY = y;
			mLayerIndex = layer;

			InitBoardReturn();
			StartMoveTween(boardPosition, bUseArc: true, EFinalScaleMode.OnBoard, () =>
			{
				mCurrentAnimation = StartCoroutine(BounceOnLandCoroutine(onComplete));
			});
		}

		#endregion

		#region Animation Coroutines

		private void KillActiveTween()
		{
			if (mActiveTween != null && mActiveTween.IsActive())
			{
				mActiveTween.Kill(false);
			}
			mActiveTween = null;
		}

		/// <summary>
		/// 보드↔슬롯 이동을 단일 DOTween 시퀀스로 처리한다.
		/// bUseArc == true 인 경우 아크 궤적 + 회전 + 펀치 스케일 적용 (보드→슬롯 첫 진입, 슬롯→보드 복귀).
		/// bUseArc == false 인 경우 스무스 직선 이동 (슬롯 내 정렬, 단순 복귀).
		/// </summary>
		private void StartMoveTween(Vector3 targetPosition, bool bUseArc, EFinalScaleMode finalScaleMode, Action onComplete)
		{
			StopCurrentAnimation();

			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = bUseArc
				? Mathf.Clamp(distance / mMoveSpeed, 0.2F, 0.35F)
				: Mathf.Clamp(distance / mMoveSpeed, 0.08F, 0.2F);

			transform.rotation = Quaternion.identity;
			mIsAnimating = true;

			Sequence seq = DOTween.Sequence();
			
			if (bUseArc)
			{
				float arcHeight = Mathf.Min(distance * mFlyArcHeight, 0.5F);
				Vector3 midPoint = (startPos + targetPosition) * 0.5F + Vector3.up * arcHeight;
				Vector3[] path = { startPos, midPoint, targetPosition };

				seq.Append(transform.DOPath(path, duration).SetEase(Ease.OutQuad));

				float rotationSign = UnityEngine.Random.Range(0, 2) == 0 ? 1F : -1F;
				seq.Join(transform.DOLocalRotate(new Vector3(0F, 0F, rotationSign * mFlyRotation), duration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
			}
			else
			{
				seq.Append(transform.DOMove(targetPosition, duration).SetEase(Ease.OutQuad));
			}

			seq.Join(BuildScalePunchTween(finalScaleMode, duration));

			Vector3 finalScale = ResolveFinalScale(finalScaleMode);
			
			seq.OnComplete(() =>
			{
				mIsAnimating = false;
				mActiveTween = null;
				onComplete?.Invoke();
			});

			mActiveTween = seq;
		}

		private Tween BuildScalePunchTween(EFinalScaleMode mode, float duration)
		{
			Sequence scaleSeq = DOTween.Sequence();

			if (mode == EFinalScaleMode.InSlot)
			{
				// 두근거림 없이 슬롯 크기로 부드럽게 축소
				scaleSeq.Append(transform.DOScale(Vector3.one * 0.6F, duration).SetEase(Ease.OutQuad));
			}
			else
			{
				float step = duration / 3F;
				scaleSeq.Append(transform.DOScale(Vector3.one * 0.6F, step));
				scaleSeq.Append(transform.DOScale(mOriginalScale * 0.9F, step));
				scaleSeq.Append(transform.DOScale(mOriginalScale, step));
			}

			return scaleSeq;
		}

		private Vector3 ResolveFinalScale(EFinalScaleMode mode)
		{
			if (mode == EFinalScaleMode.InSlot)
			{
				return Vector3.one * 0.6F;
			}
			return mOriginalScale;
		}

		private IEnumerator BounceOnLandCoroutine(Action onComplete = null)
		{
			mIsAnimating = true;

			float duration = 0.3F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float scale;

				if (t < 0.3F)
					scale = Mathf.Lerp(1.0F, 1.3F, t / 0.3F);
				else if (t < 0.7F)
					scale = Mathf.Lerp(1.3F, 0.9F, (t - 0.3F) / 0.4F);
				else
					scale = Mathf.Lerp(0.9F, 1.0F, (t - 0.7F) / 0.3F);

				transform.localScale = mOriginalScale * scale;
				yield return null;
			}

			transform.localScale = mOriginalScale;
			mIsAnimating = false;

			onComplete?.Invoke();
		}

		#endregion

		#region Other Animations

		public void PlayShakeAnimation()
		{
			if (mIsAnimating)
			{
				return;
			}
			mbIsShake = true;
			StartCoroutine(ShakeCoroutine());
		}

		private IEnumerator ShakeCoroutine()
		{
			Vector3 originalPos = transform.position;
			float elapsed = 0F;

			while (elapsed < mShakeDuration)
			{
				elapsed += Time.deltaTime;
				float progress = elapsed / mShakeDuration;
				float damping = 1F - progress;

				float offsetX = Mathf.Sin(elapsed * 60F) * mShakeIntensity * damping;
				float offsetY = Mathf.Cos(elapsed * 60F) * mShakeIntensity * damping * 0.5F;

				transform.position = originalPos + new Vector3(offsetX, offsetY, 0F);
				yield return null;
			}
			mbIsShake = false;
			transform.position = originalPos;
		}

		public void PlaySpawnAnimation(float delay = 0F, ESpawnAnimType animType = ESpawnAnimType.Random)
		{
			StartCoroutine(SpawnDispatchCoroutine(delay, animType));
		}

		private IEnumerator SpawnDispatchCoroutine(float delay, ESpawnAnimType animType)
		{
			ESpawnAnimType selectedType = animType;
			if (selectedType == ESpawnAnimType.Random)
			{
				selectedType = (ESpawnAnimType)UnityEngine.Random.Range(0, SPAWN_ANIM_COUNT);
			}

			PrepareSpawnVisual(selectedType);

			if (delay > 0F)
			{
				yield return new WaitForSeconds(delay);
			}

			mIsAnimating = true;

			switch (selectedType)
			{
				case ESpawnAnimType.PopIn:
					yield return PopInCoroutine();
					break;
				case ESpawnAnimType.FadeIn:
					yield return FadeInCoroutine();
					break;
				case ESpawnAnimType.DropDown:
					yield return DropDownCoroutine();
					break;
				case ESpawnAnimType.FlipIn:
					yield return FlipInCoroutine();
					break;
				case ESpawnAnimType.BounceIn:
					yield return BounceInCoroutine();
					break;
			}

			mIsAnimating = false;
		}

		private void PrepareSpawnVisual(ESpawnAnimType animType)
		{
			switch (animType)
			{
				case ESpawnAnimType.PopIn:
				case ESpawnAnimType.BounceIn:
					transform.localScale = Vector3.zero;
					break;
				case ESpawnAnimType.FadeIn:
					SetRenderersAlpha(0F);
					break;
				case ESpawnAnimType.DropDown:
					mSpawnTargetPos = transform.position;
					transform.position = mSpawnTargetPos + Vector3.up * 2F;
					SetRenderersAlpha(0F);
					break;
				case ESpawnAnimType.FlipIn:
					transform.localScale = new Vector3(0F, mOriginalScale.y, mOriginalScale.z);
					break;
			}
		}

		private void SetRenderersAlpha(float alpha)
		{
			if (mBackgroundRenderer != null)
			{
				Color c = mBackgroundRenderer.color;
				c.a = alpha;
				mBackgroundRenderer.color = c;
			}
			if (mSpriteRenderer != null)
			{
				Color c = mSpriteRenderer.color;
				c.a = alpha;
				mSpriteRenderer.color = c;
			}
		}

		private IEnumerator PopInCoroutine()
		{
			float elapsed = 0F;

			while (elapsed < mSpawnDuration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / mSpawnDuration);
				float scale = t < 0.7F
					? Mathf.Sin(t / 0.7F * Mathf.PI * 0.5F) * 1.1F
					: Mathf.Lerp(1.1F, 1.0F, (t - 0.7F) / 0.3F);
				transform.localScale = mOriginalScale * scale;
				yield return null;
			}

			transform.localScale = mOriginalScale;
		}

		private IEnumerator FadeInCoroutine()
		{
			float elapsed = 0F;

			while (elapsed < mSpawnDuration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / mSpawnDuration);
				SetRenderersAlpha(Mathf.Sin(t * Mathf.PI * 0.5F));
				yield return null;
			}

			SetRenderersAlpha(1F);
		}

		private IEnumerator DropDownCoroutine()
		{
			Vector3 startPos = transform.position;
			float duration = mSpawnDuration * 1.2F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				float easedT = 1F - Mathf.Pow(1F - t, 3F);

				transform.position = Vector3.Lerp(startPos, mSpawnTargetPos, easedT);
				SetRenderersAlpha(Mathf.Min(t * 4F, 1F));
				yield return null;
			}

			transform.position = mSpawnTargetPos;
			SetRenderersAlpha(1F);
		}

		private IEnumerator FlipInCoroutine()
		{
			float elapsed = 0F;

			while (elapsed < mSpawnDuration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / mSpawnDuration);
				float scaleX = Mathf.Sin(t * Mathf.PI * 0.5F) * mOriginalScale.x;
				transform.localScale = new Vector3(scaleX, mOriginalScale.y, mOriginalScale.z);
				yield return null;
			}

			transform.localScale = mOriginalScale;
		}

		private IEnumerator BounceInCoroutine()
		{
			float elapsed = 0F;

			while (elapsed < mSpawnDuration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / mSpawnDuration);
				float scale;

				if (t < 0.6F)
				{
					scale = Mathf.Sin(t / 0.6F * Mathf.PI * 0.5F) * 1.3F;
				}
				else if (t < 0.8F)
				{
					scale = Mathf.Lerp(1.3F, 0.85F, (t - 0.6F) / 0.2F);
				}
				else if (t < 0.9F)
				{
					scale = Mathf.Lerp(0.85F, 1.05F, (t - 0.8F) / 0.1F);
				}
				else
				{
					scale = Mathf.Lerp(1.05F, 1.0F, (t - 0.9F) / 0.1F);
				}

				transform.localScale = mOriginalScale * scale;
				yield return null;
			}

			transform.localScale = mOriginalScale;
		}

		public void PlayMatchAnimation(Action onComplete = null)
		{
			mIsSelectable = false;
			EnableCollider(false);
			StopCurrentAnimation();
			mCurrentAnimation = StartCoroutine(MatchCoroutine(onComplete));
		}

		private IEnumerator MatchCoroutine(Action onComplete)
		{
			mIsAnimating = true;

			Vector3 startScale = transform.localScale;
			float duration = 0.15F;
			float elapsed = 0F;

			while (elapsed < duration * 0.3F)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (duration * 0.3F);
				transform.localScale = startScale * (1F + t * 0.3F);
				yield return null;
			}

			elapsed = 0F;
			Color startColor = mSpriteRenderer != null ? mSpriteRenderer.color : Color.white;

			while (elapsed < duration * 0.7F)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (duration * 0.7F);
				transform.localScale = startScale * 1.3F * (1F - t);

				if (mSpriteRenderer != null)
				{
					Color newColor = startColor;
					newColor.a = 1F - t;
					mSpriteRenderer.color = newColor;
				}

				yield return null;
			}

			mIsAnimating = false;
			onComplete?.Invoke();
		}

		public void Remove()
		{
			StopCurrentAnimation();
			StartCoroutine(RemoveCoroutine());
		}

		private IEnumerator RemoveCoroutine()
		{
			mIsAnimating = true;
			EnableCollider(false);

			float duration = 0.1F;
			float elapsed = 0F;
			Vector3 startScale = transform.localScale;
			Color startColor = mSpriteRenderer != null ? mSpriteRenderer.color : Color.white;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				transform.localScale = startScale * (1F - t);

				if (mSpriteRenderer != null)
				{
					Color newColor = startColor;
					newColor.a = 1F - t;
					mSpriteRenderer.color = newColor;
				}

				yield return null;
			}

			Destroy(gameObject);
		}

		#endregion

		#region Utility

		public void StopAnimation()
		{
			StopCurrentAnimation();
		}

		private void StopCurrentAnimation()
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
				mCurrentAnimation = null;
			}
			KillActiveTween();
			mIsAnimating = false;
		}

		public void ShowHint()
		{
			StartCoroutine(HintCoroutine());
		}

		private IEnumerator HintCoroutine()
		{
			if (mSpriteRenderer == null)
			{
				yield break;
			}

			Color originalColor = mSpriteRenderer.color;

			for (int i = 0; i < 3; i++)
			{
				mSpriteRenderer.color = Color.yellow;
				yield return new WaitForSeconds(0.1F);
				mSpriteRenderer.color = originalColor;
				yield return new WaitForSeconds(0.1F);
			}
		}

		#endregion
	}
}
