using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Core;

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
	[RequireComponent(typeof(CanvasGroup))]
	[RequireComponent(typeof(RectTransform))]
	public class SettingsPopup : MonoBehaviour
	{
		[Header("팝업 패널")]
		[SerializeField] private RectTransform mPopupPanel; // 스케일 애니메이션 대상 (미설정 시 자신의 RectTransform 사용)

		[Header("애니메이션")]
		[SerializeField] private float mAnimDuration = 0.25F;
		[SerializeField] private Ease mShowEase = Ease.OutBack;
		[SerializeField] private Ease mHideEase = Ease.InBack;

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

		private CanvasGroup mCanvasGroup;
		private CanvasGroup mCopyToastCanvasGroup;
		private RectTransform mAnimTarget;

		[Header("약관")]
		[SerializeField] private Button mTermsButton;
		[SerializeField] private Button mPrivacyButton;

		[Header("소셜 링크")]
		[SerializeField] private Button mInstagramButton;
		[SerializeField] private Button mTwitterButton;
		[SerializeField] private Button mYoutubeButton;

		// 토글 이벤트가 코드에서 바뀔 때 무한루프 방지
		private bool mBIsRefreshing = false;

		private void Awake()
		{
			// 팝업 CanvasGroup
			mCanvasGroup = GetComponent<CanvasGroup>();
			if (mCanvasGroup == null)
			{
				mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
			}

			// 애니메이션 대상 RectTransform
			mAnimTarget = mPopupPanel != null ? mPopupPanel : GetComponent<RectTransform>();

			// 토스트 CanvasGroup
			if (mCopyToastObject != null)
			{
				mCopyToastCanvasGroup = mCopyToastObject.GetComponent<CanvasGroup>();
				if (mCopyToastCanvasGroup == null)
				{
					mCopyToastCanvasGroup = mCopyToastObject.AddComponent<CanvasGroup>();
				}

				mCopyToastObject.SetActive(false);
			}

			SetupButtons();
			SetupToggles();

			gameObject.SetActive(false);
		}

		#region Show / Hide

		/// <summary>
		/// 설정 팝업 열기
		/// </summary>
		public void Show()
		{
			Debug.Log("show");
			gameObject.SetActive(true);
			RefreshUI();

			mCanvasGroup.alpha = 0F;
			mAnimTarget.localScale = Vector3.one * 0.85F;

			DOTween.Sequence()
				.Append(mCanvasGroup.DOFade(1F, mAnimDuration).SetEase(Ease.OutQuart))
				.Join(mAnimTarget.DOScale(1F, mAnimDuration).SetEase(mShowEase))
				.SetUpdate(true);
		}

		/// <summary>
		/// 설정 팝업 닫기
		/// </summary>
		public void Hide()
		{
			DOTween.Sequence()
				.Append(mCanvasGroup.DOFade(0F, mAnimDuration * 0.75F).SetEase(Ease.InQuart))
				.Join(mAnimTarget.DOScale(0.85F, mAnimDuration * 0.75F).SetEase(mHideEase))
				.OnComplete(() => gameObject.SetActive(false))
				.SetUpdate(true);
		}

		#endregion

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

			if (mBgmToggle != null && SettingsManager.Inst != null)
			{
				mBgmToggle.isOn = SettingsManager.Inst.BGMEnabled;
			}

			if (mSfxToggle != null && SettingsManager.Inst != null)
			{
				mSfxToggle.isOn = SettingsManager.Inst.SFXEnabled;
			}

			if (mVibrationToggle != null && SettingsManager.Inst != null)
			{
				mVibrationToggle.isOn = SettingsManager.Inst.VibrationEnabled;
			}

			mBIsRefreshing = false;
		}

		private void RefreshLanguageText()
		{
			if (mCurrentLanguageText == null || SettingsManager.Inst == null)
			{
				return;
			}

			ELanguage currentLanguage = SettingsManager.Inst.Language;
			mCurrentLanguageText.text = SettingsManager.Inst.GetLanguageDisplayName(currentLanguage);
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

			SettingsManager.Inst?.SetBGM(bIsOn);
			AudioManager.Inst?.PlayButtonClick();
		}

		private void OnSfxToggleChanged(bool bIsOn)
		{
			if (mBIsRefreshing)
			{
				return;
			}

			SettingsManager.Inst?.SetSFX(bIsOn);
			// SFX가 꺼진 경우엔 소리 재생 안 함, 켠 경우엔 확인용 재생
			if (bIsOn)
			{
				AudioManager.Inst?.PlayButtonClick();
			}
		}

		private void OnVibrationToggleChanged(bool bIsOn)
		{
			if (mBIsRefreshing)
			{
				return;
			}

			SettingsManager.Inst?.SetVibration(bIsOn);
			AudioManager.Inst?.PlayButtonClick();

			// 진동 켤 때 진동으로 피드백
			if (bIsOn)
			{
				SettingsManager.Inst?.Vibrate();
			}
		}

		#endregion

		#region 버튼 콜백

		private void OnCloseClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			Hide();
		}

		private void OnLanguageClick()
		{
			AudioManager.Inst?.PlayButtonClick();

			if (mLanguagePopup != null)
			{
				mLanguagePopup.SetActive(true);
			}
		}

		private void OnCopyUidClick()
		{
			if (SettingsManager.Inst != null)
			{
				SettingsManager.Inst.CopyUID();
			}

			AudioManager.Inst?.PlayButtonClick();

			if (mCopyToastCanvasGroup != null)
			{
				ShowCopyToast();
			}
		}

		private void ShowCopyToast()
		{
			mCopyToastObject.SetActive(true);
			mCopyToastCanvasGroup.alpha = 0F;

			DOTween.Sequence()
				.Append(mCopyToastCanvasGroup.DOFade(1F, 0.2F))
				.AppendInterval(1F)
				.Append(mCopyToastCanvasGroup.DOFade(0F, 0.3F))
				.OnComplete(() => mCopyToastObject.SetActive(false))
				.SetUpdate(true);
		}

		private void OnTermsClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst.OpenTermsUrl();
		}

		private void OnPrivacyClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst.OpenPrivacyUrl();
		}

		private void OnInstagramClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst.OpenInstagramUrl();
		}

		private void OnTwitterClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst.OpenTwitterUrl();
		}

		private void OnYoutubeClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst.OpenYoutubeUrl();
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
