using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 설정 팝업 UI 컨트롤러
	/// - BGM / SFX / 진동 On/Off
	/// - 언어 설정 (LanguagePopup 호출)
	/// - 계정 정보 (UID 표시 및 복사)
	/// - 약관 확인 URL 연결
	/// - 소셜 링크 (인스타그램 / X / YouTube)
	/// </summary>
	public class SettingsPopup : MonoBehaviour
	{
		[Header("닫기")]
		[SerializeField] private Button mCloseButton;

		[Header("사운드 설정")]
		[SerializeField] private Toggle mBgmToggle;
		[SerializeField] private Toggle mSfxToggle;

		[Header("진동 설정")]
		[SerializeField] private Toggle mVibrationToggle;

		[Header("언어 설정")]
		[SerializeField] private Button mLanguageButton;
		[SerializeField] private TextMeshProUGUI mCurrentLanguageText;
		[SerializeField] private GameObject mLanguagePopup;

		[Header("계정 정보")]
		[SerializeField] private TextMeshProUGUI mUidText;
		[SerializeField] private Button mCopyUidButton;
		[SerializeField] private GameObject mCopyToastObject; // 복사 완료 토스트 (선택)

		[Header("약관")]
		[SerializeField] private Button mTermsButton;
		[SerializeField] private Button mPrivacyButton;

		[Header("소셜 링크")]
		[SerializeField] private Button mInstagramButton;
		[SerializeField] private Button mTwitterButton;
		[SerializeField] private Button mYoutubeButton;

		[Header("링크 데이터")]
		[SerializeField] private AppLinksData mAppLinksData;

		// 토글 이벤트가 코드에서 바뀔 때 무한루프 방지
		private bool mBIsRefreshing = false;

		private void Awake()
		{
			SetupButtons();
			SetupToggles();
		}

		private void OnEnable()
		{
			RefreshUI();
		}

		#region 초기화

		private void SetupButtons()
		{
			if (mCloseButton != null)
			{
				mCloseButton.onClick.AddListener(OnCloseClick);
			}

			if (mLanguageButton != null)
			{
				mLanguageButton.onClick.AddListener(OnLanguageClick);
			}

			if (mCopyUidButton != null)
			{
				mCopyUidButton.onClick.AddListener(OnCopyUidClick);
			}

			if (mTermsButton != null)
			{
				mTermsButton.onClick.AddListener(OnTermsClick);
			}

			if (mPrivacyButton != null)
			{
				mPrivacyButton.onClick.AddListener(OnPrivacyClick);
			}

			if (mInstagramButton != null)
			{
				mInstagramButton.onClick.AddListener(OnInstagramClick);
			}

			if (mTwitterButton != null)
			{
				mTwitterButton.onClick.AddListener(OnTwitterClick);
			}

			if (mYoutubeButton != null)
			{
				mYoutubeButton.onClick.AddListener(OnYoutubeClick);
			}
		}

		private void SetupToggles()
		{
			if (mBgmToggle != null)
			{
				mBgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
			}

			if (mSfxToggle != null)
			{
				mSfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
			}

			if (mVibrationToggle != null)
			{
				mVibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
			}
		}

		#endregion

		#region UI 갱신

		/// <summary>
		/// 현재 설정값으로 UI 갱신
		/// </summary>
		public void RefreshUI()
		{
			RefreshToggles();
			RefreshLanguageText();
			RefreshUid();
		}

		private void RefreshToggles()
		{
			mBIsRefreshing = true;

			if (mBgmToggle != null && SettingsManager.Instance != null)
			{
				mBgmToggle.isOn = SettingsManager.Instance.BGMEnabled;
			}

			if (mSfxToggle != null && SettingsManager.Instance != null)
			{
				mSfxToggle.isOn = SettingsManager.Instance.SFXEnabled;
			}

			if (mVibrationToggle != null && SettingsManager.Instance != null)
			{
				mVibrationToggle.isOn = SettingsManager.Instance.VibrationEnabled;
			}

			mBIsRefreshing = false;
		}

		private void RefreshLanguageText()
		{
			if (mCurrentLanguageText == null || SettingsManager.Instance == null)
			{
				return;
			}

			ELanguage currentLanguage = SettingsManager.Instance.Language;
			mCurrentLanguageText.text = SettingsManager.Instance.GetLanguageDisplayName(currentLanguage);
		}

		private void RefreshUid()
		{
			if (mUidText == null || UserDataManager.Instance == null)
			{
				return;
			}

			string uid = UserDataManager.Instance.UID;
			// 가독성을 위해 4자리씩 끊어서 표시
			mUidText.text = FormatUID(uid);
		}

		/// <summary>
		/// UID를 4자리씩 끊어 표시 (예: ABCD-EFGH-IJKL-MNOP-...)
		/// </summary>
		private string FormatUID(string uid)
		{
			if (string.IsNullOrEmpty(uid))
			{
				return "-";
			}

			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < uid.Length; i++)
			{
				if (i > 0 && i % 4 == 0)
				{
					sb.Append('-');
				}

				sb.Append(uid[i]);
			}

			return sb.ToString();
		}

		#endregion

		#region 토글 콜백

		private void OnBgmToggleChanged(bool bIsOn)
		{
			if (mBIsRefreshing)
			{
				return;
			}

			SettingsManager.Instance?.SetBGM(bIsOn);
			AudioManager.Instance?.PlayButtonClick();
		}

		private void OnSfxToggleChanged(bool bIsOn)
		{
			if (mBIsRefreshing)
			{
				return;
			}

			SettingsManager.Instance?.SetSFX(bIsOn);
			// SFX가 꺼진 경우엔 소리 재생 안 함, 켠 경우엔 확인용 재생
			if (bIsOn)
			{
				AudioManager.Instance?.PlayButtonClick();
			}
		}

		private void OnVibrationToggleChanged(bool bIsOn)
		{
			if (mBIsRefreshing)
			{
				return;
			}

			SettingsManager.Instance?.SetVibration(bIsOn);
			AudioManager.Instance?.PlayButtonClick();

			// 진동 켤 때 진동으로 피드백
			if (bIsOn)
			{
				SettingsManager.Instance?.Vibrate();
			}
		}

		#endregion

		#region 버튼 콜백

		private void OnCloseClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			gameObject.SetActive(false);
		}

		private void OnLanguageClick()
		{
			AudioManager.Instance?.PlayButtonClick();

			if (mLanguagePopup != null)
			{
				mLanguagePopup.SetActive(true);
			}
		}

		private void OnCopyUidClick()
		{
			if (UserDataManager.Instance == null)
			{
				return;
			}

			string uid = UserDataManager.Instance.UID;
			GUIUtility.systemCopyBuffer = uid;
			AudioManager.Instance?.PlayButtonClick();

			Debug.Log($"[SettingsPopup] UID copied: {uid}");

			// 복사 완료 토스트 표시
			if (mCopyToastObject != null)
			{
				StartCoroutine(ShowCopyToast());
			}
		}

		private System.Collections.IEnumerator ShowCopyToast()
		{
			mCopyToastObject.SetActive(true);

			float elapsed = 0F;
			while (elapsed < 1.5F)
			{
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			if (mCopyToastObject != null)
			{
				mCopyToastObject.SetActive(false);
			}
		}

		private void OnTermsClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			OpenURL(mAppLinksData?.TermsUrl);
		}

		private void OnPrivacyClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			OpenURL(mAppLinksData?.PrivacyUrl);
		}

		private void OnInstagramClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			OpenURL(mAppLinksData?.InstagramUrl);
		}

		private void OnTwitterClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			OpenURL(mAppLinksData?.TwitterUrl);
		}

		private void OnYoutubeClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			OpenURL(mAppLinksData?.YoutubeUrl);
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

		/// <summary>
		/// LanguagePopup에서 언어 선택 후 호출
		/// </summary>
		public void OnLanguageSelected()
		{
			RefreshLanguageText();
		}
	}
}
