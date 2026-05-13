using TrumpTile.FrameLibrary;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 오디오 시스템 파사드
	/// - EventManager의 AUDIO_* 이벤트를 구독해 BGMController / SFXController에 위임
	/// - SettingsManager에서 직접 볼륨/활성화 설정 시 접근 가능한 공개 API 유지
	/// </summary>
	public class AudioManager : Singleton_GameObject<AudioManager>
	{
		[Header("Controllers")]
		[SerializeField] private BGMController mBgmController;
		[SerializeField] private SFXController mSfxController;

		[Header("Database")]
		[SerializeField] private AudioDatabase mAudioDatabase;

		[Header("BGM AudioSource")]
		[SerializeField] private AudioSource mBgmSource;

		[Header("SFX Pool")]
		[SerializeField] private int mSfxPoolSize = 10;

		[Header("Volume Settings")]
		[SerializeField] private float mBgmVolume = 0.5F;
		[SerializeField] private float mSfxVolume = 1F;

		// PlayerPrefs 키
		private const string BGM_VOLUME_KEY = "BGMVolume";
		private const string SFX_VOLUME_KEY = "SFXVolume";
		private const string BGM_ENABLED_KEY = "BGMEnabled";
		private const string SFX_ENABLED_KEY = "SFXEnabled";

		private bool mBBgmEnabled = true;
		private bool mBSfxEnabled = true;
		private bool mBIsMuted = false;

		public bool BGMEnabled => mBBgmEnabled;
		public bool SFXEnabled => mBSfxEnabled;
		public float BGMVolume => mBgmVolume;
		public float SFXVolume => mSfxVolume;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
			InitializeControllers();
			LoadSettings();
			SubscribeEvents();
		}

		private void OnDestroy()
		{
			UnsubscribeEvents();
		}

		#region 초기화

		private void InitializeControllers()
		{
			// BGMController
			if (mBgmController == null)
			{
				mBgmController = gameObject.AddComponent<BGMController>();
			}
			if (mBgmSource == null)
			{
				mBgmSource = gameObject.AddComponent<AudioSource>();
			}
			mBgmController.Initialize(mBgmSource);

			// SFXController
			if (mSfxController == null)
			{
				mSfxController = gameObject.AddComponent<SFXController>();
			}
			mSfxController.Initialize(mSfxPoolSize);

			// AudioDatabase
			if (mAudioDatabase != null)
			{
				mAudioDatabase.Initialize();
			}
		}

		private void LoadSettings()
		{
			mBgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5F);
			mSfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1F);
			mBBgmEnabled = PlayerPrefs.GetInt(BGM_ENABLED_KEY, 1) == 1;
			mBSfxEnabled = PlayerPrefs.GetInt(SFX_ENABLED_KEY, 1) == 1;

			ApplySettingsToControllers();
		}

		private void ApplySettingsToControllers()
		{
			mBgmController.SetVolume(mBgmVolume);
			mBgmController.SetEnabled(mBBgmEnabled);
			mBgmController.SetMuted(mBIsMuted);

			mSfxController.SetVolume(mSfxVolume);
			mSfxController.SetEnabled(mBSfxEnabled);
			mSfxController.SetMuted(mBIsMuted);
		}

		#endregion

		#region 이벤트 구독

		private void SubscribeEvents()
		{
			EventManager.Inst.AddEvent<AudioEventPayload>(EventKeys.AUDIO_PLAY, OnAudioPlay);
			EventManager.Inst.AddEvent<AudioEventPayload>(EventKeys.AUDIO_PAUSE, OnAudioPause);
			EventManager.Inst.AddEvent<AudioEventPayload>(EventKeys.AUDIO_STOP, OnAudioStop);
			EventManager.Inst.AddEvent<AudioEventPayload>(EventKeys.AUDIO_RESUME, OnAudioResume);
		}

		private void UnsubscribeEvents()
		{
			if (EventManager.Inst == null)
			{
				return;
			}
			EventManager.Inst.RemoveEvent<AudioEventPayload>(EventKeys.AUDIO_PLAY, OnAudioPlay);
			EventManager.Inst.RemoveEvent<AudioEventPayload>(EventKeys.AUDIO_PAUSE, OnAudioPause);
			EventManager.Inst.RemoveEvent<AudioEventPayload>(EventKeys.AUDIO_STOP, OnAudioStop);
			EventManager.Inst.RemoveEvent<AudioEventPayload>(EventKeys.AUDIO_RESUME, OnAudioResume);
		}

		private void OnAudioPlay(AudioEventPayload payload)
		{
			bool bIsBgm = payload.Key == EAudioKey.BGM_Main || payload.Key == EAudioKey.BGM_Ingame;
			if (bIsBgm)
			{
				AudioClip clip = GetClip(payload.Key);
				mBgmController.Play(clip);
			}
			else
			{
				AudioClip clip = GetClip(payload.Key);
				bool bRandomPitch = payload.Key == EAudioKey.SFX_TileMove;
				mSfxController.Play(clip, 1F, bRandomPitch);
			}
		}

		private void OnAudioPause(AudioEventPayload payload)
		{
			mBgmController.Pause();
		}

		private void OnAudioStop(AudioEventPayload payload)
		{
			bool bIsBgm = payload.Key == EAudioKey.BGM_Main || payload.Key == EAudioKey.BGM_Ingame;
			if (bIsBgm)
			{
				mBgmController.Stop();
			}
			else
			{
				mSfxController.StopAll();
			}
		}

		private void OnAudioResume(AudioEventPayload payload)
		{
			mBgmController.Resume();
		}

		#endregion

		#region 설정 API (SettingsManager에서 직접 호출)

		public void SetBGMEnabled(bool bEnabled)
		{
			mBBgmEnabled = bEnabled;
			mBgmController.SetEnabled(bEnabled);
			PlayerPrefs.SetInt(BGM_ENABLED_KEY, bEnabled ? 1 : 0);
		}

		public void SetSFXEnabled(bool bEnabled)
		{
			mBSfxEnabled = bEnabled;
			mSfxController.SetEnabled(bEnabled);
			PlayerPrefs.SetInt(SFX_ENABLED_KEY, bEnabled ? 1 : 0);
		}

		public void SetBGMVolume(float volume)
		{
			mBgmVolume = Mathf.Clamp01(volume);
			mBgmController.SetVolume(mBgmVolume);
			PlayerPrefs.SetFloat(BGM_VOLUME_KEY, mBgmVolume);
		}

		public void SetSFXVolume(float volume)
		{
			mSfxVolume = Mathf.Clamp01(volume);
			mSfxController.SetVolume(mSfxVolume);
			PlayerPrefs.SetFloat(SFX_VOLUME_KEY, mSfxVolume);
		}

		public void ToggleMute()
		{
			mBIsMuted = !mBIsMuted;
			mBgmController.SetMuted(mBIsMuted);
			mSfxController.SetMuted(mBIsMuted);
		}

		#endregion

		#region 동적 클립 직접 재생 (AudioDatabase 밖의 클립용)

		public void PlaySFX(AudioClip clip, float volumeMultiplier = 1F)
		{
			mSfxController.Play(clip, volumeMultiplier);
		}

		#endregion

		#region 내부 유틸

		private AudioClip GetClip(EAudioKey key)
		{
			if (mAudioDatabase == null)
			{
				return null;
			}
			return mAudioDatabase.GetClip(key);
		}

		#endregion
	}
}
