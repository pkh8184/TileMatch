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
    public class IngameClearView : ViewBase
    {
        [Header("별 렉트들")]
        [SerializeField] private RectTransform[] mStarRectArray;
        [Header("타이머")]
        [SerializeField] private RectTransform mTimerRect;
        [SerializeField] private TMP_Text mTimerText;
        [Header("클리어 텍스트")]
        [SerializeField] private CanvasGroup mClearTextCanvasGroup;
        [SerializeField] private GameObject[] mStarTextArray;
        [Header("버튼들")]
        [SerializeField] private Button mRewardButton;
        [SerializeField] private Button mMainButton;
        [Header("보너스 레벨 관련")]
        [SerializeField] private GameObject mBonusLevelFrame;
        [SerializeField] private TMP_Text mGoldText;
        private RectTransform mRewardButtonRect;
        private RectTransform mMainButtonRect;

        //클리어 연출 중이거나 리워드 광고 대기 중이면 true. 버튼 입력을 막는 용도.
        //해제 경로가 하나라도 끊기면 버튼이 영구히 안 눌리므로 아래 복구 장치들과 함께 관리한다.
        private bool mbAnimProgress;

        //클리어 연출 시퀀스. 백그라운드 복귀 시 살아있는지 확인하려고 들고 있는다.
        private Sequence mShowSeq;

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
            foreach(var item in mStarTextArray)
            {
                item.SetActive(false);
            }

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
                rt.localScale = new Vector2(1, 0);
            }
            else
            {
                mBonusLevelFrame.SetActive(false);
            }

            AudioEvent.Play(EAudioKey.SFX_StageClear);
            mbAnimProgress = true;

            mShowSeq?.Kill();

            //SetUpdate(true): timeScale이 0이 되어도(일시정지 등) 연출이 멈추지 않게 한다.
            //멈추면 OnComplete가 실행되지 않아 mbAnimProgress가 true로 고정되고 버튼이 먹통이 된다.
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            mShowSeq = seq;

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
            }, GameManager.Instance.TotalPlayTime, 0.3f));

            if(bBonusLevel)
            {
                mGoldText.text = "0";
                seq.Append(rt.DOScaleY(1f, 0.5f));
                float val = 0;
                seq.Append(DOTween.To(() => val, x =>
                {
                    val = x;
                    mGoldText.text = Mathf.RoundToInt(x).ToString();
                }, CoreContainer.RewardContainer.Gold, 0.3f));
            }
            mStarTextArray[GameManager.Instance.StarCount - 1].SetActive(true);
            seq.Append(mClearTextCanvasGroup.DOFade(1, 0.3f));

            seq.AppendCallback(() => SettingsManager.Inst?.Vibrate(EVibrationStyle.Medium));
            seq.Append(mRewardButtonRect.DOScale(1.1f, 0.15f));
            seq.Append(mRewardButtonRect.DOScale(1f, 0.15f));

            seq.Append(mMainButtonRect.DOScale(1.1f, 0.15f));
            seq.Append(mMainButtonRect.DOScale(1f, 0.15f));
     
            seq.OnComplete(() =>
            {
                mShowSeq = null;
                mbAnimProgress = false;
            });
        }

        private void OnDisable()
        {
            mShowSeq?.Kill();
            mShowSeq = null;
            mbAnimProgress = false;
        }

        /// <summary>
        /// 백그라운드 복귀 시 잠금을 복구한다.
        /// 연출 시퀀스가 완료 콜백 없이 사라졌거나(Kill), 리워드 광고 콜백이 끝내 돌아오지 않으면
        /// mbAnimProgress가 true로 남아 메인/리워드 버튼이 영구히 눌리지 않는다.
        /// </summary>
        private void OnApplicationPause(bool pause)
        {
            if(pause)
            {
                return;
            }
            if(!mbAnimProgress)
            {
                return;
            }
            //연출이 아직 살아서 진행 중이면 그대로 두고 OnComplete에 맡긴다.
            if(mShowSeq != null && mShowSeq.IsActive())
            {
                return;
            }

            mShowSeq = null;
            mbAnimProgress = false;
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent(EventKeys.LEVEL_CLEAR, Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent(EventKeys.LEVEL_CLEAR, Show);
        }
        private void OnRewardButtonClick()
        {
            if(mbAnimProgress) return;
            mbAnimProgress = true;
            AdManager.Inst.ShowRewardedAd((bool done) =>
            {
                if(!done)
                {
                    //보상 미획득(광고 미준비/중간 이탈 등)이면 잠금을 반드시 되돌린다.
                    //안 풀면 리워드 버튼은 물론 메인 버튼까지 같이 막힌다.
                    mbAnimProgress = false;
                    return;
                }

                PlayerDataManager.Inst.AddGold(10);
                CoreContainer.RewardContainer.AddGold(10);
                GameManager.Instance.GoToMainMenu();
            });
        }
        private void OnMainButtonClick()
        {
            if(mbAnimProgress) return;
            mbAnimProgress = true;
            GameManager.Instance.GoToMainMenu();
        }
    }    
}

