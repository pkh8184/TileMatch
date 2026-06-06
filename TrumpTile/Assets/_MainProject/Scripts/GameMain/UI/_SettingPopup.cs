using Google.MiniJSON;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class _SettingPopup : PopupBase
    {
        [Header("소리/진동 토글")]
        [SerializeField] private Toggle mBGMToggle;
        [SerializeField] private Toggle mSFXToggle;
        [SerializeField] private Toggle mHapticToggle;

        [Header("이용약관 버튼")]
        [SerializeField] private Button mTermsButton;
        [SerializeField] private Button mPolicyButton;

        [Header("공유 버튼")]
        [SerializeField] private Button mShareButton;

        [Header("앱 버전 텍스트")]
        [SerializeField] private TMP_Text mAppVersionText;

        [Header("공유 텍스트 및 URL")]
        [SerializeField] private string mShareText;
        [SerializeField] private string mShareURL = "https://";

        public override void Initialize()
        {
            base.Initialize();

            //사운드 관련 UI 초기화
            (bool BGMOn, bool SFXOn, bool HapticOn) soundSetting = PlayerDataManager.Inst.GetUserSoundSettingDatas();
            mBGMToggle.isOn = soundSetting.BGMOn;
            mSFXToggle.isOn = soundSetting.SFXOn;
            mHapticToggle.isOn = soundSetting.HapticOn;
          
            mBGMToggle.onValueChanged.AddListener((isOn) =>
            {
                SetBGMToggle(isOn);
            });
            mSFXToggle.onValueChanged.AddListener((isOn) =>
            {
                SetSFXToggle(isOn);
            });
            mHapticToggle.onValueChanged.AddListener((isOn) =>
            {
                PlayerDataManager.Inst?.SetHapticOn(isOn);
            });

            mTermsButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://pkh8184.github.io/AcidHomePage/#/project/housetilematch/terms");
            });

            mPolicyButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://pkh8184.github.io/AcidHomePage/#/project/housetilematch/privacy");
            });

            mShareButton.onClick.AddListener(() =>
            {
#if UNITY_ANDROID
                ShareURL();
#else
                Debug.Log($"{mShareText}\n{mShareURL}");
#endif
            });
            mAppVersionText.text = Application.version;
        }
#if UNITY_ANDROID
        private void ShareURL()
        {
            string shareContent = $"{mShareText}\n{mShareURL}";
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("setType", "text/plain");
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareContent);

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "공유하기");
            currentActivity.Call("startActivity", chooser);
        }
#endif
        private void SetBGMToggle(bool isOn)
        {
            AudioManager.Inst?.SetBGMMute(!isOn);
            PlayerDataManager.Inst?.SetBGMOn(isOn);
        }
        private void SetSFXToggle(bool isOn)
        {
            AudioManager.Inst?.SetSFXMute(!isOn);
            PlayerDataManager.Inst?.SetSFXOn(isOn);
        }
    }  
}

