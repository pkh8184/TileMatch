using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class _LanguagePopup : PopupBase
    {
        [Header("언어 선택 버튼 배열\n(순서대로 ko, en, ja, zh, hi, ar)")]
        [SerializeField] private Button[] mLanguageButtonArray;
        private string[] mLocaleHeaderStringArray = { "ko", "en", "ja", "zh", "hi", "ar" };

        [Header("선택된 언어 표시를 위한 스프라이트")]
        [SerializeField] private Sprite mCurrentLangaugeSprite;
        [SerializeField] private Sprite mDefaultLangaugeSprite;

        private int currentIndex = 0;
        public override void Initialize()
        {
            base.Initialize();

            for (int i = 0; i < mLanguageButtonArray.Length; i++)
            {
                int arrayIndex = i;
                mLanguageButtonArray[i].onClick.AddListener(() => OnLanguageButtonClicked(mLocaleHeaderStringArray[arrayIndex]));
            }

            int index = PlayerDataManager.Inst.GetLocaleIndex();

            currentIndex = index;

            mLanguageButtonArray[index].image.sprite = mCurrentLangaugeSprite;
        }
        private void OnLanguageButtonClicked(string locale)
        {
            StartCoroutine(Co_ChangeLocale(locale));
        }
        private IEnumerator Co_ChangeLocale(string locale)
        {
            int index = GetLocaleHeaderIndex(locale);

            PlayerDataManager.Inst?.SetLocaleIndex(index);
            SettingsManager.Inst.SetLanguage((ELanguage)index);
            //LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(locale);
            yield return null;

            mLanguageButtonArray[currentIndex].image.sprite = mDefaultLangaugeSprite;

            currentIndex = index;

            mLanguageButtonArray[index].image.sprite = mCurrentLangaugeSprite;
            
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
