using UnityEngine;
using System.Collections;
using System;

namespace TrumpTile.Animation
{
    /// <summary>
    /// 타일 이동 및 애니메이션 처리
    /// </summary>
    public class TileAnimator : MonoBehaviour
    {
        [Header("Move Animation")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Select Animation")]
        [SerializeField] private float selectScale = 1.15f;
        [SerializeField] private float selectDuration = 0.1f;
        [SerializeField] private float hoverBobAmount = 0.05f;
        [SerializeField] private float hoverBobSpeed = 3f;

        [Header("Match Animation")]
        [SerializeField] private float matchScalePunch = 1.3f;
        [SerializeField] private float matchDuration = 0.2f;

        [Header("Spawn Animation")]
        [SerializeField] private float spawnDuration = 0.3f;
        [SerializeField] private float spawnDelay = 0.02f; // 타일 간 딜레이

        [Header("Shake Animation")]
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeIntensity = 0.1f;

        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool isAnimating;
        private Coroutine currentAnimation;

        public bool IsAnimating => isAnimating;
        public event Action OnAnimationComplete;

        private void Awake()
        {
            originalScale = transform.localScale;
            originalPosition = transform.position;
        }

        #region Move Animation

        /// <summary>
        /// 타일을 목표 위치로 이동
        /// </summary>
        public void MoveTo(Vector3 targetPosition, Action onComplete = null)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(MoveToCoroutine(targetPosition, onComplete));
        }

        private IEnumerator MoveToCoroutine(Vector3 targetPosition, Action onComplete)
        {
            isAnimating = true;
            Vector3 startPos = transform.position;
            float distance = Vector3.Distance(startPos, targetPosition);
            float duration = distance / moveSpeed;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = moveCurve.Evaluate(elapsed / duration);

                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            isAnimating = false;

            onComplete?.Invoke();
            OnAnimationComplete?.Invoke();
        }

        /// <summary>
        /// 슬롯으로 이동 (아크 궤적)
        /// </summary>
        public void MoveToSlot(Vector3 targetPosition, Action onComplete = null)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(MoveToSlotCoroutine(targetPosition, onComplete));
        }

        private IEnumerator MoveToSlotCoroutine(Vector3 targetPosition, Action onComplete)
        {
            isAnimating = true;
            Vector3 startPos = transform.position;
            float distance = Vector3.Distance(startPos, targetPosition);
            float duration = Mathf.Clamp(distance / moveSpeed, 0.15f, 0.4f);

            // 아크 높이 계산
            float arcHeight = distance * 0.3f;
            Vector3 midPoint = (startPos + targetPosition) / 2f + Vector3.up * arcHeight;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = moveCurve.Evaluate(elapsed / duration);

                // 베지어 곡선으로 아크 이동
                Vector3 a = Vector3.Lerp(startPos, midPoint, t);
                Vector3 b = Vector3.Lerp(midPoint, targetPosition, t);
                transform.position = Vector3.Lerp(a, b, t);

                // 이동 중 약간 회전
                float rotation = Mathf.Sin(t * Mathf.PI) * 15f;
                transform.rotation = Quaternion.Euler(0, 0, rotation);

                yield return null;
            }

            transform.position = targetPosition;
            transform.rotation = Quaternion.identity;
            isAnimating = false;

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
            float elapsed = 0f;
            Vector3 targetScale = originalScale * selectScale;

            while (elapsed < selectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / selectDuration;

                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            transform.localScale = targetScale;

            // 호버 애니메이션 시작
            StartCoroutine(HoverBobCoroutine());
        }

        private IEnumerator HoverBobCoroutine()
        {
            while (transform.localScale.x >= originalScale.x * selectScale * 0.99f)
            {
                float yOffset = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;
                Vector3 pos = originalPosition;
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
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < selectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / selectDuration;

                transform.localScale = Vector3.Lerp(startScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }

        #endregion

        #region Match Animation

        /// <summary>
        /// 매칭 성공 애니메이션
        /// </summary>
        public void PlayMatchAnimation(Action onComplete = null)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);

            currentAnimation = StartCoroutine(MatchAnimationCoroutine(onComplete));
        }

        private IEnumerator MatchAnimationCoroutine(Action onComplete)
        {
            isAnimating = true;

            // 펀치 스케일
            float elapsed = 0f;
            Vector3 punchScale = originalScale * matchScalePunch;

            // 확대
            while (elapsed < matchDuration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (matchDuration * 0.3f);

                transform.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            // 축소 및 페이드
            elapsed = 0f;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < matchDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (matchDuration * 0.7f);

                transform.localScale = Vector3.Lerp(punchScale, Vector3.zero, t);

                if (spriteRenderer != null)
                {
                    Color newColor = startColor;
                    newColor.a = 1f - t;
                    spriteRenderer.color = newColor;
                }

                yield return null;
            }

            transform.localScale = Vector3.zero;
            isAnimating = false;

            onComplete?.Invoke();
            OnAnimationComplete?.Invoke();
        }

        #endregion

        #region Spawn Animation

        /// <summary>
        /// 스폰 애니메이션 (등장)
        /// </summary>
        public void PlaySpawnAnimation(float delay = 0f, Action onComplete = null)
        {
            StartCoroutine(SpawnAnimationCoroutine(delay, onComplete));
        }

        private IEnumerator SpawnAnimationCoroutine(float delay, Action onComplete)
        {
            transform.localScale = Vector3.zero;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            isAnimating = true;
            float elapsed = 0f;

            while (elapsed < spawnDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / spawnDuration;

                // 바운스 효과
                float bounce = Mathf.Sin(t * Mathf.PI * 1.5f);
                float scale = t + bounce * 0.2f * (1f - t);

                transform.localScale = originalScale * Mathf.Clamp01(scale);
                yield return null;
            }

            transform.localScale = originalScale;
            isAnimating = false;

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
                    tiles[i].PlaySpawnAnimation(i * 0.02f);
                }
            }

            yield return new WaitForSeconds(tiles.Length * 0.02f + 0.3f);
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
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;

                float damping = 1f - t;
                float offsetX = Mathf.Sin(Time.time * 50f) * shakeIntensity * damping;
                float offsetY = Mathf.Cos(Time.time * 50f) * shakeIntensity * damping * 0.5f;

                transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            transform.position = originalPos;
        }

        /// <summary>
        /// 모든 타일 흔들림 (게임 오버 시)
        /// </summary>
        public static void ShakeAllTiles(TileAnimator[] tiles)
        {
            foreach (var tile in tiles)
            {
                if (tile != null)
                    tile.PlayShakeAnimation();
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 현재 진행 중인 애니메이션 중단
        /// </summary>
        public void StopAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            isAnimating = false;
        }

        /// <summary>
        /// 원래 상태로 리셋
        /// </summary>
        public void ResetState()
        {
            StopAnimation();
            transform.localScale = originalScale;
            transform.rotation = Quaternion.identity;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        /// <summary>
        /// 원래 스케일 업데이트
        /// </summary>
        public void SetOriginalScale(Vector3 scale)
        {
            originalScale = scale;
        }

        #endregion
    }
}
