using Google.MiniJSON;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameSettingPopup : PopupBase
{
        [Header("토글들")]
        [SerializeField] private Toggle mBGMToggle;
        [SerializeField] private Toggle mSFXToggle;
        [SerializeField] private Toggle mVibrationToggle;

        [Header("나가기 버튼")]
        [SerializeField] private Button mExitButton;

        private Image mBGMImage;
        private Image mSFXImage;
        private Image mVibrationImage;

        [Header("토글 스프라이트")]
        [SerializeField] private Sprite mBGMToggleOnSprite;
        [SerializeField] private Sprite mBGMToggleOffSprite;
        [SerializeField] private Sprite mSFXToggleOnSprite;
        [SerializeField] private Sprite mSFXToggleOffSprite;
        [SerializeField] private Sprite mVibrationToggleOnSprite;
        [SerializeField] private Sprite mVibrationToggleOffSprite;
        private Animator mAnimator;
        public override void Initialize()
        {
            base.Initialize();

            mAnimator = GetComponentInChildren<Animator>();
            //로컬 세팅 저장값 적용
            //mBGMToggle.isOn = true;
            //mSFXToggle.isOn = true;
            //mVibrationToggle.isOn = true;
            mBGMImage = mBGMToggle.GetComponent<Image>();
            mSFXImage = mSFXToggle.GetComponent<Image>();
            mVibrationImage = mVibrationToggle.GetComponent<Image>();

            mBGMImage.sprite = mBGMToggle.isOn ? mBGMToggleOnSprite : mBGMToggleOffSprite;
            mSFXImage.sprite = mSFXToggle.isOn ? mSFXToggleOnSprite : mSFXToggleOffSprite;
            mVibrationImage.sprite = mVibrationToggle.isOn ? mVibrationToggleOnSprite : mVibrationToggleOffSprite;

            mBGMToggle.onValueChanged.AddListener((isOn) => mBGMImage.sprite = isOn? mBGMToggleOnSprite : mBGMToggleOffSprite);
            mSFXToggle.onValueChanged.AddListener((isOn) => mSFXImage.sprite = isOn? mSFXToggleOnSprite : mSFXToggleOffSprite);
            mVibrationToggle.onValueChanged.AddListener((isOn) => mVibrationImage.sprite = isOn? mVibrationToggleOnSprite : mVibrationToggleOffSprite);
            mExitButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("ExitGameScene"));
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.SetActive(true);
            mAnimator.SetTrigger("Show");
        }
        protected override void PlayHideAnim()
        {
            mPopupObj.SetActive(false);
        }
    }
}
