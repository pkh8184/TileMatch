using UnityEngine;
using System;
using System.Collections;
namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 개별 타일 제어
	///
	/// [변경사항]
	/// 1. Sorting Order 로직 수정 - 레이어 기반으로 통일
	/// 2. 레이어 높을수록 앞에 표시 (Layer 4 = 500, Layer 0 = 420)
	/// </summary>
	[RequireComponent(typeof(SpriteRenderer))]
	[RequireComponent(typeof(BoxCollider2D))]
	public class TileController : MonoBehaviour
	{
		#region Serialized Fields

		[Header("Tile Data")]
		[SerializeField] private TileData mTileData;

		[Header("Visual")]
		[SerializeField] private SpriteRenderer mSpriteRenderer;
		[SerializeField] private SpriteRenderer mHighlightRenderer;
		[SerializeField] private GameObject mBlockedOverlay;
		[SerializeField] private GameObject mFrozenOverlay;
		[SerializeField] private GameObject mLockedOverlay;

		[Header("Animation Settings")]
		[SerializeField] private float mMoveSpeed = 20F;
		[SerializeField] private float mShakeDuration = 0.15F;
		[SerializeField] private float mShakeIntensity = 0.05F;
		[SerializeField] private float mFlyArcHeight = 0.3F;
		[SerializeField] private float mFlyRotation = 360F;

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

		#endregion

		#region Private Fields

		private Coroutine mCurrentAnimation;
		private BoxCollider2D mBoxCollider;
		private Vector3 mOriginalScale = Vector3.one;

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

		#endregion

		#region Events

		public event Action<TileController> OnTileClicked;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (mSpriteRenderer == null)
				mSpriteRenderer = GetComponent<SpriteRenderer>();

			mBoxCollider = GetComponent<BoxCollider2D>();
			mOriginalScale = transform.localScale;
		}

		private void OnDestroy()
		{
			StopAllCoroutines();
			OnTileClicked = null;
		}

		#endregion

		#region Initialize

		public void Initialize(TileData data, int layer)
		{
			Initialize(data, 0, 0, layer);
		}

		public void Initialize(TileData data, int x, int y, int layer)
		{
			mTileData = data;
			mGridX = x;
			mGridY = y;
			mLayerIndex = layer;

			mIsSelectable = true;
			mIsInSlot = false;
			mSlotIndex = -1;
			mIsFrozen = false;
			mFrozenCount = 0;
			mIsLocked = false;
			mIsAnimating = false;

			SetupVisual();
			UpdateSortingOrder();
			SetOverlaysActive(false);
			EnableCollider(true);
		}

		private void SetupVisual()
		{
			if (mSpriteRenderer != null && mTileData != null)
			{
				mSpriteRenderer.sprite = mTileData.sprite;
				mSpriteRenderer.color = Color.white;
			}

			transform.rotation = Quaternion.identity;
		}

		private void SetOverlaysActive(bool active)
		{
			if (mBlockedOverlay != null) mBlockedOverlay.SetActive(active);
			if (mFrozenOverlay != null) mFrozenOverlay.SetActive(false);
			if (mLockedOverlay != null) mLockedOverlay.SetActive(false);
		}

		#endregion

		#region Sorting

		public void UpdateSortingOrder()
		{
			if (mSpriteRenderer == null) return;

			if (mIsInSlot)
			{
				mCurrentSortingOrder = SLOT_BASE_SORTING + (mSlotIndex * SLOT_SORTING_STEP);
			}
			else
			{
				mCurrentSortingOrder = BASE_SORTING_ORDER + (mLayerIndex * SORTING_STEP);
			}

			mSpriteRenderer.sortingOrder = mCurrentSortingOrder;

			SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer sr in childRenderers)
			{
				if (sr != mSpriteRenderer)
				{
					sr.sortingOrder = mCurrentSortingOrder + 1;
				}
			}
		}

		private void SetMovingToSlotSorting()
		{
			if (mSpriteRenderer != null)
			{
				mCurrentSortingOrder = MOVING_SORTING_ORDER;
				mSpriteRenderer.sortingOrder = mCurrentSortingOrder;
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
			if (mIsAnimating) return;

			if (mIsInSlot)
			{
				PlayShakeAnimation();
				return;
			}

			if (!IsSelectable) return;

			if (mIsFrozen && mFrozenCount > 0)
			{
				BreakIce();
				return;
			}

			if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.EGameState.Playing)
				return;

			if (BoardManager.Instance != null && BoardManager.Instance.IsTileBlocked(this))
				return;

			HandleTileSelected();
		}

		private void HandleTileSelected()
		{
			BoardManager.Instance?.RemoveTileFromBoard(this);
			SlotManager.Instance?.AddTile(this);
		}

		private void OnMouseEnter()
		{
			if (mIsInSlot || !IsSelectable) return;
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
				if (mFrozenOverlay != null) mFrozenOverlay.SetActive(false);
			}
			AudioManager.Instance?.PlayTileSelect();
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
				mFrozenOverlay.SetActive(frozen);
		}

		public void SetLocked(bool locked)
		{
			mIsLocked = locked;

			if (mLockedOverlay != null)
				mLockedOverlay.SetActive(locked);
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
			bool bWasInSlot = mIsInSlot;

			mIsInSlot = true;
			mSlotIndex = index;

			if (mBlockedOverlay != null) mBlockedOverlay.SetActive(false);

			SetMovingToSlotSorting();
			AudioManager.Instance?.PlayTileMove();
			StopCurrentAnimation();

			if (bWasInSlot)
			{
				mCurrentAnimation = StartCoroutine(MoveToPositionCoroutine(slotPosition, () =>
				{
					UpdateSortingOrder();
					onComplete?.Invoke();
				}));
			}
			else
			{
				mCurrentAnimation = StartCoroutine(FlyToSlotCoroutine(slotPosition, () =>
				{
					UpdateSortingOrder();
					onComplete?.Invoke();
				}));
			}
		}

		public void MoveToSlot(Vector3 slotPosition)
		{
			MoveToSlot(slotPosition, mSlotIndex, null);
		}

		public void AdjustSlotPosition(Vector3 newPosition, int newIndex)
		{
			mSlotIndex = newIndex;
			UpdateSortingOrder();

			StopCurrentAnimation();
			mCurrentAnimation = StartCoroutine(MoveToPositionCoroutine(newPosition, null));
		}

		#endregion

		#region Movement - To Board

		public void ReturnToBoard(Vector3 boardPosition)
		{
			mIsInSlot = false;
			mSlotIndex = -1;

			if (mBlockedOverlay != null) mBlockedOverlay.SetActive(false);

			UpdateSortingOrder();

			StopCurrentAnimation();
			mCurrentAnimation = StartCoroutine(MoveToPositionCoroutine(boardPosition, null));
		}

		public void ReturnToBoard(Vector3 boardPosition, int x, int y, int layer)
		{
			mGridX = x;
			mGridY = y;
			mLayerIndex = layer;

			ReturnToBoard(boardPosition);
		}

		#endregion

		#region Animation Coroutines

		private IEnumerator FlyToSlotCoroutine(Vector3 targetPosition, Action onComplete)
		{
			mIsAnimating = true;

			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = Mathf.Clamp(distance / mMoveSpeed, 0.1F, 0.25F);

			float rotationAmount = UnityEngine.Random.Range(0, 2) == 0 ? mFlyRotation : -mFlyRotation;
			float arcHeight = distance * mFlyArcHeight;

			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;
				float easedT = t * t;

				Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, easedT);
				float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
				currentPos.y += arc;
				transform.position = currentPos;

				float currentRotation = Mathf.Lerp(0F, rotationAmount, easedT);
				transform.rotation = Quaternion.Euler(0F, 0F, currentRotation);

				yield return null;
			}

			transform.position = targetPosition;
			transform.rotation = Quaternion.identity;
			mIsAnimating = false;

			onComplete?.Invoke();
		}

		private IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, Action onComplete)
		{
			mIsAnimating = true;
			transform.rotation = Quaternion.identity;

			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = Mathf.Clamp(distance / mMoveSpeed, 0.03F, 0.15F);

			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.SmoothStep(0F, 1F, elapsed / duration);
				transform.position = Vector3.Lerp(startPos, targetPosition, t);
				yield return null;
			}

			transform.position = targetPosition;
			mIsAnimating = false;

			onComplete?.Invoke();
		}

		#endregion

		#region Other Animations

		public void PlayShakeAnimation()
		{
			if (mIsAnimating) return;
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

			transform.position = originalPos;
		}

		public void PlaySpawnAnimation(float delay = 0F)
		{
			StartCoroutine(SpawnCoroutine(delay));
		}

		private IEnumerator SpawnCoroutine(float delay)
		{
			transform.localScale = Vector3.zero;

			if (delay > 0F)
				yield return new WaitForSeconds(delay);

			mIsAnimating = true;
			float duration = 0.2F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;
				float scale = Mathf.Sin(t * Mathf.PI * 0.5F) * 1.1F;
				if (t > 0.7F)
					scale = Mathf.Lerp(1.1F, 1F, (t - 0.7F) / 0.3F);

				transform.localScale = mOriginalScale * Mathf.Clamp01(scale);
				yield return null;
			}

			transform.localScale = mOriginalScale;
			mIsAnimating = false;
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

		private void StopCurrentAnimation()
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
				mCurrentAnimation = null;
			}
			mIsAnimating = false;
		}

		public void ShowHint()
		{
			StartCoroutine(HintCoroutine());
		}

		private IEnumerator HintCoroutine()
		{
			if (mSpriteRenderer == null) yield break;

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
