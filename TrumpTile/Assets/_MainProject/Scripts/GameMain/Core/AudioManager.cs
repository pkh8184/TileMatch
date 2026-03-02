using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// BGM 및 효과음 관리
	/// </summary>
	public class AudioManager : MonoBehaviour
	{
		public static AudioManager Instance { get; private set; }

		[Header("Audio Sources")]
		[SerializeField] private AudioSource mBgmSource;
		[SerializeField] private AudioSource mSfxSource;
		[SerializeField] private int mSfxPoolSize = 10;

		[Header("BGM")]
		[SerializeField] private AudioClip mMainMenuBGM;
		[SerializeField] private AudioClip mGamePlayBGM;
		[SerializeField] private float mBgmVolume = 0.5F;
		[SerializeField] private float mBgmFadeDuration = 1F;

		[Header("SFX - Tile")]
		[SerializeField] private AudioClip mTileSelectSound;
		[SerializeField] private AudioClip mTileMoveSound;
		[SerializeField] private AudioClip mTileMatchSound;
		[SerializeField] private AudioClip[] mComboSounds; // 콤보별 다른 사운드

		[Header("SFX - UI")]
		[SerializeField] private AudioClip mButtonClickSound;
		[SerializeField] private AudioClip mPopupOpenSound;
		[SerializeField] private AudioClip mPopupCloseSound;

		[Header("SFX - Game")]
		[SerializeField] private AudioClip mGameClearSound;
		[SerializeField] private AudioClip mGameOverSound;
		[SerializeField] private AudioClip mStarSound;
		[SerializeField] private AudioClip mItemUseSound;
		[SerializeField] private AudioClip mShuffleSound;
		[SerializeField] private AudioClip mUndoSound;
		[SerializeField] private AudioClip mHintSound;

		[Header("SFX - Special")]
		[SerializeField] private AudioClip mWarningSound; // 슬롯 거의 찼을 때
		[SerializeField] private AudioClip mErrorSound;   // 잘못된 동작

		[Header("Volume Settings")]
		[SerializeField] private float mSfxVolume = 1F;
		[SerializeField] private float mPitchVariation = 0.1F;

		// SFX 풀
		private List<AudioSource> mSfxPool = new List<AudioSource>();
		private int mCurrentSfxIndex = 0;

		// 볼륨 설정 키
		private const string BGM_VOLUME_KEY = "BGMVolume";
		private const string SFX_VOLUME_KEY = "SFXVolume";
		private const string MUTE_KEY = "AudioMuted";

		private bool mIsMuted;
		private Coroutine mBgmFadeCoroutine;

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
			if (mBgmSource == null)
			{
				mBgmSource = gameObject.AddComponent<AudioSource>();
			}
			mBgmSource.loop = true;
			mBgmSource.playOnAwake = false;

			// SFX Source 풀 생성
			for (int i = 0; i < mSfxPoolSize; i++)
			{
				AudioSource source = gameObject.AddComponent<AudioSource>();
				source.playOnAwake = false;
				mSfxPool.Add(source);
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
			mBgmVolume = Mathf.Clamp01(volume);
			if (mBgmSource != null && !mIsMuted)
			{
				mBgmSource.volume = mBgmVolume;
			}
			PlayerPrefs.SetFloat(BGM_VOLUME_KEY, mBgmVolume);
		}

		/// <summary>
		/// SFX 볼륨 설정
		/// </summary>
		public void SetSFXVolume(float volume)
		{
			mSfxVolume = Mathf.Clamp01(volume);
			PlayerPrefs.SetFloat(SFX_VOLUME_KEY, mSfxVolume);
		}

		/// <summary>
		/// 음소거 토글
		/// </summary>
		public void ToggleMute()
		{
			mIsMuted = !mIsMuted;
			PlayerPrefs.SetInt(MUTE_KEY, mIsMuted ? 1 : 0);

			if (mBgmSource != null)
			{
				mBgmSource.volume = mIsMuted ? 0F : mBgmVolume;
			}
		}

		public bool IsMuted => mIsMuted;
		public float BGMVolume => mBgmVolume;
		public float SFXVolume => mSfxVolume;

		private void LoadVolumeSettings()
		{
			mBgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5F);
			mSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1F);
			mIsMuted = PlayerPrefs.GetInt(MUTE_KEY, 0) == 1;

			if (mBgmSource != null)
			{
				mBgmSource.volume = mIsMuted ? 0F : mBgmVolume;
			}
		}

		#endregion

		#region BGM

		/// <summary>
		/// 메인 메뉴 BGM 재생
		/// </summary>
		public void PlayMainMenuBGM()
		{
			PlayBGM(mMainMenuBGM);
		}

		/// <summary>
		/// 게임 플레이 BGM 재생
		/// </summary>
		public void PlayGameBGM()
		{
			PlayBGM(mGamePlayBGM);
		}

		/// <summary>
		/// BGM 재생 (페이드 인)
		/// </summary>
		public void PlayBGM(AudioClip clip, bool bFade = true)
		{
			if (clip == null || mBgmSource == null) return;

			if (mBgmSource.clip == clip && mBgmSource.isPlaying) return;

			if (bFade && mBgmSource.isPlaying)
			{
				if (mBgmFadeCoroutine != null)
				{
					StopCoroutine(mBgmFadeCoroutine);
				}
				mBgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clip));
			}
			else
			{
				mBgmSource.clip = clip;
				mBgmSource.volume = mIsMuted ? 0F : mBgmVolume;
				mBgmSource.Play();
			}
		}

		private IEnumerator CrossFadeBGM(AudioClip newClip)
		{
			// 페이드 아웃
			float elapsed = 0F;
			float startVolume = mBgmSource.volume;

			while (elapsed < mBgmFadeDuration / 2F)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(startVolume, 0F, elapsed / (mBgmFadeDuration / 2F));
				yield return null;
			}

			// 클립 변경
			mBgmSource.Stop();
			mBgmSource.clip = newClip;
			mBgmSource.Play();

			// 페이드 인
			elapsed = 0F;
			float targetVolume = mIsMuted ? 0F : mBgmVolume;

			while (elapsed < mBgmFadeDuration / 2F)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(0F, targetVolume, elapsed / (mBgmFadeDuration / 2F));
				yield return null;
			}

			mBgmSource.volume = targetVolume;
		}

		/// <summary>
		/// BGM 정지
		/// </summary>
		public void StopBGM(bool bFade = true)
		{
			if (mBgmSource == null) return;

			if (bFade)
			{
				if (mBgmFadeCoroutine != null)
				{
					StopCoroutine(mBgmFadeCoroutine);
				}
				mBgmFadeCoroutine = StartCoroutine(FadeOutBGM());
			}
			else
			{
				mBgmSource.Stop();
			}
		}

		private IEnumerator FadeOutBGM()
		{
			float elapsed = 0F;
			float startVolume = mBgmSource.volume;

			while (elapsed < mBgmFadeDuration)
			{
				elapsed += Time.deltaTime;
				mBgmSource.volume = Mathf.Lerp(startVolume, 0F, elapsed / mBgmFadeDuration);
				yield return null;
			}

			mBgmSource.Stop();
			mBgmSource.volume = mIsMuted ? 0F : mBgmVolume;
		}

		/// <summary>
		/// BGM 일시정지
		/// </summary>
		public void PauseBGM()
		{
			if (mBgmSource != null)
			{
				mBgmSource.Pause();
			}
		}

		/// <summary>
		/// BGM 재개
		/// </summary>
		public void ResumeBGM()
		{
			if (mBgmSource != null)
			{
				mBgmSource.UnPause();
			}
		}

		#endregion

		#region SFX

		/// <summary>
		/// 효과음 재생
		/// </summary>
		public void PlaySFX(AudioClip clip, float volumeMultiplier = 1F, bool bRandomPitch = false)
		{
			if (clip == null || mIsMuted) return;

			AudioSource source = GetAvailableSFXSource();
			source.clip = clip;
			source.volume = mSfxVolume * volumeMultiplier;

			if (bRandomPitch)
			{
				source.pitch = 1F + Random.Range(-mPitchVariation, mPitchVariation);
			}
			else
			{
				source.pitch = 1F;
			}

			source.Play();
		}

		private AudioSource GetAvailableSFXSource()
		{
			AudioSource source = mSfxPool[mCurrentSfxIndex];
			mCurrentSfxIndex = (mCurrentSfxIndex + 1) % mSfxPool.Count;
			return source;
		}

		// 타일 사운드
		public void PlayTileSelect() => PlaySFX(mTileSelectSound, 1F, true);
		public void PlayTileMove() => PlaySFX(mTileMoveSound, 0.8F, true);

		/// <summary>
		/// 매칭 사운드 (콤보에 따라 다른 사운드)
		/// </summary>
		public void PlayMatchSound(int comboCount = 1)
		{
			if (mComboSounds != null && mComboSounds.Length > 0)
			{
				int index = Mathf.Clamp(comboCount - 1, 0, mComboSounds.Length - 1);
				PlaySFX(mComboSounds[index], 1F, false);
			}
			else
			{
				PlaySFX(mTileMatchSound, 1F, false);
			}
		}

		// UI 사운드
		public void PlayButtonClick() => PlaySFX(mButtonClickSound);
		public void PlayPopupOpen() => PlaySFX(mPopupOpenSound);
		public void PlayPopupClose() => PlaySFX(mPopupCloseSound);

		// 게임 사운드
		public void PlayGameClear() => PlaySFX(mGameClearSound);
		public void PlayGameOver() => PlaySFX(mGameOverSound);
		public void PlayStar() => PlaySFX(mStarSound, 1F, true);
		public void PlayItemUse() => PlaySFX(mItemUseSound);
		public void PlayShuffle() => PlaySFX(mShuffleSound);
		public void PlayUndo() => PlaySFX(mUndoSound);
		public void PlayHint() => PlaySFX(mHintSound);

		// 특수 사운드
		public void PlayWarning() => PlaySFX(mWarningSound);
		public void PlayError() => PlaySFX(mErrorSound);

		#endregion

		#region Utility

		/// <summary>
		/// 연속 사운드 재생 (슬롯 채워질 때 음계)
		/// </summary>
		public void PlaySequentialSound(AudioClip baseClip, int index, int total)
		{
			if (baseClip == null || mIsMuted) return;

			AudioSource source = GetAvailableSFXSource();
			source.clip = baseClip;
			source.volume = mSfxVolume;

			// 음계 조절 (도레미파솔라시)
			float[] pitches = { 1F, 1.122F, 1.26F, 1.335F, 1.498F, 1.682F, 1.888F };
			int pitchIndex = index % pitches.Length;
			source.pitch = pitches[pitchIndex];

			source.Play();
		}

		/// <summary>
		/// 모든 사운드 정지
		/// </summary>
		public void StopAllSounds()
		{
			if (mBgmSource != null)
			{
				mBgmSource.Stop();
			}

			foreach (AudioSource source in mSfxPool)
			{
				if (source != null)
				{
					source.Stop();
				}
			}
		}

		#endregion
	}
}
