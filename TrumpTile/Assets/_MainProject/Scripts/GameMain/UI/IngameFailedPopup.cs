using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameFailedPopup : PopupBase
    {
        [Header("다시 시도 버튼")]
        [SerializeField] private Button mRetryButton;
        [SerializeField] private Button mCancleButton;
        [Header("레벨 텍스트")]
        [SerializeField] private TMP_Text mLevelText;
        public override void Initialize()
        {
            base.Initialize();

            mRetryButton.onClick.AddListener(() => 
            {
                Hide();
                GameManager.Instance.RestartLevel();
            });
            mCancleButton.onClick.AddListener(() => 
            {
                CoreContainer.RewardContainer.Clear();
                GameManager.Instance.GoToMainMenu();
            });
        }
        public override void Show()
        {
            bool bIsDailyMode = DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive;
			if (bIsDailyMode)
			{
				mLevelText.text = LocalizeManager.Inst.GetString(200068);
			}
			else
			{
                string header = GameManager.Instance.IsChampionsMode ? $"{LocalizeManager.Inst.GetString(200155)} " : $"{LocalizeManager.Inst.GetString(200189)} ";
                string number = GameManager.Instance.IsChampionsMode ? PlayerDataManager.Inst.ChampionsLevel.ToString() : GameManager.Instance.CurrentLevel.ToString();
                mLevelText.text = header + number;
            }
            mLevelText.font = LocalizeManager.Inst.GetFontAssetByLocale();
            LocalizeManager.Inst.ApplyRTL(mLevelText);
            AudioEvent.Play(EAudioKey.SFX_StageLosed_02);
            base.Show();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent(EventKeys.STAGE_FAILED, Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst.RemoveEvent(EventKeys.STAGE_FAILED, Show);
        }
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);
                GameManager.Instance.RestartLevel();
            });   
        }
    }   
}
