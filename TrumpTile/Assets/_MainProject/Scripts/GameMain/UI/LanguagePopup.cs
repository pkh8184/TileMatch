using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 언어 선택 팝업
	/// 지원 언어: 한국어, 영어, 일본어, 중국어, 베트남어, 힌디어, 아랍어
	/// </summary>
	public class LanguagePopup : MonoBehaviour
	{
		[Header("닫기")]
		[SerializeField] private Button mCloseButton;

		[Header("언어 버튼")]
		[SerializeField] private Button mKoreanButton;
		[SerializeField] private Button mEnglishButton;
		[SerializeField] private Button mJapaneseButton;
		[SerializeField] private Button mChineseButton;
		[SerializeField] private Button mVietnameseButton;
		[SerializeField] private Button mHindiButton;
		[SerializeField] private Button mArabicButton;

		[Header("선택 표시 (선택된 항목 강조 오브젝트)")]
		[SerializeField] private GameObject mKoreanSelected;
		[SerializeField] private GameObject mEnglishSelected;
		[SerializeField] private GameObject mJapaneseSelected;
		[SerializeField] private GameObject mChineseSelected;
		[SerializeField] private GameObject mVietnameseSelected;
		[SerializeField] private GameObject mHindiSelected;
		[SerializeField] private GameObject mArabicSelected;

		[Header("SettingsPopup 참조 (언어 변경 후 갱신)")]
		[SerializeField] private SettingsPopup mSettingsPopup;

		private void Awake()
		{
			SetupButtons();
		}

		private void OnEnable()
		{
			RefreshSelectedIndicator();
		}

		private void SetupButtons()
		{
			if (mCloseButton != null)
			{
				mCloseButton.onClick.AddListener(OnCloseClick);
			}

			if (mKoreanButton != null)
			{
				mKoreanButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Korean));
			}

			if (mEnglishButton != null)
			{
				mEnglishButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.English));
			}

			if (mJapaneseButton != null)
			{
				mJapaneseButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Japanese));
			}

			if (mChineseButton != null)
			{
				mChineseButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Chinese));
			}

			if (mVietnameseButton != null)
			{
				mVietnameseButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Vietnamese));
			}

			if (mHindiButton != null)
			{
				mHindiButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Hindi));
			}

			if (mArabicButton != null)
			{
				mArabicButton.onClick.AddListener(() => OnLanguageSelected(ELanguage.Arabic));
			}
		}

		/// <summary>
		/// 현재 선택된 언어에 따라 선택 표시 갱신
		/// </summary>
		private void RefreshSelectedIndicator()
		{
			if (SettingsManager.Inst == null)
			{
				return;
			}

			ELanguage currentLanguage = SettingsManager.Inst.Language;

			SetSelectedIndicator(mKoreanSelected, currentLanguage == ELanguage.Korean);
			SetSelectedIndicator(mEnglishSelected, currentLanguage == ELanguage.English);
			SetSelectedIndicator(mJapaneseSelected, currentLanguage == ELanguage.Japanese);
			SetSelectedIndicator(mChineseSelected, currentLanguage == ELanguage.Chinese);
			SetSelectedIndicator(mVietnameseSelected, currentLanguage == ELanguage.Vietnamese);
			SetSelectedIndicator(mHindiSelected, currentLanguage == ELanguage.Hindi);
			SetSelectedIndicator(mArabicSelected, currentLanguage == ELanguage.Arabic);
		}

		private void SetSelectedIndicator(GameObject indicator, bool bActive)
		{
			if (indicator != null)
			{
				indicator.SetActive(bActive);
			}
		}

		#region 버튼 콜백

		private void OnLanguageSelected(ELanguage language)
		{
			AudioManager.Inst?.PlayButtonClick();
			SettingsManager.Inst?.SetLanguage(language);
			RefreshSelectedIndicator();

			// SettingsPopup의 언어 텍스트 갱신
			if (mSettingsPopup != null)
			{
				mSettingsPopup.OnLanguageSelected();
			}

			// 팝업 닫기
			gameObject.SetActive(false);
		}

		private void OnCloseClick()
		{
			AudioManager.Inst?.PlayButtonClick();
			gameObject.SetActive(false);
		}

		#endregion
	}
}
