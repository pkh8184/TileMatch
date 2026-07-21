using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    public class IngameItemPurchasePopup : PopupBase
    {
        [Header("보유 골드 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [Header("상점 버튼")]
        [SerializeField] private Button mShopButton;

        [Header("아이템 구매 관련 텍스트들")]
        [SerializeField] private TMP_Text mItemNameText;
        [SerializeField] private TMP_Text mItemDescriptionText;
        [SerializeField] private TMP_Text mItemCountText;
        [SerializeField] private TMP_Text mItemCostText;

        [Header("아이템 이미지")]
        [SerializeField] private Image mItemImage;
        [Header("아이템 구매 버튼")]
        [SerializeField] private Button mPurchaseButton;

        private int mCurrentItemID;
        private int mCurrentCost;
        private int mCurrentAmount;
        private bool mbCanPucrchase;

        private bool mbPurchaseProgress;

        public override void Initialize()
        {
            base.Initialize();    

            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent(EventKeys.ACCESS_SHOP_VIEW));
            mPurchaseButton.onClick.AddListener(PurchaseProgress);

            RefreshGoldData();
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
                GameManager.Instance.ResumeGame();
                mbPurchaseProgress = false;
            });         
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            PlayerDataManager.Inst.OnGoldChanged += RefreshGoldData;
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            if(PlayerDataManager.Inst != null)
            {
                PlayerDataManager.Inst.OnGoldChanged -= RefreshGoldData;
            }
        }
        public void SetValid(IngameItemConfig config)
        {
            mItemNameText.text = config.itemName;
            mItemDescriptionText.text = config.itemDescription;
            mItemCountText.text = "X" + config.amount.ToString();
            mCurrentAmount = config.amount;
            mItemCostText.text = config.cost.ToString();
            mCurrentCost = config.cost;
            mItemImage.sprite = config.itemIcon;
            mCurrentItemID = config.itemId;

            RefreshGoldData();
        }
        private void PurchaseProgress()
        {
            if(mbPurchaseProgress)
            {
                return;
            }
            if(!mbCanPucrchase)
            {
                EventManager.Inst.ActiveEvent(EventKeys.ACCESS_SHOP_VIEW);
                return;
            }

            mbPurchaseProgress = true;

            PlayerDataManager.Inst.UseGold(mCurrentCost);
            
            ItemManager.Inst.AddItem(mCurrentItemID, mCurrentAmount);

            AudioEvent.Play(EAudioKey.SFX_Purchase);

            Hide();
        }
        private void RefreshGoldData()
        {
            mGoldText.text = PlayerDataManager.Inst.Gold.ToString();
            mbCanPucrchase = PlayerDataManager.Inst.Gold >= mCurrentCost;
            mItemCostText.color = mbCanPucrchase? Color.white : Color.red;
        }
    }    
}

