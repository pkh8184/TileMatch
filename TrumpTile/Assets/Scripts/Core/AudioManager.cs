using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TrumpTile.Audio
{
    /// <summary>
    /// BGM 및 효과음 관리
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private int sfxPoolSize = 10;

        [Header("BGM")]
        [SerializeField] private AudioClip mainMenuBGM;
        [SerializeField] private AudioClip gamePlayBGM;
        [SerializeField] private float bgmVolume = 0.5f;
        [SerializeField] private float bgmFadeDuration = 1f;

        [Header("SFX - Tile")]
        [SerializeField] private AudioClip tileSelectSound;
        [SerializeField] private AudioClip tileMoveSound;
        [SerializeField] private AudioClip tileMatchSound;
        [SerializeField] private AudioClip[] comboSounds; // 콤보별 다른 사운드

        [Header("SFX - UI")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip popupOpenSound;
        [SerializeField] private AudioClip popupCloseSound;

        [Header("SFX - Game")]
        [SerializeField] private AudioClip gameClearSound;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip starSound;
        [SerializeField] private AudioClip itemUseSound;
        [SerializeField] private AudioClip shuffleSound;
        [SerializeField] private AudioClip undoSound;
        [SerializeField] private AudioClip hintSound;

        [Header("SFX - Special")]
        [SerializeField] private AudioClip warningSound; // 슬롯 거의 찼을 때
        [SerializeField] private AudioClip errorSound;   // 잘못된 동작

        [Header("Volume Settings")]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float pitchVariation = 0.1f;

        // SFX 풀
        private List<AudioSource> sfxPool = new List<AudioSource>();
        private int currentSfxIndex = 0;

        // 볼륨 설정 키
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string MUTE_KEY = "AudioMuted";

        private bool isMuted;
        private Coroutine bgmFadeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            // BGM Source 설정
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            // SFX Source 풀 생성
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            // 저장된 볼륨 로드
            LoadVolumeSettings();
        }

        #region Volume Control

        /// <summary>
        /// BGM 볼륨 설정
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmSource != null && !isMuted)
            {
                bgmSource.volume = bgmVolume;
            }
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        }

        /// <summary>
        /// 음소거 토글
        /// </summary>
        public void ToggleMute()
        {
            isMuted = !isMuted;
            PlayerPrefs.SetInt(MUTE_KEY, isMuted ? 1 : 0);

            if (bgmSource != null)
            {
                bgmSource.volume = isMuted ? 0f : bgmVolume;
            }
        }

        public bool IsMuted => isMuted;
        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;

        private void LoadVolumeSettings()
        {
            bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            isMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

            if (bgmSource != null)
            {
                bgmSource.volume = isMuted ? 0f : bgmVolume;
            }
        }

        #endregion

        #region BGM

        /// <summary>
        /// 메인 메뉴 BGM 재생
        /// </summary>
        public void PlayMainMenuBGM()
        {
            PlayBGM(mainMenuBGM);
        }

        /// <summary>
        /// 게임 플레이 BGM 재생
        /// </summary>
        public void PlayGameBGM()
        {
            PlayBGM(gamePlayBGM);
        }

        /// <summary>
        /// BGM 재생 (페이드 인)
        /// </summary>
        public void PlayBGM(AudioClip clip, bool fade = true)
        {
            if (clip == null || bgmSource == null) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            if (fade && bgmSource.isPlaying)
            {
                if (bgmFadeCoroutine != null)
                    StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clip));
            }
            else
            {
                bgmSource.clip = clip;
                bgmSource.volume = isMuted ? 0f : bgmVolume;
                bgmSource.Play();
            }
        }

        private IEnumerator CrossFadeBGM(AudioClip newClip)
        {
            // 페이드 아웃
            float elapsed = 0f;
            float startVolume = bgmSource.volume;

            while (elapsed < bgmFadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (bgmFadeDuration / 2f));
                yield return null;
            }

            // 클립 변경
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.Play();

            // 페이드 인
            elapsed = 0f;
            float targetVolume = isMuted ? 0f : bgmVolume;

            while (elapsed < bgmFadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (bgmFadeDuration / 2f));
                yield return null;
            }

            bgmSource.volume = targetVolume;
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM(bool fade = true)
        {
            if (bgmSource == null) return;

            if (fade)
            {
                if (bgmFadeCoroutine != null)
                    StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
            }
            else
            {
                bgmSource.Stop();
            }
        }

        private IEnumerator FadeOutBGM()
        {
            float elapsed = 0f;
            float startVolume = bgmSource.volume;

            while (elapsed < bgmFadeDuration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / bgmFadeDuration);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = isMuted ? 0f : bgmVolume;
        }

        /// <summary>
        /// BGM 일시정지
        /// </summary>
        public void PauseBGM()
        {
            if (bgmSource != null)
                bgmSource.Pause();
        }

        /// <summary>
        /// BGM 재개
        /// </summary>
        public void ResumeBGM()
        {
            if (bgmSource != null)
                bgmSource.UnPause();
        }

        #endregion

        #region SFX

        /// <summary>
        /// 효과음 재생
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f, bool randomPitch = false)
        {
            if (clip == null || isMuted) return;

            var source = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = sfxVolume * volumeMultiplier;

            if (randomPitch)
            {
                source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            }
            else
            {
                source.pitch = 1f;
            }

            source.Play();
        }

        private AudioSource GetAvailableSFXSource()
        {
            var source = sfxPool[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxPool.Count;
            return source;
        }

        // 타일 사운드
        public void PlayTileSelect() => PlaySFX(tileSelectSound, 1f, true);
        public void PlayTileMove() => PlaySFX(tileMoveSound, 0.8f, true);

        /// <summary>
        /// 매칭 사운드 (콤보에 따라 다른 사운드)
        /// </summary>
        public void PlayMatchSound(int comboCount = 1)
        {
            if (comboSounds != null && comboSounds.Length > 0)
            {
                int index = Mathf.Clamp(comboCount - 1, 0, comboSounds.Length - 1);
                PlaySFX(comboSounds[index], 1f, false);
            }
            else
            {
                PlaySFX(tileMatchSound, 1f, false);
            }
        }

        // UI 사운드
        public void PlayButtonClick() => PlaySFX(buttonClickSound);
        public void PlayPopupOpen() => PlaySFX(popupOpenSound);
        public void PlayPopupClose() => PlaySFX(popupCloseSound);

        // 게임 사운드
        public void PlayGameClear() => PlaySFX(gameClearSound);
        public void PlayGameOver() => PlaySFX(gameOverSound);
        public void PlayStar() => PlaySFX(starSound, 1f, true);
        public void PlayItemUse() => PlaySFX(itemUseSound);
        public void PlayShuffle() => PlaySFX(shuffleSound);
        public void PlayUndo() => PlaySFX(undoSound);
        public void PlayHint() => PlaySFX(hintSound);

        // 특수 사운드
        public void PlayWarning() => PlaySFX(warningSound);
        public void PlayError() => PlaySFX(errorSound);

        #endregion

        #region Utility

        /// <summary>
        /// 연속 사운드 재생 (슬롯 채워질 때 음계)
        /// </summary>
        public void PlaySequentialSound(AudioClip baseClip, int index, int total)
        {
            if (baseClip == null || isMuted) return;

            var source = GetAvailableSFXSource();
            source.clip = baseClip;
            source.volume = sfxVolume;

            // 음계 조절 (도레미파솔라시)
            float[] pitches = { 1f, 1.122f, 1.26f, 1.335f, 1.498f, 1.682f, 1.888f };
            int pitchIndex = index % pitches.Length;
            source.pitch = pitches[pitchIndex];

            source.Play();
        }

        /// <summary>
        /// 모든 사운드 정지
        /// </summary>
        public void StopAllSounds()
        {
            if (bgmSource != null)
                bgmSource.Stop();

            foreach (var source in sfxPool)
            {
                if (source != null)
                    source.Stop();
            }
        }

        #endregion
    }
}
