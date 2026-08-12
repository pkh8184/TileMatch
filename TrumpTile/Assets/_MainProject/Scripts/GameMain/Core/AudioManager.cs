using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
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

		//볼륨/Enabled는 인게임 연출용 런타임 값이라 저장하지 않는다.
		//유저의 BGM/SFX On·Off 설정은 PlayerDataManager(UserData.BGMOn/SFXOn)가 단독으로 소유한다.
		//예전엔 여기서 "BGMVolume" 키에 연출 볼륨(0.2/1)을 덮어썼고, 그 키를 설정 On·Off 판정에 쓰는 바람에
		//BGM을 꺼도 스테이지를 한 번 플레이하면 다음 실행에 다시 켜져 있었다.

		private bool mBBgmEnabled = true;
		private bool mBSfxEnabled = true;
		private bool mBIsMuted = false;

		public bool BGMEnabled => mBBgmEnabled;
		public bool SFXEnabled => mBSfxEnabled;
		public float BGMVolume => mBgmVolume;
		public float SFXVolume => mSfxVolume;

		private void Awake()
		{
			 if (FindObjectsOfType<AudioManager>().Length > 1)
			{
				Destroy(gameObject);
				return;
			}
			DontDestroyOnLoad(gameObject);
			InitializeControllers();
			SubscribeEvents();
		}
        private void Start()
        {
            LoadSettings();
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
			mBgmVolume = 1;
			mSfxVolume = 1;		

			ApplySettingsToControllers();
		}

		private void ApplySettingsToControllers()
		{
			//UserData가 아직 없으면(부팅 순서 문제) 음소거를 켜버리지 않도록 기본 On으로 둔다.
			bool bBgmOn = true;
			bool bSfxOn = true;
			if(PlayerDataManager.Inst != null && PlayerDataManager.Inst.UserData != null)
			{
				bBgmOn = PlayerDataManager.Inst.UserData.BGMOn;
				bSfxOn = PlayerDataManager.Inst.UserData.SFXOn;
			}

			mBgmController.SetVolume(mBgmVolume);
			mBgmController.SetEnabled(mBBgmEnabled);
			mBgmController.SetMuted(!bBgmOn);

			mSfxController.SetVolume(mSfxVolume);
			mSfxController.SetEnabled(mBSfxEnabled);
			mSfxController.SetMuted(!bSfxOn);
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
			string key = payload.Key.ToString();
			bool bIsBgm = key.Contains("BGM");
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
		}

		public void SetSFXEnabled(bool bEnabled)
		{
			mBSfxEnabled = bEnabled;
			mSfxController.SetEnabled(bEnabled);
		}

		public void SetBGMVolume(float volume)
		{
			mBgmVolume = Mathf.Clamp01(volume);
			mBgmController.SetVolume(mBgmVolume);
		}

		public void SetSFXVolume(float volume)
		{
			mSfxVolume = Mathf.Clamp01(volume);
			mSfxController.SetVolume(mSfxVolume);
		}
		public void SetBGMMute(bool isMute)
		{
			mBgmController.SetMuted(isMute);
		}
		public void SetSFXMute(bool isMute)
		{
			mSfxController.SetMuted(isMute);
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
