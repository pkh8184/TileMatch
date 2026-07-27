using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameGameOverPopup : PopupBase
    {
        public enum EGameOverType
        {
            TimeOut,
            SlotFull,
            None
        }
        [Header("게임 오버 타입")]
        [SerializeField] private EGameOverType mEGameOverType;
        [Header("상점 버튼 / 보유 골드 텍스트")]
        [SerializeField] private Button mShopButton;
        [SerializeField] private TMP_Text mGoldText;
        [Header("부활 팝업 버튼들")]
        [SerializeField] private Button mCostButton;
        [SerializeField] private Button mAdsButton;
        [SerializeField] private Button mAdsFreeButton;
        [SerializeField] private Button mFreeButton;
        [SerializeField] private Button mCancleButton;
        [Header("부활 팝업 텍스트")]
        [SerializeField] private TMP_Text mReviveCostText;

        private Button mActiveAdsButton;
        private bool mbAdsFreeDone;
        public override void Initialize()
        {
            base.Initialize();

            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent(EventKeys.ACCESS_SHOP_VIEW));

            mCostButton.onClick.AddListener(ReviveWithPay);

            if(PlayerDataManager.Inst.UserData.RemoveAds)
            {
                mAdsButton.gameObject.SetActive(false);
                mAdsFreeButton.gameObject.SetActive(true);

                mActiveAdsButton = mAdsFreeButton;
            }
            else
            {
                mAdsFreeButton.gameObject.SetActive(false);
                mAdsButton.gameObject.SetActive(true);

                mActiveAdsButton = mAdsButton;
            }

            mAdsButton.onClick.AddListener(ReviveWithAds);

            mAdsFreeButton.onClick.AddListener(ReviveWithAdsFree);

            mFreeButton.onClick.AddListener(ReviveWithFree);

            mCancleButton.onClick.AddListener(Hide);
        }
        public override void Show()
        {
            Refresh();

            base.Show();
        }
        protected override void Refresh()
        {
            base.Refresh();

            int gold = PlayerDataManager.Inst.Gold;
            mGoldText.text = gold.ToString();

            if(GameManager.Instance.FreeReviveStage)
            {
                mFreeButton.gameObject.SetActive(true);

                mActiveAdsButton.gameObject.SetActive(false);
                mCostButton.gameObject.SetActive(false);
                return;
            }
            else
            {
                mFreeButton.gameObject.SetActive(false);
            }

            int index = GameManager.Instance.CurrentReviveCount;
            if(index >= 3)
            {
                index = 2;
            }
            if(mbAdsFreeDone)
            {
                mActiveAdsButton.interactable = false;
                mActiveAdsButton.GetComponent<CanvasGroup>().alpha = 0.5f;
                mActiveAdsButton.transform.Find("Text_Count").GetComponent<TMP_Text>().text = "(0/1)";
            }

            int cost = GameManager.Instance.ReviveCost[index];
            mReviveCostText.text = cost.ToString();
            if(gold >= cost)
            {
                mReviveCostText.color = Color.white;
            }
            else
            {
                mReviveCostText.color = Color.red;
            }
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            if(mEGameOverType == EGameOverType.TimeOut)
            {
                EventManager.Inst.AddEvent(EventKeys.GAME_OVER_TIME_OUT, Show);
            }
            else if(mEGameOverType == EGameOverType.SlotFull)
            {
                EventManager.Inst.AddEvent(EventKeys.GAME_OVER_SLOT_FULL, Show);
            }
            EventManager.Inst.AddEvent(EventKeys.RESTART_LEVEL, OnRestartLevel);
            PlayerDataManager.Inst.OnGoldChanged += Refresh;
        }

        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            if(mEGameOverType == EGameOverType.TimeOut)
            {
                EventManager.Inst.RemoveEvent(EventKeys.GAME_OVER_TIME_OUT, Show);
            }
            else if(mEGameOverType == EGameOverType.SlotFull)
            {
                EventManager.Inst.RemoveEvent(EventKeys.GAME_OVER_SLOT_FULL, Show);
            }
            EventManager.Inst.RemoveEvent(EventKeys.RESTART_LEVEL, OnRestartLevel);

            if(PlayerDataManager.Inst != null)
            {
                PlayerDataManager.Inst.OnGoldChanged -= Refresh;
            }
        }

        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                MarkClosed();
                gameObject.SetActive(false);
                EventManager.Inst.ActiveEvent(EventKeys.STAGE_FAILED);
            });   
        }

        private void ReviveWithPay()
        {
            int gold = PlayerDataManager.Inst.Gold;

            int index = GameManager.Instance.CurrentReviveCount;
            if(index >= 3)
            {
                index = 2;
            }

            int cost = GameManager.Instance.ReviveCost[index];

            if(gold < cost)
            {
                EventManager.Inst.ActiveEvent(EventKeys.ACCESS_SHOP_VIEW);
                return;
            }

            PlayerDataManager.Inst.UseGold(cost);

            //골드 부활 사용 횟수. 골드가 실제로 차감된 뒤에만 집계한다.
            FirebaseAnalyticsService.LogStageReviveGold(GameManager.Instance.CurrentLevel, cost);

            GameManager.Instance.CurrentReviveCount++;

            OnReviveAfterHide();
        }
        private void ReviveWithAds()
        {
            AdManager.Inst.ShowRewardedAd((bool done) =>
            {
                if(done)
                {
                    //광고 부활 사용 횟수. 보상을 실제로 받은 경우에만 집계한다.
                    //(광고 미준비/중간 이탈은 done=false로 들어오므로 제외된다)
                    FirebaseAnalyticsService.LogStageReviveAd(GameManager.Instance.CurrentLevel);

                    mbAdsFreeDone = true;
                    OnReviveAfterHide();
                }
            });
        }
        private void ReviveWithAdsFree()
        {
            mbAdsFreeDone = true;
            OnReviveAfterHide();
        }
        private void ReviveWithFree()
        {
            OnReviveAfterHide();
        }
        private void OnReviveAfterHide()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                MarkClosed();
                gameObject.SetActive(false);
                GameManager.Instance.ContinueGame();
            });   
        }
        private void OnRestartLevel()
        {
            mbAdsFreeDone = false;

            mActiveAdsButton.interactable = true;
            mActiveAdsButton.GetComponent<CanvasGroup>().alpha = 1f;
            mActiveAdsButton.transform.Find("Text_Count").GetComponent<TMP_Text>().text = "(1/1)";
        }
    }    
}

