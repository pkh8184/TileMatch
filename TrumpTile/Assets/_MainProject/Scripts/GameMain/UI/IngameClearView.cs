using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameClearView : ViewBase
    {
        [Header("별 렉트들")]
        [SerializeField] private RectTransform[] mStarRectArray;
        [Header("타이머")]
        [SerializeField] private RectTransform mTimerRect;
        [SerializeField] private TMP_Text mTimerText;
        [Header("클리어 텍스트")]
        [SerializeField] private CanvasGroup mClearTextCanvasGroup;
        [Header("버튼들")]
        [SerializeField] private Button mRewardButton;
        [SerializeField] private Button mMainButton;
        [Header("보너스 레벨 관련")]
        [SerializeField] private GameObject mBonusLevelFrame;
        [SerializeField] private TMP_Text mGoldText;
        private RectTransform mRewardButtonRect;
        private RectTransform mMainButtonRect;

        private bool mbAnimProgress;
        public override void Initialize()
        {
            base.Initialize();

            mStarRectArray[0].parent.localScale = Vector2.zero;
            
            foreach(var item in mStarRectArray)
            {
                item.localScale = Vector2.zero;
            }
            
            mTimerRect.localScale = new Vector2(1,0);
            mTimerText.text = "00 : 00";
            mClearTextCanvasGroup.alpha = 0;

            mRewardButtonRect = mRewardButton.GetComponent<RectTransform>();
            mMainButtonRect = mMainButton.GetComponent<RectTransform>();

            mRewardButtonRect.localScale = Vector2.zero;
            mMainButtonRect.localScale = Vector2.zero;

            mRewardButton.onClick.AddListener(OnRewardButtonClick);
            mMainButton.onClick.AddListener(OnMainButtonClick);
        }
        public override void Show()
        {
            base.Show();
            
            bool bBonusLevel = GameManager.Instance.LevelDifficulty.ToString().Contains("Bonus");
            RectTransform rt = mBonusLevelFrame.GetComponent<RectTransform>();

            if(bBonusLevel)
            {
                mBonusLevelFrame.SetActive(true);
                rt.localScale = Vector2.zero;
            }
            else
            {
                mBonusLevelFrame.SetActive(false);
            }

            AudioEvent.Play(EAudioKey.SFX_StageClear);
            mbAnimProgress = true;

            Sequence seq = DOTween.Sequence();

            seq.Append(mStarRectArray[0].parent.DOScale(1, 0.5f));
            for(int i = 0; i < GameManager.Instance.StarCount; i++)
            {
                seq.Append(mStarRectArray[i].DOScale(1, 0.2f));
            }
            seq.Append(mTimerRect.DOScaleY(1, 0.5f));
            float value = 0;
            seq.Append(DOTween.To(() => value, x =>
            {
                value = x;
                int minutes = Mathf.FloorToInt(x) / 60;
                int seconds = Mathf.FloorToInt(x) % 60;
                mTimerText.text = string.Format("{0:D2} : {1:D2}", minutes, seconds);
            }, GameManager.Instance.ElapsedTime, 0.3f));

            seq.Append(mClearTextCanvasGroup.DOFade(1, 0.3f));
            if(bBonusLevel)
            {
                mGoldText.text = "+0";
                seq.Append(rt.DOScale(1.1f, 0.15f));
                seq.Append(rt.DOScale(1f, 0.15f));
                float val = 0;
                seq.Join(DOTween.To(() => val, x =>
                {
                    val = x;
                    mGoldText.text = "+" + Mathf.RoundToInt(x).ToString();
                }, GameManager.Instance.BonusLevelGold, 0.3f));
            }
            seq.Append(mRewardButtonRect.DOScale(1.1f, 0.15f));
            seq.Append(mRewardButtonRect.DOScale(1f, 0.15f));

            seq.Append(mMainButtonRect.DOScale(1.1f, 0.15f));
            seq.Append(mMainButtonRect.DOScale(1f, 0.15f));
     
            seq.OnComplete(() => mbAnimProgress = false);
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("LevelClear", Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("LevelClear", Show);
        }
        private void OnRewardButtonClick()
        {
            Debug.Log($"{mRewardButton.name} 클릭, 리워드 광고 재생 후 보상 획득");
        }
        private void OnMainButtonClick()
        {
            if(mbAnimProgress) return;
            GameManager.Instance.GoToMainMenu();
        }
    }    
}

