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
    public class TrainTravelView : ViewBase, IPurchasable
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

        private List<ExcitTravelRewardUI> mRewardUIList = new List<ExcitTravelRewardUI>();
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
            mActiveTime = mContentData.GetRemainTimeSeconds();
            mShowButton.gameObject.SetActive(true);

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentController.PlayShowButtonAnim(mShowButton);

            mContentController.SetLimitTimeText(mActiveTime);

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

            EventManager.Inst.ActiveEvent(EventKeys.PLAY_REWARD_ANIM);
            AdManager.Inst.ShowBannerAd();
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
        private void CreateRewards()
        {
            int start = mContentData.CurrentIndex;
            int max = mContentData.GetRewardCount();
            int interval = mContentData.GetPaidInterval();
            string cost = "무료";
            List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();

            int count = 0;
            for(int i = start; i < max; i++)
            {
                int modifiedIndex = i + 1;
                bool isFree = modifiedIndex % interval != 0;

                if(isFree)
                {
                    modifiedIndex = i - modifiedIndex / interval;
                    infos.Add(mContentData.GetRewardDisplayInfo(modifiedIndex));
                    cost = "무료";
                }
                else
                {
                    modifiedIndex = modifiedIndex / interval - 1;
                    EProductId id = mContentData.GetProductId(modifiedIndex);
                    infos = IAPManager.Instance.GetRewardDisplayInfos(id);
                    cost = IAPManager.Instance.GetProductPrice(id);
                    
                }
                ExcitTravelRewardUI ui = Instantiate(mRewardPrefab, mRewardParent).GetComponent<ExcitTravelRewardUI>();

                ui.Initialize(isFree, infos, OnConfirmButtonClick, cost);

                mRewardUIList.Add(ui);

                if(count >= 3)
                {
                    ui.gameObject.SetActive(false);
                }

                infos.Clear();

                count++;
            }

            if(mRewardUIList.Count > 0)
            {
                mRewardUIList[0].SetValid();
            }
        }
        private void OnConfirmButtonClick()
        {
            ExcitTravelRewardUI peek = mRewardUIList[0];
            if(peek.IsFree)
            {
                mContentData.ConfirmCurrentReward();
                PeekProgress();
                AudioEvent.Play(EAudioKey.SFX_Reward_Gain);
                return;
            }
            mContentData.ConfirmCurrentReward();
        }
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf) return;

            mContentData.OnPurchaseSuccess();
            
            PeekProgress();
        }
        private void PeekProgress()
        {
            SetInteractable(false);

            Sequence seq = DOTween.Sequence();
            seq.Append(mRewardUIList[0].transform.DOScale(0, 0.5f).SetEase(Ease.InBack));

            if(mRewardUIList.Count >= 4)
            {
                mRewardUIList[3].transform.localScale = Vector2.zero;
                mRewardUIList[3].gameObject.SetActive(true);
                seq.Insert(0.25f, mRewardUIList[3].transform.DOScale(1, 0.5f).SetEase(Ease.OutBack));   
            }

            seq.OnComplete(() =>
            {
                mRewardUIList[0].gameObject.SetActive(false);
                mRewardUIList.RemoveAt(0);
                if(mRewardUIList.Count == 0)
                {
                    Hide();
                    mShowButton.gameObject.SetActive(false);
                }
                else
                {
                    mRewardUIList[0].SetValid(); 
                    SetInteractable(true);
                }
            });
        }
    }    
}
