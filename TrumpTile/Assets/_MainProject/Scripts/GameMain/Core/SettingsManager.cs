using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum ELanguage
	{
		Korean = 0,
		English = 1,
		Japanese = 2,
		Chinese = 3,
		Vietnamese = 4,
		Hindi = 5,
		Arabic = 6
	}

	/// <summary>
	/// 앱 설정 관리 (BGM / SFX / 진동 / 언어)
	/// DontDestroyOnLoad 싱글톤
	/// </summary>
	public class SettingsManager : MonoBehaviour
	{
		public static SettingsManager Instance { get; private set; }

		private const string KEY_BGM = "Settings_BGM";
		private const string KEY_SFX = "Settings_SFX";
		private const string KEY_VIBRATION = "Settings_Vibration";
		private const string KEY_LANGUAGE = "Settings_Language";

		private bool mBBgmEnabled;
		private bool mBSfxEnabled;
		private bool mBVibrationEnabled;
		private ELanguage mLanguage;

		public bool BGMEnabled => mBBgmEnabled;
		public bool SFXEnabled => mBSfxEnabled;
		public bool VibrationEnabled => mBVibrationEnabled;
		public ELanguage Language => mLanguage;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
				LoadSettings();
			}
			else
			{
				Destroy(gameObject);
			}
		}

		private void Start()
		{
			ApplySettings();
		}

		private void LoadSettings()
		{
			mBBgmEnabled = PlayerPrefs.GetInt(KEY_BGM, 1) == 1;
			mBSfxEnabled = PlayerPrefs.GetInt(KEY_SFX, 1) == 1;
			mBVibrationEnabled = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;
			mLanguage = (ELanguage)PlayerPrefs.GetInt(KEY_LANGUAGE, (int)ELanguage.Korean);
		}

		/// <summary>
		/// 현재 설정값을 AudioManager에 적용
		/// </summary>
		public void ApplySettings()
		{
			AudioManager.Instance?.SetBGMEnabled(mBBgmEnabled);
			AudioManager.Instance?.SetSFXEnabled(mBSfxEnabled);
		}

		#region BGM

		/// <summary>
		/// BGM On/Off 설정
		/// </summary>
		public void SetBGM(bool bEnabled)
		{
			mBBgmEnabled = bEnabled;
			PlayerPrefs.SetInt(KEY_BGM, bEnabled ? 1 : 0);
			PlayerPrefs.Save();
			AudioManager.Instance?.SetBGMEnabled(mBBgmEnabled);
		}

		#endregion

		#region SFX

		/// <summary>
		/// SFX On/Off 설정
		/// </summary>
		public void SetSFX(bool bEnabled)
		{
			mBSfxEnabled = bEnabled;
			PlayerPrefs.SetInt(KEY_SFX, bEnabled ? 1 : 0);
			PlayerPrefs.Save();
			AudioManager.Instance?.SetSFXEnabled(mBSfxEnabled);
		}

		#endregion

		#region 진동

		/// <summary>
		/// 진동 On/Off 설정
		/// </summary>
		public void SetVibration(bool bEnabled)
		{
			mBVibrationEnabled = bEnabled;
			PlayerPrefs.SetInt(KEY_VIBRATION, bEnabled ? 1 : 0);
			PlayerPrefs.Save();
		}

		/// <summary>
		/// 진동 실행 (설정이 켜져있을 때만 동작)
		/// </summary>
		public void Vibrate()
		{
			if (!mBVibrationEnabled)
			{
				return;
			}

#if UNITY_ANDROID || UNITY_IOS
			Handheld.Vibrate();
#endif
		}

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
				case ELanguage.Vietnamese: return "vi";
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
				case ELanguage.Vietnamese: return "Tiếng Việt";
				case ELanguage.Hindi: return "हिन्दी";
				case ELanguage.Arabic: return "العربية";
				default: return "한국어";
			}
		}

		#endregion
	}
}
