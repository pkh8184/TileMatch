using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TMPro;

namespace TrumpTile.GameMain.UI
{
    public class TreasureBoxPopup : PopupBase, IPurchasable
    {
        [Header("구매 버튼")]
        [SerializeField] private Button mPurchaseButton;
        [Header("가격 텍스트")]
        [SerializeField] private TMP_Text mPriceText;
        [SerializeField] private PermanentContentUIController mContentController;
        private TreasureBoxContent mContentData;
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<TreasureBoxContent>("TreasureBox");
            if(mContentData == null)
            {
                Debug.Log($"[ExcitTravelView] 컨텐츠 데이터 읽어오기에 실패했습니다.");
                return;
            }

            if(!mContentData.Unlock || !mContentData.IsActive)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }
            mShowButton.gameObject.SetActive(true);
            mContentController.ActiveRedDot(true);
            mPriceText.text = IAPManager.Instance.GetProductPrice(EProductId.TreasureBox);
            mPurchaseButton.onClick.AddListener(() => OnPurchaseButton());

            MainManager.Instance.AddEvent(Co_MainSceneEnterEvent, EMainSceneEventType.TreasurePackProgress);
        }
        private IEnumerator Co_MainSceneEnterEvent()
        {
            Show();
            yield return new WaitWhile(() => gameObject.activeSelf);
        }
        private void OnPurchaseButton()
        {
            IAPManager.Instance.PurchaseProduct(EProductId.TreasureBox);
        }
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf) return;

            mContentData.OnPurchaseSuccess();
            mShowButton.gameObject.SetActive(false);

            Hide();
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

                EventManager.Inst.ActiveEvent(EventKeys.PLAY_REWARD_ANIM);
            }); 
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent(EventKeys.PURCHASE_SUCCESS, OnPurchaseSuccess);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent(EventKeys.PURCHASE_SUCCESS, OnPurchaseSuccess);
        }
    }
}

