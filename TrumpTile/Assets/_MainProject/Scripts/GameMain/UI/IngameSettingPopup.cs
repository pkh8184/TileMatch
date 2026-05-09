using DG.Tweening;
using Google.MiniJSON;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
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
        [Header("세팅 아이콘 렉트 참조")]
        [SerializeField] private RectTransform mSettingIconRect;

        private Sequence mShowAnimSequence;

        private List<RectTransform> mAnimRectList = new List<RectTransform>();
        public override void Initialize()
        {
            base.Initialize();

            //로컬 세팅 저장값 적용
            (bool BGMOn, bool SFXOn, bool HapticOn) soundSetting = PlayerDataManager.Inst.GetUserSoundSettingDatas();
            mBGMToggle.isOn = soundSetting.BGMOn;
            mSFXToggle.isOn = soundSetting.SFXOn;
            mVibrationToggle.isOn = soundSetting.HapticOn;

            mBGMToggle.onValueChanged.AddListener((isOn) =>
            {
                SetBGMToggle(isOn);
            });
            mSFXToggle.onValueChanged.AddListener((isOn) =>
            {
                SetSFXToggle(isOn);
            });
            mVibrationToggle.onValueChanged.AddListener((isOn) =>
            {
                PlayerDataManager.Inst?.SetHapticOn(isOn);
            });

            mExitButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("ExitGameScene"));

            mAnimRectList.Add(mBGMToggle.GetComponent<RectTransform>());
            mAnimRectList.Add(mSFXToggle.GetComponent<RectTransform>());
            mAnimRectList.Add(mVibrationToggle.GetComponent<RectTransform>());
            mAnimRectList.Add(mExitButton.GetComponent<RectTransform>());
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.SetActive(true);
            GameManager.Instance.PauseGame();
            StartCoroutine(Co_PlayShowAnim());  
        }
        private IEnumerator Co_PlayShowAnim()
        {
            mShowAnimSequence?.Kill();
            mShowAnimSequence = DOTween.Sequence();
            mShowAnimSequence.SetUpdate(true);
            mShowAnimSequence.Insert(0, mSettingIconRect.DORotate(new Vector3(0,0,360), 0.4f, RotateMode.FastBeyond360));

            for (int i = 0; i < mAnimRectList.Count; i++)
            {
                RectTransform rect = mAnimRectList[i];

                rect.anchoredPosition = new Vector2(300, rect.anchoredPosition.y);
                mShowAnimSequence.Insert(i * 0.05f, rect.DOAnchorPosX(0, 0.1f).SetEase(Ease.OutQuad));
            }

            yield return mShowAnimSequence.WaitForCompletion();
        }
        protected override void PlayHideAnim()
        {
            mPopupObj.SetActive(false);
            GameManager.Instance.ResumeGame();
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
