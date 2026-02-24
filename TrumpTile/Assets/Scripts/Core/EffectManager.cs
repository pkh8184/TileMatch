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
        [SerializeField] private GameObject mMatchEffectPrefab;
        [SerializeField] private float mMatchEffectDuration = 0.5F;
        [SerializeField] private Color[] mSuitColors = new Color[]
        {
            new Color(0.2F, 0.2F, 0.3F, 1F),    // Spade
            new Color(0.9F, 0.2F, 0.3F, 1F),    // Heart
            new Color(0.9F, 0.2F, 0.3F, 1F),    // Diamond
            new Color(0.2F, 0.2F, 0.3F, 1F)     // Club
        };

        [Header("Particle Settings")]
        [SerializeField] private int mParticleCount = 12;
        [SerializeField] private float mParticleSpeed = 5F;

        [Header("Screen Flash")]
        [SerializeField] private bool mEnableScreenFlash = true;
        [SerializeField] private CanvasGroup mScreenFlashGroup;
        [SerializeField] private float mFlashDuration = 0.1F;
        [SerializeField] private float mFlashIntensity = 0.3F;

        #endregion

        #region Victory/GameOver Effect Settings

        [Header("Victory Effect")]
        [SerializeField] private GameObject mVictoryEffectPrefab;
        [SerializeField] private Transform mVictoryEffectSpawnPoint;
        [SerializeField] private int mVictoryEffectCount = 5;
        [SerializeField] private float mVictoryEffectDelay = 0.2F;
        [SerializeField] private Vector2 mVictorySpawnRangeX = new Vector2(-3F, 3F);
        [SerializeField] private Vector2 mVictorySpawnRangeY = new Vector2(-2F, 2F);

        [Header("Game Over Effect")]
        [SerializeField] private GameObject mGameOverEffectPrefab;

        #endregion

        #region Item Effect Settings

        [Header("=== Item Effects ===")]

        [Header("Strike Effect")]
        [Tooltip("Fx_Strike_001 - 슬롯에서 터지는 이펙트")]
        [SerializeField] private GameObject mStrikePopEffectPrefab;
        [Tooltip("Fx_Strike_002 - 보드 착지 이펙트")]
        [SerializeField] private GameObject mStrikeLandEffectPrefab;
        [SerializeField] private float mStrikePopDuration = 0.3F;
        [SerializeField] private float mStrikeLandDuration = 0.2F;

        [Header("BlackHole Effect")]
        [Tooltip("Fx_BlackHole_001 - 블랙홀 등장 이펙트")]
        [SerializeField] private GameObject mBlackHoleAppearEffectPrefab;
        [Tooltip("Fx_BlackHole_002 - 블랙홀 폭발 이펙트")]
        [SerializeField] private GameObject mBlackHoleExplodeEffectPrefab;
        [SerializeField] private float mBlackHoleAppearDuration = 0.3F;
        [SerializeField] private float mBlackHoleSuckDuration = 0.5F;
        [Tooltip("블랙홀 폭발 이펙트 지속 시간")]
        [SerializeField] private float mBlackHoleExplodeDuration = 0.3F;
        [Tooltip("폭발 후 타일이 튀어나가기 시작하는 딜레이")]
        [SerializeField] private float mBlackHoleScatterDelay = 0.1F;
        [Tooltip("타일이 원위치로 돌아가는 시간")]
        [SerializeField] private float mBlackHoleScatterDuration = 0.4F;
        [Tooltip("각 타일의 도착 시간 랜덤 범위 (최소)")]
        [SerializeField] private float mBlackHoleScatterRandomMin = 0.01F;
        [Tooltip("각 타일의 도착 시간 랜덤 범위 (최대)")]
        [SerializeField] private float mBlackHoleScatterRandomMax = 0.15F;
        [SerializeField] private Vector3 mBlackHolePosition = Vector3.zero;

        [Header("Boom Effect")]
        [Tooltip("Fx_Bomb_001 - 폭탄 폭발 이펙트")]
        [SerializeField] private GameObject mBombExplodeEffectPrefab;
        [SerializeField] private float mBombExplodeDelay = 0.15F;
        [SerializeField] private float mBombExplodeDuration = 0.3F;
        [Tooltip("폭발 후 타일 사라지기까지 딜레이")]
        [SerializeField] private float mBombRemoveDelay = 0.1F;

        #endregion

        #region Private Fields

        private Queue<GameObject> mEffectPool = new Queue<GameObject>();
        private const int POOL_SIZE = 20;

        private Sprite mSharedCircleSprite;
        private Texture2D mCircleTexture;

        private bool mIsClearEffectPlaying = false;
        private bool mIsGameOverEffectPlaying = false;

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

        private void OnDestroy()
        {
            if (mCircleTexture != null)
            {
                Destroy(mCircleTexture);
                mCircleTexture = null;
            }
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Pool Management

        private void InitializePool()
        {
            mSharedCircleSprite = CreateCircleSprite();

            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject obj = new GameObject("EffectParticle");
                obj.transform.SetParent(transform);

                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = mSharedCircleSprite;
                sr.sortingOrder = 200;

                obj.SetActive(false);
                mEffectPool.Enqueue(obj);
            }
        }

        private Sprite CreateCircleSprite()
        {
            int size = 32;
            mCircleTexture = new Texture2D(size, size);
            Color[] colors = new Color[size * size];

            float center = size / 2F;
            float radius = size / 2F - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    colors[y * size + x] = dist < radius ? Color.white : Color.clear;
                }
            }

            mCircleTexture.SetPixels(colors);
            mCircleTexture.Apply();

            return Sprite.Create(mCircleTexture, new Rect(0, 0, size, size), Vector2.one * 0.5F, size);
        }

        private GameObject GetFromPool()
        {
            if (mEffectPool.Count > 0)
            {
                GameObject obj = mEffectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            GameObject newObj = new GameObject("EffectParticle");
            newObj.transform.SetParent(transform);
            SpriteRenderer sr = newObj.AddComponent<SpriteRenderer>();
            sr.sprite = mSharedCircleSprite;
            sr.sortingOrder = 200;
            return newObj;
        }

        private void ReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                obj.transform.localScale = Vector3.one * 0.2F;
                mEffectPool.Enqueue(obj);
            }
        }

        #endregion

        #region Match Effect

        public void PlayMatchEffect(Vector3 position, int suitIndex = 0, int comboLevel = 1)
        {
            Debug.Log($"[EffectManager] PlayMatchEffect at {position}");

            if (mMatchEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(mMatchEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effectObj, mMatchEffectDuration + 1F);
            }

            if (mEnableScreenFlash && mScreenFlashGroup != null && comboLevel > 1)
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
                GameObject particle = GetFromPool();
                if (particle == null) continue;

                particle.transform.position = position;
                particle.transform.localScale = Vector3.one * 0.2F;

                SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = color;

                float angle = (360F / count) * i + UnityEngine.Random.Range(-15F, 15F);
                Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
                dir *= mParticleSpeed * UnityEngine.Random.Range(0.8F, 1.2F);

                particles.Add(particle);
                directions.Add(dir);
            }

            float elapsed = 0F;
            while (elapsed < mMatchEffectDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / mMatchEffectDuration;

                for (int i = 0; i < particles.Count; i++)
                {
                    if (particles[i] == null) continue;

                    particles[i].transform.position += directions[i] * Time.deltaTime;
                    particles[i].transform.localScale = Vector3.one * 0.2F * (1F - t);

                    SpriteRenderer sr = particles[i].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1F - t;
                        sr.color = c;
                    }
                }

                yield return null;
            }

            foreach (GameObject particle in particles)
            {
                ReturnToPool(particle);
            }
        }

        private IEnumerator DoScreenFlash(Color color)
        {
            UnityEngine.UI.Image image = mScreenFlashGroup.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = new Color(color.r, color.g, color.b, mFlashIntensity);
            }

            mScreenFlashGroup.alpha = mFlashIntensity;

            yield return new WaitForSeconds(mFlashDuration);

            float elapsed = 0F;
            while (elapsed < mFlashDuration)
            {
                elapsed += Time.deltaTime;
                mScreenFlashGroup.alpha = mFlashIntensity * (1F - elapsed / mFlashDuration);
                yield return null;
            }

            mScreenFlashGroup.alpha = 0F;
        }

        #endregion

        #region Victory/GameOver Effects

        public void PlayClearEffect()
        {
            if (mIsClearEffectPlaying)
            {
                Debug.Log("[EffectManager] PlayClearEffect - BLOCKED");
                return;
            }

            Debug.Log("[EffectManager] PlayClearEffect - STARTING");
            mIsClearEffectPlaying = true;
            StartCoroutine(ClearCelebration());
        }

        private IEnumerator ClearCelebration()
        {
            if (mVictoryEffectPrefab != null)
            {
                for (int i = 0; i < mVictoryEffectCount; i++)
                {
                    Vector3 spawnPos;

                    if (mVictoryEffectSpawnPoint != null)
                    {
                        spawnPos = mVictoryEffectSpawnPoint.position;
                    }
                    else
                    {
                        spawnPos = new Vector3(
                            UnityEngine.Random.Range(mVictorySpawnRangeX.x, mVictorySpawnRangeX.y),
                            UnityEngine.Random.Range(mVictorySpawnRangeY.x, mVictorySpawnRangeY.y),
                            0F
                        );
                    }

                    spawnPos += new Vector3(UnityEngine.Random.Range(-0.5F, 0.5F), UnityEngine.Random.Range(-0.5F, 0.5F), 0F);

                    GameObject effectObj = Instantiate(mVictoryEffectPrefab, spawnPos, Quaternion.identity);
                    AutoDestroyEffect(effectObj, 3F);

                    yield return new WaitForSeconds(mVictoryEffectDelay);
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 randomPos = new Vector3(
                        UnityEngine.Random.Range(-3F, 3F),
                        UnityEngine.Random.Range(-2F, 2F),
                        0F
                    );

                    int randomColor = UnityEngine.Random.Range(0, mSuitColors.Length);
                    StartCoroutine(SpawnMatchParticles(randomPos, mSuitColors[randomColor], mParticleCount));

                    yield return new WaitForSeconds(0.15F);
                }
            }

            yield return new WaitForSeconds(1F);
            mIsClearEffectPlaying = false;
            Debug.Log("[EffectManager] ClearEffect - FINISHED");
        }

        public void PlayGameOverEffect()
        {
            if (mIsGameOverEffectPlaying)
            {
                Debug.Log("[EffectManager] PlayGameOverEffect - BLOCKED");
                return;
            }

            Debug.Log("[EffectManager] PlayGameOverEffect - STARTING");
            mIsGameOverEffectPlaying = true;
            StartCoroutine(GameOverEffectCoroutine());
        }

        private IEnumerator GameOverEffectCoroutine()
        {
            if (mGameOverEffectPrefab != null)
            {
                Vector3 spawnPos = Camera.main != null
                    ? Camera.main.transform.position + Vector3.forward * 5F
                    : Vector3.zero;
                spawnPos.z = 0F;

                GameObject effectObj = Instantiate(mGameOverEffectPrefab, spawnPos, Quaternion.identity);
                AutoDestroyEffect(effectObj, 2F);
            }

            if (mScreenFlashGroup != null)
            {
                yield return StartCoroutine(DoScreenFlash(Color.red));
            }

            yield return new WaitForSeconds(0.5F);
            mIsGameOverEffectPlaying = false;
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
            if (mStrikePopEffectPrefab != null)
            {
                GameObject popEffect = Instantiate(mStrikePopEffectPrefab, slotPosition, Quaternion.identity);
                AutoDestroyEffect(popEffect, mStrikePopDuration + 1F);
            }

            yield return new WaitForSeconds(mStrikePopDuration);

            // 터짐 완료 콜백 (타일 이동 시작, 착지 위치 콜백 받기)
            Vector3 landPosition = Vector3.zero;
            bool bLandPositionReceived = false;

            onPopComplete?.Invoke((pos) =>
            {
                landPosition = pos;
                bLandPositionReceived = true;
            });

            // 타일 이동 시간 대기
            yield return new WaitForSeconds(0.2F);

            // 착지 위치를 못 받았으면 대기
            float waitTime = 0F;
            while (!bLandPositionReceived && waitTime < 1F)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[EffectManager] Strike Effect - Land at {landPosition}");

            // 2. 보드 착지 이펙트
            if (mStrikeLandEffectPrefab != null)
            {
                GameObject landEffect = Instantiate(mStrikeLandEffectPrefab, landPosition, Quaternion.identity);
                AutoDestroyEffect(landEffect, mStrikeLandDuration + 1F);
            }

            yield return new WaitForSeconds(mStrikeLandDuration);

            // 착지 완료 콜백
            onLandComplete?.Invoke();

            Debug.Log("[EffectManager] Strike Effect - FINISHED");
        }

        /// <summary>
        /// Strike 이펙트 (간단 버전 - 위치 직접 지정)
        /// </summary>
        public void PlayStrikePopEffect(Vector3 position)
        {
            if (mStrikePopEffectPrefab != null)
            {
                GameObject effect = Instantiate(mStrikePopEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effect, mStrikePopDuration + 1F);
            }
        }

        public void PlayStrikeLandEffect(Vector3 position)
        {
            if (mStrikeLandEffectPrefab != null)
            {
                GameObject effect = Instantiate(mStrikeLandEffectPrefab, position, Quaternion.identity);
                AutoDestroyEffect(effect, mStrikeLandDuration + 1F);
            }
        }

        /// <summary>
        /// Strike 이펙트 총 시간 반환
        /// </summary>
        public float GetStrikeEffectDuration()
        {
            return mStrikePopDuration + 0.2F + mStrikeLandDuration;
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
            Debug.Log($"[EffectManager] BlackHole Effect - Start at {mBlackHolePosition}");

            // 원본 위치/스케일 저장
            List<Vector3> originalPositions = new List<Vector3>();
            List<Vector3> originalScales = new List<Vector3>();
            foreach (Transform tile in tiles)
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
            if (mBlackHoleAppearEffectPrefab != null)
            {
                blackHoleObj = Instantiate(mBlackHoleAppearEffectPrefab, mBlackHolePosition, Quaternion.identity);

                blackHoleObj.transform.localScale = Vector3.zero;
                float elapsed = 0F;
                while (elapsed < mBlackHoleAppearDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / mBlackHoleAppearDuration;
                    float scale = Mathf.Lerp(0F, 1F, EaseOutBack(t));
                    blackHoleObj.transform.localScale = Vector3.one * scale;
                    yield return null;
                }
                blackHoleObj.transform.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(0.1F);

            // 2. 타일들 블랙홀로 흡수 (각 타일 도착 시간 랜덤)
            if (tiles != null && tiles.Count > 0)
            {
                List<float> randomDelays = new List<float>();
                foreach (Transform tile in tiles)
                {
                    randomDelays.Add(UnityEngine.Random.Range(mBlackHoleScatterRandomMin, mBlackHoleScatterRandomMax));
                }

                float suckElapsed = 0F;
                float maxSuckTime = mBlackHoleSuckDuration + mBlackHoleScatterRandomMax;

                while (suckElapsed < maxSuckTime)
                {
                    suckElapsed += Time.deltaTime;

                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            float tileElapsed = suckElapsed - randomDelays[i];
                            if (tileElapsed < 0F) tileElapsed = 0F;

                            float t = Mathf.Clamp01(tileElapsed / mBlackHoleSuckDuration);
                            float easeT = EaseInQuad(t);

                            tiles[i].position = Vector3.Lerp(originalPositions[i], mBlackHolePosition, easeT);
                            tiles[i].localScale = Vector3.Lerp(originalScales[i], Vector3.one * 0.1F, easeT);
                        }
                    }
                    yield return null;
                }

                // 모든 타일을 블랙홀 중심으로 확실히 이동
                foreach (Transform tile in tiles)
                {
                    if (tile != null)
                    {
                        tile.position = mBlackHolePosition;
                        tile.localScale = Vector3.one * 0.1F;
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

            if (mBlackHoleExplodeEffectPrefab != null)
            {
                GameObject explodeEffect = Instantiate(mBlackHoleExplodeEffectPrefab, mBlackHolePosition, Quaternion.identity);
                AutoDestroyEffect(explodeEffect, mBlackHoleExplodeDuration + 1F);
            }

            // 폭발 후 딜레이
            yield return new WaitForSeconds(mBlackHoleScatterDelay);

            // 4. 타일들 원위치로 흩어지는 애니메이션
            if (tiles != null && tiles.Count > 0)
            {
                List<float> scatterDelays = new List<float>();
                foreach (Transform tile in tiles)
                {
                    scatterDelays.Add(UnityEngine.Random.Range(mBlackHoleScatterRandomMin, mBlackHoleScatterRandomMax));
                }

                float scatterElapsed = 0F;
                float maxScatterTime = mBlackHoleScatterDuration + mBlackHoleScatterRandomMax;

                while (scatterElapsed < maxScatterTime)
                {
                    scatterElapsed += Time.deltaTime;

                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i] != null)
                        {
                            float tileElapsed = scatterElapsed - scatterDelays[i];
                            if (tileElapsed < 0F) tileElapsed = 0F;

                            float t = Mathf.Clamp01(tileElapsed / mBlackHoleScatterDuration);
                            float easeT = EaseOutBack(t);

                            tiles[i].position = Vector3.Lerp(mBlackHolePosition, originalPositions[i], easeT);
                            tiles[i].localScale = Vector3.Lerp(Vector3.one * 0.1F, originalScales[i], easeT);
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
            return mBlackHoleAppearDuration + 0.1F + mBlackHoleSuckDuration + mBlackHoleExplodeDuration;
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
            foreach (Vector3 pos in tilePositions)
            {
                if (mBombExplodeEffectPrefab != null)
                {
                    GameObject explodeEffect = Instantiate(mBombExplodeEffectPrefab, pos, Quaternion.identity);
                    AutoDestroyEffect(explodeEffect, mBombExplodeDuration + 1F);
                }

                yield return new WaitForSeconds(mBombExplodeDelay);
            }

            // 폭발 후 타일 사라지기까지 딜레이 (조정 가능)
            yield return new WaitForSeconds(mBombRemoveDelay);

            // 모든 폭발 완료 콜백
            onAllExplodeComplete?.Invoke();

            Debug.Log("[EffectManager] Boom Effect - FINISHED");
        }

        /// <summary>
        /// Boom 이펙트 예상 시간 반환
        /// </summary>
        public float GetBoomEffectDuration(int explosionCount)
        {
            return (explosionCount * mBombExplodeDelay) + mBombExplodeDuration;
        }

        #endregion

        #region Utility

        private void AutoDestroyEffect(GameObject effectObj, float defaultDuration)
        {
            if (effectObj == null) return;

            ParticleSystem ps = effectObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(effectObj, ps.main.duration + ps.main.startLifetime.constantMax + 1F);
            }
            else
            {
                Destroy(effectObj, defaultDuration);
            }
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158F;
            float c3 = c1 + 1F;
            return 1F + c3 * Mathf.Pow(t - 1F, 3F) + c1 * Mathf.Pow(t - 1F, 2F);
        }

        private float EaseInQuad(float t)
        {
            return t * t;
        }

        public void ResetEffectStates()
        {
            mIsClearEffectPlaying = false;
            mIsGameOverEffectPlaying = false;
            StopAllCoroutines();
        }

        #endregion
    }
}
