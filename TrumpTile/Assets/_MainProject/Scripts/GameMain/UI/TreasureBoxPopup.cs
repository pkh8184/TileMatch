using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TrumpTile.FirebaseLibrary;
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
            //최초 해금 시에만 자동 표시한다. (이후 메인씬 진입에서는 ShowButton으로만 접근)
            if(!TreasureBoxContent.ShouldAutoShowOnMainSceneEnter(mContentData.Unlock, mContentData.IsActive, mContentData.ShowUnlockPopup))
            {
                yield break;
            }

            Show();
            yield return new WaitWhile(() => gameObject.activeSelf);
        }
        public override void Show()
        {
            base.Show();

            //트레져박스 UI 오픈 횟수
            FirebaseAnalyticsService.LogContentEvent(FirebaseAnalyticsEvents.TREASURE_BOX_OPEN);
        }
        private void OnPurchaseButton()
        {
            //트레져박스 구매 버튼 클릭 횟수
            FirebaseAnalyticsService.LogContentEvent(FirebaseAnalyticsEvents.TREASURE_BOX_CLICK);

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
                MarkClosed();
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

