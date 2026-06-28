using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TMPro;

namespace TrumpTile.GameMain.UI
{
    public class IngameSlotPurchasePopup : PopupBase
    {
        [Header("버튼들")]
        [SerializeField] private Button mCancleButton;
        [SerializeField] private Button mConfirmButton;
        [SerializeField] private Button mShopButton;
        [Header("골드")]
        [SerializeField] private TMP_Text mGoldText;
        private bool mbPurchaseProgress;
        private GameObject mSlotButtonObject;
        public override void Initialize()
        {
            base.Initialize();

            mCancleButton.onClick.AddListener(Hide);
            mConfirmButton.onClick.AddListener(PurchaseSlot);
            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("AccessShopView"));
            RefreshGoldData();
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
        public void SetSlotButtonObject(GameObject obj)
        {
            mSlotButtonObject = obj;
        }
        private void PurchaseSlot()
        {
            mbPurchaseProgress = true;

            PlayerDataManager.Inst.UseGold(SlotManager.Instance.BonusSlotCost);

            if(DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
            {
                SlotManager.Instance?.SetSlotCount(5);
            }
            else
            {
                SlotManager.Instance?.SetSlotCount(7);   
            }
            
            mSlotButtonObject.SetActive(false);

            Hide();
        }
        private void RefreshGoldData()
        {
            mGoldText.text = PlayerDataManager.Inst.Gold.ToString();
        }
    }    
}

