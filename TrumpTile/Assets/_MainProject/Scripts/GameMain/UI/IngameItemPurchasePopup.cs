using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using UnityEngine;
using UnityEngine.UI;

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

        [Header("아이템 이름, 아이템 설명, 아이템 개수, 아이템 가격, 아이템 스프라이트(순서대로 망치, 시계, 모자, 폭탄)")]
        [SerializeField] private string[] mItemNameStringArray = new string[4];
        [SerializeField] private string[] mItemDescriptionStringArray = new string[4];
        [SerializeField] private int[] mItemCountArray = new int[4];
        [SerializeField] private int[] mItemCostArray = new int[4];
        [SerializeField] private Sprite[] mItemSpriteArray = new Sprite[4];

        private int mCurrentItemID;
        private bool mbCanPucrchase;
        public override void Initialize()
        {
            base.Initialize();    

            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("AccessShopView"));
            mPurchaseButton.onClick.AddListener(PurchaseProgress);
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();
            //임시
            EventManager.Inst.AddEvent("ShowItemPurchasePopup", obj => InitPopupDataBeforeShow(obj));
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("ShowItemPurchasePopup");
        }
        public override void Hide()
        {
            base.Hide();
            GameManager.Instance.ResumeGame();
        }
        private void InitPopupDataBeforeShow(object obj)
        {
            int index = (int)obj;

            mCurrentItemID = index;

            index -= 1005;
            if(index >= 4) return;

            RefreshGoldData(index);
            
            mItemNameText.text = mItemNameStringArray[index];
            
            mItemDescriptionText.text = mItemDescriptionStringArray[index];
            
            mItemCountText.text = "X" + mItemCountArray[index].ToString(); 
            
            mItemCostText.text = mItemCostArray[index].ToString();

            mItemImage.sprite = mItemSpriteArray[index];

            GameManager.Instance.PauseGame();
            
            Show();
        }

        private void PurchaseProgress()
        {
            if(!mbCanPucrchase)
            {
                EventManager.Inst.ActiveEvent("AccessShopView");
                return;
            }
            int index = mCurrentItemID - 1005;

            PlayerDataManager.Inst.UseGold(mItemCostArray[index]);
            
            ItemManager.Inst.AddItem(mCurrentItemID, mItemCountArray[index]);

            AudioEvent.Play(EAudioKey.SFX_Purchase);

            RefreshGoldData(index);

            Hide();
        }
        private void RefreshGoldData(int index)
        {
            mGoldText.text = PlayerDataManager.Inst.Gold.ToString();
            mbCanPucrchase = PlayerDataManager.Inst.Gold >= mItemCostArray[index];
            mItemCostText.color = mbCanPucrchase? Color.white : Color.red;
        }
    }    
}

