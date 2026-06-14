using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;
using DG.Tweening;
using TrumpTile.GameMain.Core;
using System;

namespace TrumpTile.GameMain.UI
{
    public class ShopView : ViewBase
    {
        [Header("목록에 맞는 제품 생성을 위한 프리팹")]
        [SerializeField] private GameObject mBundlePrefab;
        [SerializeField] private GameObject mSinglePrefab;

        [Header("ShopView 접근 시 애니메이션 효과를 주기 위한 RectTransform 참조")]
        [SerializeField] private RectTransform mUIContainerTransform;
        [SerializeField] private float mAnimDuration = 0.5f;
        [Serializable]
        private class PurchaseButtonConfig
        {
            public EProductId eProductId;
            public Button button;
        }
        [SerializeField] private PurchaseButtonConfig[] mPurchaseButtonCofigArray;

        private EProductId mCurrentPurchaseProductId = EProductId.None;
        public override void Initialize()
        {
            base.Initialize();

            foreach(var item in mPurchaseButtonCofigArray)
            {
                if(IAPManager.Instance != null)
                {
                    item.button.GetComponentInChildren<TMP_Text>().text = IAPManager.Instance.GetProductPrice(item.eProductId);   
                }
                item.button.onClick.AddListener(() => OnPurchaeButtonClick(item.eProductId));
            }
        }
        public override void Show()
        {
            base.Show();

            ScrollRect scroll = GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = 1f;
            scroll.enabled = false;

            AdManager.Inst.HideBannerAd();
            StartCoroutine(Co_PlayPackageShowAnim(scroll));
        }
        public override void Hide()
        {
            base.Hide();

            if(GameManager.Instance != null)
            {
                if(!PopupBase.IsAnyPopupOpen)
                {
                    GameManager.Instance.ResumeGame();
                }
            }
            AdManager.Inst.ShowBannerAd();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("AccessShopView", Show);
            EventManager.Inst.AddEvent("PurchaseSuccess", OnPurchaseSuccess);
           // EventManager.Inst.AddEvent("ShopView", Show);    
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("AccessShopView", Show);
            EventManager.Inst?.RemoveEvent("PurchaseSuccess", OnPurchaseSuccess);
        }
        private void OnPurchaeButtonClick(EProductId eProductId)
        {
            IAPManager.Instance.PurchaseProduct(eProductId);
            mCurrentPurchaseProductId = eProductId;
        }
        private void OnPurchaseSuccess()
        {
            if(mCurrentPurchaseProductId == EProductId.None)
            {
                return;
            }

            List<ProductReward> rewards = IAPManager.Instance.GetProductRewads(mCurrentPurchaseProductId);
            foreach(var item in rewards)
            {
                CoreContainer.RewardContainer.AddElement(item);
            }
            EventManager.Inst.ActiveEvent("GetPackageReward", CoreContainer.RewardContainer.GetContainer());
            CoreContainer.RewardContainer.Clear();

            Hide();
        }
        private IEnumerator Co_PlayPackageShowAnim(ScrollRect scroll)
        {
            VerticalLayoutGroup layoutGroup = mUIContainerTransform.GetComponent<VerticalLayoutGroup>();

            layoutGroup.enabled = false;

            Sequence sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            for (int i = 0; i < 6; i++)
            {
                RectTransform rect = mUIContainerTransform.GetChild(i).GetComponent<RectTransform>();

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + Screen.width, rect.anchoredPosition.y);
                sequence.Insert(i * 0.1f, rect.DOAnchorPosX(rect.anchoredPosition.x - Screen.width, 0.3f).SetEase(Ease.OutQuad));
            }
            sequence.OnComplete(() => layoutGroup.enabled = true);

            yield return sequence.WaitForCompletion();

            scroll.enabled = true;
        }
        /// <summary>
        /// 제품 목록에 존재하는 제품들의 UI를 종류에 맞게 생성합니다.
        /// 생성과 동시에 구매 버튼에 제품의 가격으로 구매 이벤트를 추가해줍니다.
        /// </summary>
        private void CreateProduct()
        {

        }
    }
}

