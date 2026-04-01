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

        [Header("UID 관련")]
        [SerializeField] private TMP_Text mUidText;
        [SerializeField] private Button mUidCopyButton;

        [Header("이용약관 관련")]
        [SerializeField] private TMP_Text mTermsAndConditionsVerText;
        [SerializeField] private Button mTermsAndConditionsURLButton;

        [Header("SNS 버튼")]
        [SerializeField] private Button mInstagramButton;

        [Header("확인 버튼")]
        [SerializeField] private Button mConfirmButton;

        [Header("앱 버전 텍스트")]
        [SerializeField] private TMP_Text mAppVersionText;
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

            //UID 관련 UI 초기화
            mUidText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.UID);
            mUidCopyButton.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.UID);
            });

            //이용약관 관련 UI 초기화
            mTermsAndConditionsVerText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.TermsAndConditionVersion);
            mTermsAndConditionsURLButton.onClick.AddListener(() =>
            {
                //Application.OpenURL("이용약관 URL");
            });

            //SNS 버튼 초기화
            mInstagramButton.onClick.AddListener(() =>
            {
                //Application.OpenURL("인스타그램 URL");
            });

            //확인 버튼 초기화
            mConfirmButton.onClick.AddListener(() =>
            {
                Hide();
            });

            mAppVersionText.text = Application.version;
        }

        private void SetBGMToggle(bool isOn)
        {
            AudioManager.Inst?.SetBGMEnabled(isOn);
            PlayerDataManager.Inst?.SetBGMOn(isOn);
        }
        private void SetSFXToggle(bool isOn)
        {
            AudioManager.Inst?.SetSFXEnabled(isOn);
            PlayerDataManager.Inst?.SetSFXOn(isOn);
        }
    }  
}

