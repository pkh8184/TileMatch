using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class _LanguagePopup : PopupBase
    {
        [Header("언어 선택 버튼 배열\n(순서대로 ko, en, ja, zh, vi, hi, ar)")]
        [SerializeField] private Button[] mLanguageButtonArray;
        private string[] mLocaleHeaderStringArray = { "ko", "en", "ja", "zh", "vi", "hi", "ar" };

        [Header("현재 언어 선택 표시 오브젝트")]
        [SerializeField] private GameObject mCurrentLanguageObj;
        private RectTransform mCurrentLanguageObjRectTransform;

        [Header("언어 변경 시 로딩 표시 오브젝트")]
        [SerializeField] private GameObject mChangeLoadingObj;

        [Header("확인 버튼")]
        [SerializeField] private Button mConfirmButton;
        public override void Initialize()
        {
            base.Initialize();

            mCurrentLanguageObjRectTransform = mCurrentLanguageObj.GetComponent<RectTransform>();

            for (int i = 0; i < mLanguageButtonArray.Length; i++)
            {
                int arrayIndex = i;
                mLanguageButtonArray[i].onClick.AddListener(() => OnLanguageButtonClicked(mLocaleHeaderStringArray[arrayIndex]));
            }

            int index = PlayerDataManager.Inst.GetLocaleIndex();

            RefreshCurrentLangaugeObjTransform(index);

            mConfirmButton.onClick.AddListener(Hide);
        }
        private void RefreshCurrentLangaugeObjTransform(int index)
        {
            mCurrentLanguageObj.SetActive(false);

            mCurrentLanguageObj.transform.SetParent(mLanguageButtonArray[index].transform);
            mCurrentLanguageObjRectTransform.offsetMin = Vector2.zero;
            mCurrentLanguageObjRectTransform.offsetMax = Vector2.zero;

            mCurrentLanguageObj.SetActive(true);
        }
        private void OnLanguageButtonClicked(string locale)
        {
            StartCoroutine(Co_ChangeLocale(locale));
        }
        private IEnumerator Co_ChangeLocale(string locale)
        {
            mChangeLoadingObj.SetActive(true);

            int index = GetLocaleHeaderIndex(locale);

            PlayerDataManager.Inst?.SetLocaleIndex(index);
            //LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
            yield return null;

            mChangeLoadingObj.SetActive(false);

            RefreshCurrentLangaugeObjTransform(index);
        }
        private int GetLocaleHeaderIndex(string locale)
        {
            for(int i = 0; i < mLocaleHeaderStringArray.Length; i++)
            {
                if(mLocaleHeaderStringArray[i] == locale)
                {
                    return i;
                }
            }
            return -1;
        }
    }
    
}
