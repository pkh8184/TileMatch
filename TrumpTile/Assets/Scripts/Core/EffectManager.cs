using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace TrumpTile.Effects
{
    /// <summary>
    /// 이펙트 관리자 (아이템 이펙트 추가)
    /// 
    /// [추가된 이펙트]
    /// - Strike: 슬롯 터짐 → 보드 착지
    /// - BlackHole: 등장 → 타일 흡수 → 폭발
    /// - Boom: 타일 위치에서 순차 폭발
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        #region Match Effect Settings

        [Header("Match Effect")]
        [SerializeField] private GameObject matchEffectPrefab;
        [SerializeField] private float matchEffectDuration = 0.5f;
        [SerializeField] private Color[] suitColors = new Color[]
        {
            new Color(0.2f, 0.2f, 0.3f, 1f),    // Spade
            new Color(0.9f, 0.2f, 0.3f, 1f),    // Heart
            new Color(0.9f, 0.2f, 0.3f, 1f),    // Diamond
            new Color(0.2f, 0.2f, 0.3f, 1f)     // Club
        };

        [Header("Particle Settings")]
        [SerializeField] private int particleCount = 12;
        [SerializeField] private float particleSpeed = 5f;

        [Header("Screen Flash")]
        [SerializeField] private bool enableScreenFlash = true;
        [SerializeField] private CanvasGroup screenFlashGroup;
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private float flashIntensity = 0.3f;

        #endregion

        #region Victory/GameOver Effect Settings

        [Header("Victory Effect")]
        [SerializeField] private GameObject victoryEffectPrefab;
        [SerializeField] private Transform victoryEffectSpawnPoint;
        [SerializeField] private int victoryEffectCount = 5;
        [SerializeField] private float victoryEffectDelay = 0.2f;
        [SerializeField] private Vector2 victorySpawnRangeX = new Vector2(-3f, 3f);
        [SerializeField] private Vector2 victorySpawnRangeY = new Vector2(-2f, 2f);

        [Header("Game Over Effect")]
        [SerializeField] private GameObject gameOverEffectPrefab;

        #endregion

        #region Item Effect Settings

        [Header("=== Item Effects ===")]
        
        [Header("Strike Effect")]
        [Tooltip("Fx_Strike_001 - 슬롯에서 터지는 이펙트")]
        [SerializeField] private GameObject strikePopEffectPrefab;
        [Tooltip("Fx_Strike_002 - 보드 착지 이펙트")]
        [SerializeField] private GameObject strikeLandEffectPrefab;
        [SerializeField] private float strikePopDuration = 0.3f;
        [SerializeField] private float strikeLandDuration = 0.2f;

        [Header("BlackHole Effect")]
        [Tooltip("Fx_BlackHole_001 - 블랙홀 등장 이펙트")]
        [SerializeField] private GameObject blackHoleAppearEffectPrefab;
        [Tooltip("Fx_BlackHole_002 - 블랙홀 폭발 이펙트")]
        [SerializeField] private GameObject blackHoleExplodeEffectPrefab;
        [SerializeField] private float blackHoleAppearDuration = 0.3f;
        [SerializeField] private float blackHoleSuckDuration = 0.5f;
        [Tooltip("블랙홀 폭발 이펙트 지속 시간")]
        [SerializeField] private float blackHoleExplodeDuration = 0.3f;
        [Tooltip("폭발 후 타일이 튀어나가기 시작하는 딜레이")]
        [SerializeField] private float blackHoleScatterDelay = 0.1f;
        [Tooltip("타일이 원위치로 돌아가는 시간")]
        [SerializeField] private float blackHoleScatterDuration = 0.4f;
        [Tooltip("각 타일의 도착 시간 랜덤 범위 (최소)")]
        [SerializeField] private float blackHoleScatterRandomMin = 0.01f;
        [Tooltip("각 타일의 도착 시간 랜덤 범위 (최대)")]
        [SerializeField] private float blackHoleScatterRandomMax = 0.15f;
        [SerializeField] private Vector3 blackHolePosition = Vector3.zero;

        [Header("Boom Effect")]
        [Tooltip("Fx_Bomb_001 - 폭탄 폭발 이펙트")]
        [SerializeField] private GameObject bombExplodeEffectPrefab;
        [SerializeField] private float bombExplodeDelay = 0.15f;
        [SerializeField] private float bombExplodeDuration = 0.3f;
        [Tooltip("폭발 후 타일 사라지기까지 딜레이")]
        [SerializeField] private float bombRemoveDelay = 0.1f;

        #endregion

        #region Private Fields

        private Queue<GameObject> effectPool = new Queue<GameObject>();
        private const int PoolSize = 20;

        private bool isClearEffectPlaying = false;
        private bool isGameOverEffectPlaying = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePool();
        }

        #endregion

        #region Pool Management

        private void InitializePool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject obj = new GameObject("EffectParticle");
                obj.transform.SetParent(transform);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite();
                sr.sortingOrder = 200;

                obj.SetActive(false);
                effectPool.Enqueue(obj);
            }
        }

        private Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    colors[y * size + x] = dist < radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private GameObject GetFromPool()
        {
            if (effectPool.Count > 0)
            {
                var obj = effectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            GameObject newObj = new GameObject("EffectParticle");
            newObj.transform.SetParent(transform);
            var sr = newObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.sortingOrder = 200;
            return newObj;
        }

        private void ReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                obj.transform.localScale = Vector3.one * 0.2f;
                effectPool.Enqueue(obj);
            }
        }

        #endregion

        #region Match Effect

        public void PlayMatchEffect(Vector3 position, int suitIndex = 0, int comboLevel = 1)
        {
            Debug.Log($"[EffectManager] PlayMatchEffect at {position}");

            if (matchEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(matchEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effectObj, matchEffectDuration + 1f);
            }

            if (enableScreenFlash && screenFlashGroup != null && comboLevel > 1)
            {
                StartCoroutine(DoScreenFlash(Color.white));
            }
        }

        private IEnumerator SpawnMatchParticles(Vector3 position, Color color, int count)
        {
            List<GameObject> particles = new List<GameObject>();
            List<Vector3> directions = new List<Vector3>();

            for (int i = 0; i < count; i++)
            {
                var particle = GetFromPool();
                if (particle == null) continue;

                particle.transform.position = position;
                particle.transform.localScale = Vector3.one * 0.2f;

                var sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;

                float angle = (360f / count) * i + UnityEngine.Random.Range(-15f, 15f);
                Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                dir *= particleSpeed * UnityEngine.Random.Range(0.8f, 1.2f);

                particles.Add(particle);
                directions.Add(dir);
            }

            float elapsed = 0f;
            while (elapsed < matchEffectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / matchEffectDuration;

                for (int i = 0; i < particles.Count; i++)
                {
                    if (particles[i] == null) continue;

                    particles[i].transform.position += directions[i] * Time.deltaTime;
                    particles[i].transform.localScale = Vector3.one * 0.2f * (1f - t);

                    var sr = particles[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f - t;
                        sr.color = c;
                    }
                }

                yield return null;
            }

            foreach (var particle in particles)
            {
                ReturnToPool(particle);
            }
        }

        private IEnumerator DoScreenFlash(Color color)
        {
            var image = screenFlashGroup.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = new Color(color.r, color.g, color.b, flashIntensity);
            }

            screenFlashGroup.alpha = flashIntensity;

            yield return new WaitForSeconds(flashDuration);

            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                screenFlashGroup.alpha = flashIntensity * (1f - elapsed / flashDuration);
                yield return null;
            }

            screenFlashGroup.alpha = 0f;
        }

        #endregion

        #region Victory/GameOver Effects

        public void PlayClearEffect()
        {
            if (isClearEffectPlaying)
            {
                Debug.Log("[EffectManager] PlayClearEffect - BLOCKED");
                return;
            }

            Debug.Log("[EffectManager] PlayClearEffect - STARTING");
            isClearEffectPlaying = true;
            StartCoroutine(ClearCelebration());
        }

        private IEnumerator ClearCelebration()
        {
            if (victoryEffectPrefab != null)
            {
                for (int i = 0; i < victoryEffectCount; i++)
                {
                    Vector3 spawnPos;

                    if (victoryEffectSpawnPoint != null)
                    {
                        spawnPos = victoryEffectSpawnPoint.position;
                    }
                    else
                    {
                        spawnPos = new Vector3(
                            UnityEngine.Random.Range(victorySpawnRangeX.x, victorySpawnRangeX.y),
                            UnityEngine.Random.Range(victorySpawnRangeY.x, victorySpawnRangeY.y),
                            0f
                        );
                    }

                    spawnPos += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), 0f);

                    GameObject effectObj = Instantiate(victoryEffectPrefab, spawnPos, Quaternion.identity);
                    AutoDestroyEffect(effectObj, 3f);

                    yield return new WaitForSeconds(victoryEffectDelay);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomPos = new Vector3(
                        UnityEngine.Random.Range(-3f, 3f),
                        UnityEngine.Random.Range(-2f, 2f),
                        0f
                    );

                    int randomColor = UnityEngine.Random.Range(0, suitColors.Length);
                    StartCoroutine(SpawnMatchParticles(randomPos, suitColors[randomColor], particleCount));

                    yield return new WaitForSeconds(0.15f);
                }
            }

            yield return new WaitForSeconds(1f);
            isClearEffectPlaying = false;
            Debug.Log("[EffectManager] ClearEffect - FINISHED");
        }

        public void PlayGameOverEffect()
        {
            if (isGameOverEffectPlaying)
            {
                Debug.Log("[EffectManager] PlayGameOverEffect - BLOCKED");
                return;
            }

            Debug.Log("[EffectManager] PlayGameOverEffect - STARTING");
            isGameOverEffectPlaying = true;
            StartCoroutine(GameOverEffectCoroutine());
        }

        private IEnumerator GameOverEffectCoroutine()
        {
            if (gameOverEffectPrefab != null)
            {
                Vector3 spawnPos = Camera.main != null
                    ? Camera.main.transform.position + Vector3.forward * 5f
                    : Vector3.zero;
                spawnPos.z = 0f;

                GameObject effectObj = Instantiate(gameOverEffectPrefab, spawnPos, Quaternion.identity);
                AutoDestroyEffect(effectObj, 2f);
            }

            if (screenFlashGroup != null)
            {
                yield return StartCoroutine(DoScreenFlash(Color.red));
            }

            yield return new WaitForSeconds(0.5f);
            isGameOverEffectPlaying = false;
            Debug.Log("[EffectManager] GameOverEffect - FINISHED");
        }

        #endregion

        #region Strike Effect

        /// <summary>
        /// Strike 아이템 이펙트 재생
        /// </summary>
        /// <param name="slotPosition">슬롯에서 타일 위치</param>
        /// <param name="onPopComplete">터짐 이펙트 완료 후 콜백 (착지 위치 반환 필요)</param>
        /// <param name="onLandComplete">착지 이펙트 완료 후 콜백</param>
        public void PlayStrikeEffect(Vector3 slotPosition, Action<Action<Vector3>> onPopComplete, Action onLandComplete)
        {
            StartCoroutine(StrikeEffectCoroutine(slotPosition, onPopComplete, onLandComplete));
        }

        private IEnumerator StrikeEffectCoroutine(Vector3 slotPosition, Action<Action<Vector3>> onPopComplete, Action onLandComplete)
        {
            Debug.Log($"[EffectManager] Strike Effect - Pop at {slotPosition}");

            // 1. 슬롯에서 터짐 이펙트
            if (strikePopEffectPrefab != null)
            {
                GameObject popEffect = Instantiate(strikePopEffectPrefab, slotPosition, Quaternion.identity);
                AutoDestroyEffect(popEffect, strikePopDuration + 1f);
            }

            yield return new WaitForSeconds(strikePopDuration);

            // 터짐 완료 콜백 (타일 이동 시작, 착지 위치 콜백 받기)
            Vector3 landPosition = Vector3.zero;
            bool landPositionReceived = false;

            onPopComplete?.Invoke((pos) =>
            {
                landPosition = pos;
                landPositionReceived = true;
            });

            // 타일 이동 시간 대기
            yield return new WaitForSeconds(0.2f);

            // 착지 위치를 못 받았으면 대기
            float waitTime = 0f;
            while (!landPositionReceived && waitTime < 1f)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[EffectManager] Strike Effect - Land at {landPosition}");

            // 2. 보드 착지 이펙트
            if (strikeLandEffectPrefab != null)
            {
                GameObject landEffect = Instantiate(strikeLandEffectPrefab, landPosition, Quaternion.identity);
                AutoDestroyEffect(landEffect, strikeLandDuration + 1f);
            }

            yield return new WaitForSeconds(strikeLandDuration);

            // 착지 완료 콜백
            onLandComplete?.Invoke();

            Debug.Log("[EffectManager] Strike Effect - FINISHED");
        }
        
        /// <summary>
        /// Strike 이펙트 (간단 버전 - 위치 직접 지정)
        /// </summary>
        public void PlayStrikePopEffect(Vector3 position)
        {
            if (strikePopEffectPrefab != null)
            {
                GameObject effect = Instantiate(strikePopEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effect, strikePopDuration + 1f);
            }
        }

        public void PlayStrikeLandEffect(Vector3 position)
        {
            if (strikeLandEffectPrefab != null)
            {
                GameObject effect = Instantiate(strikeLandEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effect, strikeLandDuration + 1f);
            }
        }

        /// <summary>
        /// Strike 이펙트 총 시간 반환
        /// </summary>
        public float GetStrikeEffectDuration()
        {
            return strikePopDuration + 0.2f + strikeLandDuration;
        }

        #endregion

        #region BlackHole Effect

        /// <summary>
        /// BlackHole 아이템 이펙트 재생
        /// </summary>
        /// <param name="tiles">빨려들어갈 타일들</param>
        /// <param name="onSuckComplete">흡수 완료 후 콜백 (셔플 시작)</param>
        /// <param name="onExplodeComplete">폭발 완료 후 콜백</param>
        public void PlayBlackHoleEffect(List<Transform> tiles, Action onSuckComplete, Action onExplodeComplete)
        {
            StartCoroutine(BlackHoleEffectCoroutine(tiles, onSuckComplete, onExplodeComplete));
        }

        private IEnumerator BlackHoleEffectCoroutine(List<Transform> tiles, Action onSuckComplete, Action onExplodeComplete)
        {
            Debug.Log($"[EffectManager] BlackHole Effect - Start at {blackHolePosition}");

            // 원본 위치/스케일 저장
            var originalPositions = new List<Vector3>();
            var originalScales = new List<Vector3>();
            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    originalPositions.Add(tile.position);
                    originalScales.Add(tile.localScale);
                }
                else
                {
                    originalPositions.Add(Vector3.zero);
                    originalScales.Add(Vector3.one);
                }
            }

            // 1. 블랙홀 등장 이펙트 (스케일 0 → 1)
            GameObject blackHoleObj = null;
            if (blackHoleAppearEffectPrefab != null)
            {
                blackHoleObj = Instantiate(blackHoleAppearEffectPrefab, blackHolePosition, Quaternion.identity);
                
                blackHoleObj.transform.localScale = Vector3.zero;
                float elapsed = 0f;
                while (elapsed < blackHoleAppearDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / blackHoleAppearDuration;
                    float scale = Mathf.Lerp(0f, 1f, EaseOutBack(t));
                    blackHoleObj.transform.localScale = Vector3.one * scale;
                    yield return null;
                }
                blackHoleObj.transform.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(0.1f);

            // 2. 타일들 블랙홀로 흡수 (각 타일 도착 시간 랜덤)
            if (tiles != null && tiles.Count > 0)
            {
                var randomDelays = new List<float>();
                foreach (var tile in tiles)
                {
                    randomDelays.Add(UnityEngine.Random.Range(blackHoleScatterRandomMin, blackHoleScatterRandomMax));
                }

                float suckElapsed = 0f;
                float maxSuckTime = blackHoleSuckDuration + blackHoleScatterRandomMax;
                
                while (suckElapsed < maxSuckTime)
                {
                    suckElapsed += Time.deltaTime;

                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            float tileElapsed = suckElapsed - randomDelays[i];
                            if (tileElapsed < 0f) tileElapsed = 0f;
                            
                            float t = Mathf.Clamp01(tileElapsed / blackHoleSuckDuration);
                            float easeT = EaseInQuad(t);

                            tiles[i].position = Vector3.Lerp(originalPositions[i], blackHolePosition, easeT);
                            tiles[i].localScale = Vector3.Lerp(originalScales[i], Vector3.one * 0.1f, easeT);
                        }
                    }
                    yield return null;
                }

                // 모든 타일을 블랙홀 중심으로 확실히 이동
                foreach (var tile in tiles)
                {
                    if (tile != null)
                    {
                        tile.position = blackHolePosition;
                        tile.localScale = Vector3.one * 0.1f;
                    }
                }
            }

            Debug.Log("[EffectManager] BlackHole Effect - Suck complete");

            // 흡수 완료 콜백 (셔플 실행)
            onSuckComplete?.Invoke();

            // 3. 블랙홀 폭발 이펙트
            if (blackHoleObj != null)
            {
                Destroy(blackHoleObj);
            }

            if (blackHoleExplodeEffectPrefab != null)
            {
                GameObject explodeEffect = Instantiate(blackHoleExplodeEffectPrefab, blackHolePosition, Quaternion.identity);
                AutoDestroyEffect(explodeEffect, blackHoleExplodeDuration + 1f);
            }

            // 폭발 후 딜레이
            yield return new WaitForSeconds(blackHoleScatterDelay);

            // 4. 타일들 원위치로 흩어지는 애니메이션
            if (tiles != null && tiles.Count > 0)
            {
                var scatterDelays = new List<float>();
                foreach (var tile in tiles)
                {
                    scatterDelays.Add(UnityEngine.Random.Range(blackHoleScatterRandomMin, blackHoleScatterRandomMax));
                }

                float scatterElapsed = 0f;
                float maxScatterTime = blackHoleScatterDuration + blackHoleScatterRandomMax;

                while (scatterElapsed < maxScatterTime)
                {
                    scatterElapsed += Time.deltaTime;

                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            float tileElapsed = scatterElapsed - scatterDelays[i];
                            if (tileElapsed < 0f) tileElapsed = 0f;

                            float t = Mathf.Clamp01(tileElapsed / blackHoleScatterDuration);
                            float easeT = EaseOutBack(t);

                            tiles[i].position = Vector3.Lerp(blackHolePosition, originalPositions[i], easeT);
                            tiles[i].localScale = Vector3.Lerp(Vector3.one * 0.1f, originalScales[i], easeT);
                        }
                    }
                    yield return null;
                }

                // 최종 위치/스케일 확정
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i] != null)
                    {
                        tiles[i].position = originalPositions[i];
                        tiles[i].localScale = originalScales[i];
                    }
                }
            }

            // 폭발 완료 콜백
            onExplodeComplete?.Invoke();

            Debug.Log("[EffectManager] BlackHole Effect - FINISHED");
        }

        /// <summary>
        /// BlackHole 이펙트 총 시간 반환
        /// </summary>
        public float GetBlackHoleEffectDuration()
        {
            return blackHoleAppearDuration + 0.1f + blackHoleSuckDuration + blackHoleExplodeDuration;
        }

        #endregion

        #region Boom Effect

        /// <summary>
        /// Boom 아이템 이펙트 재생
        /// </summary>
        /// <param name="tilePositions">폭발할 타일 위치들</param>
        /// <param name="onAllExplodeComplete">모든 폭발 완료 후 콜백</param>
        public void PlayBoomEffect(List<Vector3> tilePositions, Action onAllExplodeComplete)
        {
            StartCoroutine(BoomEffectCoroutine(tilePositions, onAllExplodeComplete));
        }

        private IEnumerator BoomEffectCoroutine(List<Vector3> tilePositions, Action onAllExplodeComplete)
        {
            Debug.Log($"[EffectManager] Boom Effect - {tilePositions.Count} explosions");

            // 순차적으로 폭발
            foreach (var pos in tilePositions)
            {
                if (bombExplodeEffectPrefab != null)
                {
                    GameObject explodeEffect = Instantiate(bombExplodeEffectPrefab, pos, Quaternion.identity);
                    AutoDestroyEffect(explodeEffect, bombExplodeDuration + 1f);
                }

                yield return new WaitForSeconds(bombExplodeDelay);
            }

            // 폭발 후 타일 사라지기까지 딜레이 (조정 가능)
            yield return new WaitForSeconds(bombRemoveDelay);

            // 모든 폭발 완료 콜백
            onAllExplodeComplete?.Invoke();

            Debug.Log("[EffectManager] Boom Effect - FINISHED");
        }

        /// <summary>
        /// Boom 이펙트 예상 시간 반환
        /// </summary>
        public float GetBoomEffectDuration(int explosionCount)
        {
            return (explosionCount * bombExplodeDelay) + bombExplodeDuration;
        }

        #endregion

        #region Utility

        private void AutoDestroyEffect(GameObject effectObj, float defaultDuration)
        {
            if (effectObj == null) return;

            var ps = effectObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effectObj, ps.main.duration + ps.main.startLifetime.constantMax + 1f);
            }
            else
            {
                Destroy(effectObj, defaultDuration);
            }
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInQuad(float t)
        {
            return t * t;
        }

        public void ResetEffectStates()
        {
            isClearEffectPlaying = false;
            isGameOverEffectPlaying = false;
            StopAllCoroutines();
        }

        #endregion
    }
}
