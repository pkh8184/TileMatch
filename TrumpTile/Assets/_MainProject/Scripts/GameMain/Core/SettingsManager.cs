using System.Runtime.InteropServices;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum ELanguage
	{
		Korean = 0,
		English = 1,
		Japanese = 2,
		Chinese = 3,
		Hindi = 4,
		Arabic = 5
	}

	/// <summary>
	/// 진동 스타일 타입
	/// </summary>
	public enum EVibrationStyle
	{
		Light,    // 짧고 가벼운 - 버튼 탭, 타일 선택
		Medium,   // 표준 - 타일 매치
		Heavy,    // 강하고 긴 - 콤보, 특수 이벤트
		Success,  // 성공 패턴 - 레벨 클리어
		Fail,     // 실패 패턴 - 게임 오버
	}

	/// <summary>
	/// 앱 설정 관리 (BGM / SFX / 진동 / 언어)
	/// DontDestroyOnLoad 싱글톤
	/// </summary>
	public class SettingsManager : Singleton_GameObject<SettingsManager>
	{
		[Header("링크 데이터")]
		[SerializeField] private AppLinksData mAppLinksData;

		private const string KEY_LANGUAGE = "Settings_Language";

		private ELanguage mLanguage;

		//BGM / SFX / 진동 설정은 PlayerDataManager(UserData)가 단독으로 소유한다.
		//예전엔 SettingsManager가 Settings_* 키로 같은 설정을 따로 들고 있었고, 실제로 쓰이는 설정 팝업
		//(_SettingPopup / IngameSettingPopup)은 PlayerDataManager 쪽에만 저장했다.
		//그 결과 Vibrate()가 아무도 갱신하지 않는 Settings_Vibration(기본 1)을 보고 있어서
		//진동을 꺼도 계속 울렸다. 여기서는 저장하지 않고 PlayerDataManager 값을 그대로 읽어 쓴다.
		public bool BGMEnabled => GetSoundSetting().BGMOn;
		public bool SFXEnabled => GetSoundSetting().SFXOn;
		public bool VibrationEnabled => GetSoundSetting().HapticOn;
		public ELanguage Language => mLanguage;

		private void Awake()
		{
			LoadSettings();
		}

		private void Start()
		{
			ApplySettings();
		}

		private void LoadSettings()
		{
			mLanguage = (ELanguage)PlayerPrefs.GetInt(KEY_LANGUAGE, (int)ELanguage.Korean);
		}

		/// <summary>
		/// 저장된 사운드 설정을 읽는다. PlayerDataManager가 아직 없으면 켜짐을 기본값으로 둔다.
		/// </summary>
		private (bool BGMOn, bool SFXOn, bool HapticOn) GetSoundSetting()
		{
			if (PlayerDataManager.Inst == null || PlayerDataManager.Inst.UserData == null)
			{
				return (true, true, true);
			}
			return PlayerDataManager.Inst.GetUserSoundSettingDatas();
		}

		/// <summary>
		/// 현재 설정값을 AudioManager에 적용
		/// </summary>
		public void ApplySettings()
		{
			(bool BGMOn, bool SFXOn, bool HapticOn) setting = GetSoundSetting();
			AudioManager.Inst?.SetBGMMute(!setting.BGMOn);
			AudioManager.Inst?.SetSFXMute(!setting.SFXOn);
		}

		#region BGM

		/// <summary>
		/// BGM On/Off 설정
		/// </summary>
		public void SetBGM(bool bEnabled)
		{
			PlayerDataManager.Inst?.SetBGMOn(bEnabled);
			AudioManager.Inst?.SetBGMMute(!bEnabled);
		}

		#endregion

		#region SFX

		/// <summary>
		/// SFX On/Off 설정
		/// </summary>
		public void SetSFX(bool bEnabled)
		{
			PlayerDataManager.Inst?.SetSFXOn(bEnabled);
			AudioManager.Inst?.SetSFXMute(!bEnabled);
		}

		#endregion

		#region 진동

#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern void _HapticLight();
		[DllImport("__Internal")] private static extern void _HapticMedium();
		[DllImport("__Internal")] private static extern void _HapticHeavy();
		[DllImport("__Internal")] private static extern void _HapticSuccess();
		[DllImport("__Internal")] private static extern void _HapticError();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
		private AndroidJavaObject mAndroidVibrator;
		private int mAndroidAPILevel = -1;

		private AndroidJavaObject GetAndroidVibrator()
		{
			if (mAndroidVibrator == null)
			{
				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				mAndroidVibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
			}
			return mAndroidVibrator;
		}

		private int GetAndroidAPILevel()
		{
			if (mAndroidAPILevel < 0)
			{
				AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
				mAndroidAPILevel = versionClass.GetStatic<int>("SDK_INT");
			}
			return mAndroidAPILevel;
		}
#endif

		/// <summary>
		/// 진동 On/Off 설정
		/// </summary>
		public void SetVibration(bool bEnabled)
		{
			PlayerDataManager.Inst?.SetHapticOn(bEnabled);
		}

		/// <summary>
		/// 진동 실행 (설정이 켜져있을 때만 동작)
		/// </summary>
		public void Vibrate(EVibrationStyle style = EVibrationStyle.Medium)
		{
			if (!VibrationEnabled)
			{
				return;
			}

#if UNITY_ANDROID && !UNITY_EDITOR
			VibrateAndroid(style);
#elif UNITY_IOS && !UNITY_EDITOR
			VibrateIOS(style);
#endif
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		private void VibrateAndroid(EVibrationStyle style)
		{
			AndroidJavaObject vibrator = GetAndroidVibrator();
			if (vibrator == null)
			{
				return;
			}

			if (GetAndroidAPILevel() >= 26)
			{
				AndroidJavaClass vibEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
				AndroidJavaObject effect = null;

				switch (style)
				{
					case EVibrationStyle.Light:
						effect = vibEffectClass.CallStatic<AndroidJavaObject>("createOneShot", 30L, 80);
						break;
					case EVibrationStyle.Medium:
						effect = vibEffectClass.CallStatic<AndroidJavaObject>("createOneShot", 60L, 160);
						break;
					case EVibrationStyle.Heavy:
						effect = vibEffectClass.CallStatic<AndroidJavaObject>("createOneShot", 100L, 255);
						break;
					case EVibrationStyle.Success:
						long[] successTimings = { 0L, 60L, 80L, 60L };
						int[] successAmplitudes = { 0, 200, 0, 255 };
						effect = vibEffectClass.CallStatic<AndroidJavaObject>("createWaveform", successTimings, successAmplitudes, -1);
						break;
					case EVibrationStyle.Fail:
						long[] failTimings = { 0L, 100L, 60L, 80L };
						int[] failAmplitudes = { 0, 255, 0, 150 };
						effect = vibEffectClass.CallStatic<AndroidJavaObject>("createWaveform", failTimings, failAmplitudes, -1);
						break;
				}

				if (effect != null)
				{
					vibrator.Call("vibrate", effect);
				}
			}
			else
			{
				vibrator.Call("vibrate", GetFallbackDurationMs(style));
			}
		}

		private static long GetFallbackDurationMs(EVibrationStyle style)
		{
			switch (style)
			{
				case EVibrationStyle.Light:   return 30L;
				case EVibrationStyle.Medium:  return 60L;
				case EVibrationStyle.Heavy:   return 100L;
				case EVibrationStyle.Success: return 150L;
				case EVibrationStyle.Fail:    return 200L;
				default:                      return 60L;
			}
		}
#endif

#if UNITY_IOS && !UNITY_EDITOR
		private void VibrateIOS(EVibrationStyle style)
		{
			switch (style)
			{
				case EVibrationStyle.Light:   _HapticLight();   break;
				case EVibrationStyle.Medium:  _HapticMedium();  break;
				case EVibrationStyle.Heavy:   _HapticHeavy();   break;
				case EVibrationStyle.Success: _HapticSuccess(); break;
				case EVibrationStyle.Fail:    _HapticError();   break;
			}
		}
#endif

		#endregion

		#region 언어

		/// <summary>
		/// 언어 설정
		/// </summary>
		public void SetLanguage(ELanguage language)
		{
			mLanguage = language;
			PlayerPrefs.SetInt(KEY_LANGUAGE, (int)language);
			PlayerPrefs.Save();
			EventManager.Inst?.ActiveEvent(RequestEventKeys.REFRESH_LANGUAGE);
			Debug.Log($"[SettingsManager] Language changed to {language}");
		}

		/// <summary>
		/// 언어 코드 반환 (예: "ko", "en", "ja" ...)
		/// </summary>
		public string GetLanguageCode()
		{
			switch (mLanguage)
			{
				case ELanguage.Korean: return "ko";
				case ELanguage.English: return "en";
				case ELanguage.Japanese: return "ja";
				case ELanguage.Chinese: return "zh";
				case ELanguage.Hindi: return "hi";
				case ELanguage.Arabic: return "ar";
				default: return "ko";
			}
		}

		/// <summary>
		/// 언어 표시명 반환
		/// </summary>
		public string GetLanguageDisplayName(ELanguage language)
		{
			switch (language)
			{
				case ELanguage.Korean: return "한국어";
				case ELanguage.English: return "English";
				case ELanguage.Japanese: return "日本語";
				case ELanguage.Chinese: return "中文";
				case ELanguage.Hindi: return "हिन्दी";
				case ELanguage.Arabic: return "العربية";
				default: return "한국어";
			}
		}

		#endregion

		#region 약관 / 소셜

		public void OpenTermsUrl()
		{
			if (mAppLinksData != null)
			{
				OpenURL(mAppLinksData.TermsUrl);
			}
		}

		public void OpenPrivacyUrl()
		{
			if (mAppLinksData != null)
			{
				OpenURL(mAppLinksData.PrivacyUrl);
			}
		}

		public void OpenInstagramUrl()
		{
			if (mAppLinksData != null)
			{
				OpenURL(mAppLinksData.InstagramUrl);
			}
		}

		public void OpenTwitterUrl()
		{
			if (mAppLinksData != null)
			{
				OpenURL(mAppLinksData.TwitterUrl);
			}
		}

		public void OpenYoutubeUrl()
		{
			if (mAppLinksData != null)
			{
				OpenURL(mAppLinksData.YoutubeUrl);
			}
		}

		private void OpenURL(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				return;
			}

			Application.OpenURL(url);
		}

		#endregion

		#region 계정

		/// <summary>
		/// 사용자 UID를 클립보드에 복사
		/// </summary>
		public void CopyUID()
		{
			if (PlayerDataManager.Inst == null)
			{
				return;
			}

			// string uid = PlayerDataManager.Inst.UID;
			// if (string.IsNullOrEmpty(uid))
			// {
			// 	return;
			// }

			// GUIUtility.systemCopyBuffer = uid;
			// Debug.Log($"[SettingsManager] UID copied: {uid}");
		}

		#endregion
	}
}
