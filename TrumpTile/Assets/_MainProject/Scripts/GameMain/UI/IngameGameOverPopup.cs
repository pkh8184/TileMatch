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
        [Header("부활 버튼들")]
        [SerializeField] private Button mCostButton;
        [SerializeField] private Button mAdsButton;
        [SerializeField] private Button mAdsFreeButton;
        [SerializeField] private Button mCancleButton;
        [Header("부활 비용 텍스트")]
        [SerializeField] private TMP_Text mReviveCostText;

        private Button mActiveAdsButton;
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
                mAdsFreeButton.onClick.AddListener(ReviveWithAds);
            }
            else
            {
                mAdsFreeButton.gameObject.SetActive(false);
                mAdsButton.gameObject.SetActive(true);

                mActiveAdsButton = mAdsButton;
                mAdsButton.onClick.AddListener(ReviveWithAds);
            }

            mCancleButton.onClick.AddListener(OnCancleButtonClick);
        }
        public override void Show()
        {
            Refresh();

            base.Show();
        }
        protected override void Refresh()
        {
            base.Refresh();
            int index = GameManager.Instance.CurrentReviveCount;
            if(index >= 3)
            {
                return;
            }
            if(index > 0)
            {
                mActiveAdsButton.interactable = false;
                mActiveAdsButton.GetComponent<CanvasGroup>().alpha = 0.5f;
            }

            int gold = PlayerDataManager.Inst.Gold;
            mGoldText.text = gold.ToString();

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
            if(PlayerDataManager.Inst != null)
            {
                PlayerDataManager.Inst.OnGoldChanged -= Refresh;
            }
        }

        private void ReviveWithPay()
        {
            int gold = PlayerDataManager.Inst.Gold;

            int index = GameManager.Instance.CurrentReviveCount;
            int cost = GameManager.Instance.ReviveCost[index];

            if(gold < cost)
            {
                EventManager.Inst.ActiveEvent("AccessShopView");
                return;
            }

            PlayerDataManager.Inst.UseGold(cost);

            Hide();

            GameManager.Instance.ContinueGame();
        }
        private void ReviveWithAds()
        {
            // 리워드 광고 진행
            // 리워드 광고 대기

            Hide();

            GameManager.Instance.ContinueGame();
        }
        private void OnCancleButtonClick()
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
    }    
}

