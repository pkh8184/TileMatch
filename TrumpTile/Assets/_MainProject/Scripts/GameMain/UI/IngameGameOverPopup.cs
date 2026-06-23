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

            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("AccessShopView"));

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
                EventManager.Inst.AddEvent("GameOver_TimeOut", Show);
            }
            else if(mEGameOverType == EGameOverType.SlotFull)
            {
                EventManager.Inst.AddEvent("GameOver_SlotFull", Show);
            }
            EventManager.Inst.AddEvent("RestartLevel", OnRestartLevel);
            PlayerDataManager.Inst.OnGoldChanged += Refresh;
        }

        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            if(mEGameOverType == EGameOverType.TimeOut)
            {
                EventManager.Inst.RemoveEvent("GameOver_TimeOut", Show);
            }
            else if(mEGameOverType == EGameOverType.SlotFull)
            {
                EventManager.Inst.RemoveEvent("GameOver_SlotFull", Show);
            }
            EventManager.Inst.RemoveEvent("RestartLevel", OnRestartLevel);

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
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);
                EventManager.Inst.ActiveEvent("StageFailed");
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
                EventManager.Inst.ActiveEvent("AccessShopView");
                return;
            }

            PlayerDataManager.Inst.UseGold(cost);
            
            GameManager.Instance.CurrentReviveCount++;

            OnReviveAfterHide();
        }
        private void ReviveWithAds()
        {
            AdManager.Inst.ShowRewardedAd((bool done) =>
            {
                if(done)
                {
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
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
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

