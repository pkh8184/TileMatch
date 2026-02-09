using UnityEngine;
using System;
using System.Collections;
using TrumpTile.Audio;
using TileMatch;

namespace TrumpTile.Core
{
	/// <summary>
	/// 개별 타일 제어 (수정됨)
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
		[SerializeField] private TileData tileData;

		[Header("Visual")]
		[SerializeField] private SpriteRenderer spriteRenderer;
		[SerializeField] private SpriteRenderer highlightRenderer;
		[SerializeField] private GameObject blockedOverlay;
		[SerializeField] private GameObject frozenOverlay;
		[SerializeField] private GameObject lockedOverlay;

		[Header("Animation Settings")]
		[SerializeField] private float moveSpeed = 20f;
		[SerializeField] private float shakeDuration = 0.15f;
		[SerializeField] private float shakeIntensity = 0.05f;
		[SerializeField] private float flyArcHeight = 0.3f;
		[SerializeField] private float flyRotation = 360f;

		[Header("Debug Info")]
		[SerializeField] private int gridX;
		[SerializeField] private int gridY;
		[SerializeField] private int layerIndex;
		[SerializeField] private int currentSortingOrder;
		[SerializeField] private bool isSelectable = true;
		[SerializeField] private bool isInSlot;
		[SerializeField] private int slotIndex = -1;
		[SerializeField] private bool isFrozen;
		[SerializeField] private int frozenCount;
		[SerializeField] private bool isLocked;
		[SerializeField] private bool isAnimating;

		#endregion

		#region Sorting Constants

		// 레이어별 Sorting Order
		// Layer 0 (맨 아래) = 420
		// Layer 1 = 440
		// Layer 2 = 460
		// Layer 3 = 480
		// Layer 4 (맨 위) = 500
		private const int BASE_SORTING_ORDER = 420;
		private const int SORTING_STEP = 20;

		// 슬롯 타일 Sorting Order
		private const int SLOT_BASE_SORTING = 1000;
		private const int SLOT_SORTING_STEP = 10;

		// 이동 중 Sorting Order (가장 앞)
		private const int MOVING_SORTING_ORDER = 1099;

		#endregion

		#region Private Fields

		private Coroutine currentAnimation;
		private BoxCollider2D boxCollider;
		private Vector3 originalScale = Vector3.one;

		#endregion

		#region Properties

		public TileData Data => tileData;
		public string TileTypeId => tileData != null ? tileData.tileTypeId : "";
		public int GridX => gridX;
		public int GridY => gridY;
		public int LayerIndex => layerIndex;
		public int Layer => layerIndex;
		public bool IsSelectable => isSelectable && !isLocked && !isAnimating && !isInSlot;
		public bool IsInSlot => isInSlot;
		public int SlotIndex => slotIndex;
		public bool IsFrozen => isFrozen;
		public bool IsLocked => isLocked;
		public bool IsAnimating => isAnimating;

		#endregion

		#region Events

		public event Action<TileController> OnTileClicked;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (spriteRenderer == null)
				spriteRenderer = GetComponent<SpriteRenderer>();

			boxCollider = GetComponent<BoxCollider2D>();

			originalScale = transform.localScale;
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
			tileData = data;
			gridX = x;
			gridY = y;
			layerIndex = layer;

			isSelectable = true;
			isInSlot = false;
			slotIndex = -1;
			isFrozen = false;
			frozenCount = 0;
			isLocked = false;
			isAnimating = false;

			SetupVisual();

			// Sorting Order 설정 - 레이어 기반
			UpdateSortingOrder();

			SetOverlaysActive(false);
			EnableCollider(true);
		}

		private void SetupVisual()
		{
			if (spriteRenderer != null && tileData != null)
			{
				spriteRenderer.sprite = tileData.sprite;
				spriteRenderer.color = Color.white;
			}

			transform.rotation = Quaternion.identity;
		}

		private void SetOverlaysActive(bool active)
		{
			if (blockedOverlay != null) blockedOverlay.SetActive(active);
			if (frozenOverlay != null) frozenOverlay.SetActive(false);
			if (lockedOverlay != null) lockedOverlay.SetActive(false);
		}

		#endregion

		#region Sorting

		/// <summary>
		/// Sorting Order 업데이트
		/// 레이어 기반: Layer 0 = 420, Layer 1 = 440, ..., Layer 4 = 500
		/// </summary>
		public void UpdateSortingOrder()
		{
			if (spriteRenderer == null) return;

			if (isInSlot)
			{
				// 슬롯 타일: 1000 + (slotIndex * 10)
				currentSortingOrder = SLOT_BASE_SORTING + (slotIndex * SLOT_SORTING_STEP);
			}
			else
			{
				// 보드 타일: 420 + (layer * 20)
				// 레이어가 높을수록 Sorting Order가 높음 (앞에 표시)
				currentSortingOrder = BASE_SORTING_ORDER + (layerIndex * SORTING_STEP);
			}

			spriteRenderer.sortingOrder = currentSortingOrder;

			// 자식 렌더러도 함께 설정 (blockedOverlay 등)
			var childRenderers = GetComponentsInChildren<SpriteRenderer>();
			foreach (var sr in childRenderers)
			{
				if (sr != spriteRenderer)
				{
					sr.sortingOrder = currentSortingOrder + 1;
				}
			}
		}

		/// <summary>
		/// 슬롯 이동 중 Sorting Order (가장 앞)
		/// </summary>
		private void SetMovingToSlotSorting()
		{
			if (spriteRenderer != null)
			{
				currentSortingOrder = MOVING_SORTING_ORDER;
				spriteRenderer.sortingOrder = currentSortingOrder;
			}
		}

		/// <summary>
		/// 레이어 기반 Sorting Order 계산
		/// </summary>
		public static int GetLayerSortingOrder(int layer)
		{
			return BASE_SORTING_ORDER + (layer * SORTING_STEP);
		}

		#endregion

		#region Input

		private void OnMouseDown()
		{
			if (isAnimating) return;

			if (isInSlot)
			{
				PlayShakeAnimation();
				return;
			}

			if (!IsSelectable) return;

			if (isFrozen && frozenCount > 0)
			{
				BreakIce();
				return;
			}

			if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
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
			if (isInSlot || !IsSelectable) return;
			SetHighlight(true);
		}

		private void OnMouseExit()
		{
			SetHighlight(false);
		}

		private void BreakIce()
		{
			frozenCount--;
			if (frozenCount <= 0)
			{
				isFrozen = false;
				if (frozenOverlay != null) frozenOverlay.SetActive(false);
			}
			AudioManager.Instance?.PlayTileSelect();
		}

		#endregion

		#region Visual State

		public void SetHighlight(bool active)
		{
			if (highlightRenderer != null)
			{
				highlightRenderer.enabled = active;
			}
		}

		public void SetSelectable(bool selectable)
		{
			isSelectable = selectable;

			if (blockedOverlay != null)
			{
				blockedOverlay.SetActive(!selectable && !isInSlot);
			}
		}

		public void SetFrozen(bool frozen, int count = 1)
		{
			isFrozen = frozen;
			frozenCount = count;

			if (frozenOverlay != null)
				frozenOverlay.SetActive(frozen);
		}

		public void SetLocked(bool locked)
		{
			isLocked = locked;

			if (lockedOverlay != null)
				lockedOverlay.SetActive(locked);
		}

		public void EnableCollider(bool enable)
		{
			if (boxCollider != null)
			{
				boxCollider.enabled = enable;
			}
		}

		#endregion

		#region Data

		public void SetTileData(TileData data)
		{
			tileData = data;

			if (spriteRenderer != null && tileData != null)
			{
				spriteRenderer.sprite = tileData.sprite;
			}
		}

		/// <summary>
		/// 레이어 정보 업데이트 (Strike 아이템 등에서 사용)
		/// </summary>
		public void UpdateLayerInfo(int x, int y, int layer)
		{
			gridX = x;
			gridY = y;
			layerIndex = layer;
			UpdateSortingOrder();
		}

		#endregion

		#region Movement - To Slot

		public void MoveToSlot(Vector3 slotPosition, int index, Action onComplete = null)
		{
			bool wasInSlot = isInSlot;

			isInSlot = true;
			slotIndex = index;

			if (blockedOverlay != null) blockedOverlay.SetActive(false);

			SetMovingToSlotSorting();

			AudioManager.Instance?.PlayTileMove();

			StopCurrentAnimation();

			if (wasInSlot)
			{
				currentAnimation = StartCoroutine(MoveToPositionCoroutine(slotPosition, () => {
					UpdateSortingOrder();
					onComplete?.Invoke();
				}));
			}
			else
			{
				currentAnimation = StartCoroutine(FlyToSlotCoroutine(slotPosition, () => {
					UpdateSortingOrder();
					onComplete?.Invoke();
				}));
			}
		}

		public void MoveToSlot(Vector3 slotPosition)
		{
			MoveToSlot(slotPosition, slotIndex, null);
		}

		public void AdjustSlotPosition(Vector3 newPosition, int newIndex)
		{
			slotIndex = newIndex;
			UpdateSortingOrder();

			StopCurrentAnimation();
			currentAnimation = StartCoroutine(MoveToPositionCoroutine(newPosition, null));
		}

		#endregion

		#region Movement - To Board

		public void ReturnToBoard(Vector3 boardPosition)
		{
			isInSlot = false;
			slotIndex = -1;

			if (blockedOverlay != null) blockedOverlay.SetActive(false);

			UpdateSortingOrder();

			StopCurrentAnimation();
			currentAnimation = StartCoroutine(MoveToPositionCoroutine(boardPosition, null));
		}

		public void ReturnToBoard(Vector3 boardPosition, int x, int y, int layer)
		{
			gridX = x;
			gridY = y;
			layerIndex = layer;

			ReturnToBoard(boardPosition);
		}

		#endregion

		#region Animation Coroutines

		private IEnumerator FlyToSlotCoroutine(Vector3 targetPosition, Action onComplete)
		{
			isAnimating = true;

			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = Mathf.Clamp(distance / moveSpeed, 0.1f, 0.25f);

			float rotationAmount = UnityEngine.Random.Range(0, 2) == 0 ? flyRotation : -flyRotation;
			float arcHeight = distance * flyArcHeight;

			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				float easedT = t * t;

				Vector3 currentPos = Vector3.Lerp(startPos, targetPosition, easedT);
				float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
				currentPos.y += arc;
				transform.position = currentPos;

				float currentRotation = Mathf.Lerp(0f, rotationAmount, easedT);
				transform.rotation = Quaternion.Euler(0f, 0f, currentRotation);

				yield return null;
			}

			transform.position = targetPosition;
			transform.rotation = Quaternion.identity;
			isAnimating = false;

			onComplete?.Invoke();
		}

		private IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, Action onComplete)
		{
			isAnimating = true;
			transform.rotation = Quaternion.identity;

			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = Mathf.Clamp(distance / moveSpeed, 0.03f, 0.15f);

			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
				transform.position = Vector3.Lerp(startPos, targetPosition, t);
				yield return null;
			}

			transform.position = targetPosition;
			isAnimating = false;

			onComplete?.Invoke();
		}

		#endregion

		#region Other Animations

		public void PlayShakeAnimation()
		{
			if (isAnimating) return;
			StartCoroutine(ShakeCoroutine());
		}

		private IEnumerator ShakeCoroutine()
		{
			Vector3 originalPos = transform.position;
			float elapsed = 0f;

			while (elapsed < shakeDuration)
			{
				elapsed += Time.deltaTime;
				float progress = elapsed / shakeDuration;
				float damping = 1f - progress;

				float offsetX = Mathf.Sin(elapsed * 60f) * shakeIntensity * damping;
				float offsetY = Mathf.Cos(elapsed * 60f) * shakeIntensity * damping * 0.5f;

				transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
				yield return null;
			}

			transform.position = originalPos;
		}

		public void PlaySpawnAnimation(float delay = 0f)
		{
			StartCoroutine(SpawnCoroutine(delay));
		}

		private IEnumerator SpawnCoroutine(float delay)
		{
			transform.localScale = Vector3.zero;

			if (delay > 0f)
				yield return new WaitForSeconds(delay);

			isAnimating = true;
			float duration = 0.2f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;
				float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.1f;
				if (t > 0.7f)
					scale = Mathf.Lerp(1.1f, 1f, (t - 0.7f) / 0.3f);

				transform.localScale = originalScale * Mathf.Clamp01(scale);
				yield return null;
			}

			transform.localScale = originalScale;
			isAnimating = false;
		}

		public void PlayMatchAnimation(Action onComplete = null)
		{
			isSelectable = false;
			EnableCollider(false);
			StopCurrentAnimation();
			currentAnimation = StartCoroutine(MatchCoroutine(onComplete));
		}

		private IEnumerator MatchCoroutine(Action onComplete)
		{
			isAnimating = true;

			Vector3 startScale = transform.localScale;
			float duration = 0.15f;
			float elapsed = 0f;

			while (elapsed < duration * 0.3f)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (duration * 0.3f);
				transform.localScale = startScale * (1f + t * 0.3f);
				yield return null;
			}

			elapsed = 0f;
			Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

			while (elapsed < duration * 0.7f)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (duration * 0.7f);
				transform.localScale = startScale * 1.3f * (1f - t);

				if (spriteRenderer != null)
				{
					Color newColor = startColor;
					newColor.a = 1f - t;
					spriteRenderer.color = newColor;
				}

				yield return null;
			}

			isAnimating = false;
			onComplete?.Invoke();
		}

		public void Remove()
		{
			StopCurrentAnimation();
			StartCoroutine(RemoveCoroutine());
		}

		private IEnumerator RemoveCoroutine()
		{
			isAnimating = true;
			EnableCollider(false);

			float duration = 0.1f;
			float elapsed = 0f;
			Vector3 startScale = transform.localScale;
			Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				transform.localScale = startScale * (1f - t);

				if (spriteRenderer != null)
				{
					Color newColor = startColor;
					newColor.a = 1f - t;
					spriteRenderer.color = newColor;
				}

				yield return null;
			}

			Destroy(gameObject);
		}

		#endregion

		#region Utility

		private void StopCurrentAnimation()
		{
			if (currentAnimation != null)
			{
				StopCoroutine(currentAnimation);
				currentAnimation = null;
			}
			isAnimating = false;
		}

		public void ShowHint()
		{
			StartCoroutine(HintCoroutine());
		}

		private IEnumerator HintCoroutine()
		{
			if (spriteRenderer == null) yield break;

			Color originalColor = spriteRenderer.color;

			for (int i = 0; i < 3; i++)
			{
				spriteRenderer.color = Color.yellow;
				yield return new WaitForSeconds(0.1f);
				spriteRenderer.color = originalColor;
				yield return new WaitForSeconds(0.1f);
			}
		}

		#endregion
	}
}