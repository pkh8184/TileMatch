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
    public class ShopView : ViewBase, IPurchasable
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
            [Tooltip("재구매 쿨타임(비활성) 중이면 스크롤 리스트에서 통째로 끌 패키지 레이아웃 루트")]
            public GameObject packageObject;
        }
        [SerializeField] private PurchaseButtonConfig[] mPurchaseButtonCofigArray;

        [Header("구매 불가(쿨타임 중) 패키지 버튼에 적용할 색")]
        [SerializeField] private Color mSoldOutColor = new Color(0.5f, 0.5f, 0.5f, 1f);

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

            //상점 접근 시마다 패키지 구매 가능 여부 갱신 (쿨타임 지난 것은 재구매 가능하도록 리셋 + 버튼 활성/비활성)
            UpdatePurchaseButtons();

            ScrollRect scroll = GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = 1f;
            scroll.enabled = false;

            AdManager.Inst.HideBannerAd();
            StartCoroutine(Co_PlayPackageShowAnim(scroll));
        }
        public override void Hide()
        {
            base.Hide();

            mCurrentPurchaseProductId = EProductId.None;

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

            EventManager.Inst.AddEvent(EventKeys.ACCESS_SHOP_VIEW, Show);
            EventManager.Inst.AddEvent(EventKeys.PURCHASE_SUCCESS, OnPurchaseSuccess);
           // EventManager.Inst.AddEvent("ShopView", Show);    
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent(EventKeys.ACCESS_SHOP_VIEW, Show);
            EventManager.Inst?.RemoveEvent(EventKeys.PURCHASE_SUCCESS, OnPurchaseSuccess);
        }
        private void OnPurchaeButtonClick(EProductId eProductId)
        {
            IAPManager.Instance.PurchaseProduct(eProductId);
            mCurrentPurchaseProductId = eProductId;
        }
        /// <summary>
        /// 패키지 구매 가능 여부에 따라 버튼 활성/비활성 처리.
        /// CanPurchasePackage 호출로 쿨타임이 지난 패키지는 여기서 자동으로 재구매 가능하게 리셋된다.
        /// (쿨타임이 없는 일반 상품은 항상 구매 가능하므로 영향 없음)
        /// </summary>
        private void UpdatePurchaseButtons()
        {
            foreach(var item in mPurchaseButtonCofigArray)
            {
                bool canBuy = PlayerDataManager.Inst.CanPurchasePackage(item.eProductId);

                //재구매 쿨타임(비활성) 중인 패키지는 스크롤 리스트에서 레이아웃 자체를 끈다.
                //(쿨타임 없는 패키지는 CanPurchasePackage가 항상 true라 그대로 보임)
                if(item.packageObject != null)
                {
                    item.packageObject.SetActive(canBuy);
                }

                item.button.interactable = canBuy;

                //UIBase가 disabledColor을 normalColor로 맞춰버리므로, 잠긴 버튼은 회색으로 되돌려준다.
                ColorBlock colors = item.button.colors;
                colors.disabledColor = canBuy ? colors.normalColor : mSoldOutColor;
                item.button.colors = colors;
            }
        }
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf)
            {
                return;    
            }
            if(mCurrentPurchaseProductId == EProductId.None)
            {
                return;
            }
            if(GameManager.Instance != null)
            {
                return;
            }
            List<RewardDisplayInfo> rewards = IAPManager.Instance.GetRewardDisplayInfos(mCurrentPurchaseProductId);
            foreach(var item in rewards)
            {
                CoreContainer.RewardContainer.AddReward(item);
            }
            EventManager.Inst.ActiveEvent(EventKeys.PLAY_REWARD_ANIM);

            Hide();
        }
        private IEnumerator Co_PlayPackageShowAnim(ScrollRect scroll)
        {
            VerticalLayoutGroup layoutGroup = mUIContainerTransform.GetComponent<VerticalLayoutGroup>();

            layoutGroup.enabled = false;

            Sequence sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            //꺼진(쿨타임) 패키지는 건너뛰고, 활성 패키지 기준으로 최대 6개까지 애니메이션한다.
            //(앞쪽이 꺼져 있으면 그 개수만큼 뒤 항목을 끌어와 총 6개를 채움)
            int animIndex = 0;
            for (int i = 0; i < mUIContainerTransform.childCount && animIndex < 6; i++)
            {
                GameObject child = mUIContainerTransform.GetChild(i).gameObject;
                if (!child.activeSelf)
                {
                    continue;
                }

                RectTransform rect = child.GetComponent<RectTransform>();

                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + Screen.width, rect.anchoredPosition.y);
                sequence.Insert(animIndex * 0.1f, rect.DOAnchorPosX(rect.anchoredPosition.x - Screen.width, 0.3f).SetEase(Ease.OutQuad));
                animIndex++;
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

