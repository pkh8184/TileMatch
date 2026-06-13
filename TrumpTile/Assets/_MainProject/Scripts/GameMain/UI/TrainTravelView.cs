using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using DG.Tweening;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class TrainTravelView : ViewBase
    {
        [Header("Show 애니메이션을 위한 참조들")]
        [SerializeField] private ScrollRect mScrollRect;
        [SerializeField] private RectTransform mContentRect;
        [SerializeField] private TemporaryContentUIController mContentController;

        [Header("컨텐츠 해금 팝업 프리팹")]
        [SerializeField] private GameObject mUnlockPopupPrefab;

        [Header("보상 프리팹")]
        [SerializeField] private GameObject mRewardPrefab;
        [Header("보상 프리팹 부모 스크롤뷰 트랜스폼")]
        [SerializeField] private Transform mRewardParent;

        private Queue<ExcitTravelRewardUI> mRewardUIQueue = new Queue<ExcitTravelRewardUI>();
        private Sequence mShowAnimSeq; 
        private ExcitTravelContent mContentData;

        private float mActiveTime;
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<ExcitTravelContent>("ExcitTravel");
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
            mActiveTime = mContentData.GetContentInfo().ActiveTime;
            mShowButton.gameObject.SetActive(true);

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentController.PlayShowButtonAnim(mShowButton);

            mContentController.SetLimitTimeText(mActiveTime);

            if(mContentData.ShowUnlockPopup)
            {
                GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
                UIBase ui = obj.GetComponent<UIBase>();
                ui.Initialize();
                ui.Show();
            }
            CreateRewards();
        }
        public override void Show()
        {
            base.Show();

            AdManager.Inst.HideBannerAd();

            if(mShowAnimSeq != null && mShowAnimSeq.active)
            {
                mShowAnimSeq.Kill();
            }
            mScrollRect.enabled = false;
            mContentRect.anchoredPosition = new Vector2(0, -Screen.height);

            mShowAnimSeq = DOTween.Sequence();
            mShowAnimSeq.Append(mContentRect.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutQuad));
            mShowAnimSeq.OnComplete(() => mScrollRect.enabled = true);
        }
        public override void Hide()
        {
            base.Hide();

            AdManager.Inst.ShowBannerAd();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("PurchaseSuccess", OnPurchaseSuccess);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("PurchaseSuccess", OnPurchaseSuccess);
        }
        private void CreateRewards()
        {
            int start = mContentData.CurrentIndex;
            int max = mContentData.GetRewardCount();
            int interval = mContentData.GetPaidInterval();
            string cost = "무료";
            List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
            for(int i = start; i < max; i++)
            {
                int modifiedIndex = i + 1;
                bool isFree = modifiedIndex % interval != 0;

                if(isFree)
                {
                    modifiedIndex = i - modifiedIndex / interval;
                    Debug.Log($"Free index : {i}, {modifiedIndex}");
                    infos.Add(mContentData.GetRewardDisplayInfos(modifiedIndex));
                    cost = "무료";
                }
                else
                {
                    modifiedIndex = modifiedIndex / interval - 1;
                    Debug.Log($"Paid index : {i}, {modifiedIndex}");
                    EProductId id = mContentData.GetProductId(modifiedIndex);
                    infos = IAPManager.Instance.GetRewardDisplayInfos(id);
                    cost = IAPManager.Instance.GetProductPrice(id);
                    
                }
                ExcitTravelRewardUI ui = Instantiate(mRewardPrefab, mRewardParent).GetComponent<ExcitTravelRewardUI>();

                ui.Initialize(isFree, infos, OnConfirmButtonClick, cost);

                mRewardUIQueue.Enqueue(ui);

                infos.Clear();
            }

            mRewardUIQueue.Peek().SetValid();
        }
        private void OnConfirmButtonClick()
        {
            ExcitTravelRewardUI peek = mRewardUIQueue.Peek();
            if(peek.IsFree)
            {
                peek.gameObject.SetActive(false);
                mRewardUIQueue.Dequeue();
                mRewardUIQueue.Peek().SetValid();

                gameObject.SetActive(false);

                mContentData.ConfirmCurrentReward();
                return;
            }
            mContentData.ConfirmCurrentReward();
        }
        private void OnPurchaseSuccess()
        {
            mContentData.OnPurchaseSuccess();
            
            mRewardUIQueue.Peek().gameObject.SetActive(false);
            mRewardUIQueue.Dequeue();
            mRewardUIQueue.Peek().SetValid();

            Hide();
        }
    }    
}
