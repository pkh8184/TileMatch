using UnityEngine;
using System.Collections;
using System;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 타일 이동 및 애니메이션 처리
	/// </summary>
	public class TileAnimator : MonoBehaviour
	{
		[Header("Move Animation")]
		[SerializeField] private float moveSpeed = 10F;
		[SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

		[Header("Select Animation")]
		[SerializeField] private float selectScale = 1.15F;
		[SerializeField] private float selectDuration = 0.1F;
		[SerializeField] private float hoverBobAmount = 0.05F;
		[SerializeField] private float hoverBobSpeed = 3F;

		[Header("Match Animation")]
		[SerializeField] private float matchScalePunch = 1.3F;
		[SerializeField] private float matchDuration = 0.2F;

		[Header("Spawn Animation")]
		[SerializeField] private float spawnDuration = 0.3F;
		[SerializeField] private float spawnDelay = 0.02F; // 타일 간 딜레이

		[Header("Shake Animation")]
		[SerializeField] private float shakeDuration = 0.3F;
		[SerializeField] private float shakeIntensity = 0.1F;

		private Vector3 mOriginalScale;
		private Vector3 mOriginalPosition;
		private bool mIsAnimating;
		private Coroutine mCurrentAnimation;

		public bool IsAnimating => mIsAnimating;
		public event Action OnAnimationComplete;

		private void Awake()
		{
			mOriginalScale = transform.localScale;
			mOriginalPosition = transform.position;
		}

		#region Move Animation

		/// <summary>
		/// 타일을 목표 위치로 이동
		/// </summary>
		public void MoveTo(Vector3 targetPosition, Action onComplete = null)
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
			}

			mCurrentAnimation = StartCoroutine(MoveToCoroutine(targetPosition, onComplete));
		}

		private IEnumerator MoveToCoroutine(Vector3 targetPosition, Action onComplete)
		{
			mIsAnimating = true;
			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = distance / moveSpeed;

			float elapsed = 0F;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = moveCurve.Evaluate(elapsed / duration);

				transform.position = Vector3.Lerp(startPos, targetPosition, t);
				yield return null;
			}

			transform.position = targetPosition;
			mIsAnimating = false;

			onComplete?.Invoke();
			OnAnimationComplete?.Invoke();
		}

		/// <summary>
		/// 슬롯으로 이동 (아크 궤적)
		/// </summary>
		public void MoveToSlot(Vector3 targetPosition, Action onComplete = null)
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
			}

			mCurrentAnimation = StartCoroutine(MoveToSlotCoroutine(targetPosition, onComplete));
		}

		private IEnumerator MoveToSlotCoroutine(Vector3 targetPosition, Action onComplete)
		{
			mIsAnimating = true;
			Vector3 startPos = transform.position;
			float distance = Vector3.Distance(startPos, targetPosition);
			float duration = Mathf.Clamp(distance / moveSpeed, 0.15F, 0.4F);

			// 아크 높이 계산
			float arcHeight = distance * 0.3F;
			Vector3 midPoint = (startPos + targetPosition) / 2F + Vector3.up * arcHeight;

			float elapsed = 0F;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = moveCurve.Evaluate(elapsed / duration);

				// 베지어 곡선으로 아크 이동
				Vector3 a = Vector3.Lerp(startPos, midPoint, t);
				Vector3 b = Vector3.Lerp(midPoint, targetPosition, t);
				transform.position = Vector3.Lerp(a, b, t);

				// 이동 중 약간 회전
				float rotation = Mathf.Sin(t * Mathf.PI) * 15F;
				transform.rotation = Quaternion.Euler(0, 0, rotation);

				yield return null;
			}

			transform.position = targetPosition;
			transform.rotation = Quaternion.identity;
			mIsAnimating = false;

			onComplete?.Invoke();
			OnAnimationComplete?.Invoke();
		}

		#endregion

		#region Select Animation

		/// <summary>
		/// 선택 애니메이션 (확대)
		/// </summary>
		public void PlaySelectAnimation()
		{
			StartCoroutine(SelectAnimationCoroutine());
		}

		private IEnumerator SelectAnimationCoroutine()
		{
			float elapsed = 0F;
			Vector3 targetScale = mOriginalScale * selectScale;

			while (elapsed < selectDuration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / selectDuration;

				transform.localScale = Vector3.Lerp(mOriginalScale, targetScale, t);
				yield return null;
			}

			transform.localScale = targetScale;

			// 호버 애니메이션 시작
			StartCoroutine(HoverBobCoroutine());
		}

		private IEnumerator HoverBobCoroutine()
		{
			while (transform.localScale.x >= mOriginalScale.x * selectScale * 0.99F)
			{
				float yOffset = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;
				Vector3 pos = mOriginalPosition;
				pos.y += yOffset;
				// transform.position = pos; // 호버 위치 유지가 필요하면 사용
				yield return null;
			}
		}

		/// <summary>
		/// 선택 해제 애니메이션
		/// </summary>
		public void PlayDeselectAnimation()
		{
			StartCoroutine(DeselectAnimationCoroutine());
		}

		private IEnumerator DeselectAnimationCoroutine()
		{
			float elapsed = 0F;
			Vector3 startScale = transform.localScale;

			while (elapsed < selectDuration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / selectDuration;

				transform.localScale = Vector3.Lerp(startScale, mOriginalScale, t);
				yield return null;
			}

			transform.localScale = mOriginalScale;
		}

		#endregion

		#region Match Animation

		/// <summary>
		/// 매칭 성공 애니메이션
		/// </summary>
		public void PlayMatchAnimation(Action onComplete = null)
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
			}

			mCurrentAnimation = StartCoroutine(MatchAnimationCoroutine(onComplete));
		}

		private IEnumerator MatchAnimationCoroutine(Action onComplete)
		{
			mIsAnimating = true;

			// 펀치 스케일
			float elapsed = 0F;
			Vector3 punchScale = mOriginalScale * matchScalePunch;

			// 확대
			while (elapsed < matchDuration * 0.3F)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (matchDuration * 0.3F);

				transform.localScale = Vector3.Lerp(mOriginalScale, punchScale, t);
				yield return null;
			}

			// 축소 및 페이드
			elapsed = 0F;
			SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
			Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

			while (elapsed < matchDuration * 0.7F)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / (matchDuration * 0.7F);

				transform.localScale = Vector3.Lerp(punchScale, Vector3.zero, t);

				if (spriteRenderer != null)
				{
					Color newColor = startColor;
					newColor.a = 1F - t;
					spriteRenderer.color = newColor;
				}

				yield return null;
			}

			transform.localScale = Vector3.zero;
			mIsAnimating = false;

			onComplete?.Invoke();
			OnAnimationComplete?.Invoke();
		}

		#endregion

		#region Spawn Animation

		/// <summary>
		/// 스폰 애니메이션 (등장)
		/// </summary>
		public void PlaySpawnAnimation(float delay = 0F, Action onComplete = null)
		{
			StartCoroutine(SpawnAnimationCoroutine(delay, onComplete));
		}

		private IEnumerator SpawnAnimationCoroutine(float delay, Action onComplete)
		{
			transform.localScale = Vector3.zero;

			if (delay > 0F)
			{
				yield return new WaitForSeconds(delay);
			}

			mIsAnimating = true;
			float elapsed = 0F;

			while (elapsed < spawnDuration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / spawnDuration;

				// 바운스 효과
				float bounce = Mathf.Sin(t * Mathf.PI * 1.5F);
				float scale = t + bounce * 0.2F * (1F - t);

				transform.localScale = mOriginalScale * Mathf.Clamp01(scale);
				yield return null;
			}

			transform.localScale = mOriginalScale;
			mIsAnimating = false;

			onComplete?.Invoke();
		}

		/// <summary>
		/// 여러 타일 스폰 애니메이션 (딜레이 적용)
		/// </summary>
		public static IEnumerator SpawnMultipleTiles(TileAnimator[] tiles)
		{
			for (int i = 0; i < tiles.Length; i++)
			{
				if (tiles[i] != null)
				{
					tiles[i].PlaySpawnAnimation(i * 0.02F);
				}
			}

			yield return new WaitForSeconds(tiles.Length * 0.02F + 0.3F);
		}

		#endregion

		#region Shake Animation

		/// <summary>
		/// 흔들림 애니메이션 (슬롯 가득 찼을 때 등)
		/// </summary>
		public void PlayShakeAnimation()
		{
			StartCoroutine(ShakeAnimationCoroutine());
		}

		private IEnumerator ShakeAnimationCoroutine()
		{
			Vector3 originalPos = transform.position;
			float elapsed = 0F;

			while (elapsed < shakeDuration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / shakeDuration;

				float damping = 1F - t;
				float offsetX = Mathf.Sin(Time.time * 50F) * shakeIntensity * damping;
				float offsetY = Mathf.Cos(Time.time * 50F) * shakeIntensity * damping * 0.5F;

				transform.position = originalPos + new Vector3(offsetX, offsetY, 0F);
				yield return null;
			}

			transform.position = originalPos;
		}

		/// <summary>
		/// 모든 타일 흔들림 (게임 오버 시)
		/// </summary>
		public static void ShakeAllTiles(TileAnimator[] tiles)
		{
			foreach (TileAnimator tile in tiles)
			{
				if (tile != null)
				{
					tile.PlayShakeAnimation();
				}
			}
		}

		#endregion

		#region Utility

		/// <summary>
		/// 현재 진행 중인 애니메이션 중단
		/// </summary>
		public void StopAnimation()
		{
			if (mCurrentAnimation != null)
			{
				StopCoroutine(mCurrentAnimation);
				mCurrentAnimation = null;
			}
			mIsAnimating = false;
		}

		/// <summary>
		/// 원래 상태로 리셋
		/// </summary>
		public void ResetState()
		{
			StopAnimation();
			transform.localScale = mOriginalScale;
			transform.rotation = Quaternion.identity;

			SpriteRenderer sr = GetComponent<SpriteRenderer>();
			if (sr != null)
			{
				Color c = sr.color;
				c.a = 1F;
				sr.color = c;
			}
		}

		/// <summary>
		/// 원래 스케일 업데이트
		/// </summary>
		public void SetOriginalScale(Vector3 scale)
		{
			mOriginalScale = scale;
		}

		#endregion
	}
}
