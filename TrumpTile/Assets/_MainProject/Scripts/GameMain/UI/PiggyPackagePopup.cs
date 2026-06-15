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
    public class PiggyPackagePopup : PopupBase, IPurchasable
    {
        [Header("애니메이션 효과를 줄 이미지들")]
        [SerializeField] private RectTransform mPigRect;
        [SerializeField] private RectTransform mLeftGoldRect;
        [SerializeField] private RectTransform mRightGoldRect;
        [SerializeField] private TemporaryContentUIController mContentController;

        [Header("축적된 돈 텍스트")]
        [SerializeField] private TMP_Text mStackedGoldText;
        [Header("구매 버튼")]
        [SerializeField] private Button mPurchaseButton;
        [Header("구매 버튼 텍스트")]
        [SerializeField] private TMP_Text mPurchaseButtonText;
        [Header("구매 완료 시 교체할 버튼 스프라이트")]
        [SerializeField] private Sprite mAfterPurchaseSprite;
        [Header("획득 진행도를 나타낼 슬라이더 / 체크포인트")]
        [SerializeField] private Slider mSlider;
        [SerializeField] private RectTransform[] mCheckPointRectArray;
        [Header("보상 수령 뷰")]
        [SerializeField] private PiggyBankRewardView mRewardProgressView;
        
        private Sequence mShowAnimSeq;
        private Sequence mProgressAnimSeq;
        private PiggyBankContent mContentData;
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<PiggyBankContent>("PiggyBank");
            if(mContentData == null)
            {
                Debug.Log($"[ExcitTravelView] 컨텐츠 데이터 읽어오기에 실패했습니다.");
                return;
            }
            if(mContentData.CanGetRewardAfterEndActive)
            {
                Show();
                RewardProgress();
                return;
            }
            if(mContentData.IsFull && mContentData.CanConfirm)
            {
                Show();
                RewardProgress();
                return;
            }

            if(!mContentData.Unlock || !mContentData.IsActive)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }
            mShowButton.gameObject.SetActive(true);

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentController.PlayShowButtonAnim(mShowButton);

            mContentController.SetLimitTimeText(mContentData.GetContentInfo().ActiveTime);

            SetState();
        }
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf) return;

            SetState();
            mContentController.ActiveRedDot(mContentData.HasNewThing);
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("PurchaseSuccess", OnPurchaseSuccess);
            EventManager.Inst.AddEvent("PiggyRewardConfirm", OnPiggyRewardConfirm);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("PurchaseSuccess", OnPurchaseSuccess);
            EventManager.Inst?.RemoveEvent("PiggyRewardConfirm", OnPiggyRewardConfirm);
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;
            mPigRect.anchoredPosition = Vector2.up * 500;
            if(mShowAnimSeq != null && mShowAnimSeq.active)
            {
                mShowAnimSeq.Kill();
            }

            mStackedGoldText.text = "0";
            mSlider.value = 0;
            foreach(var item in mCheckPointRectArray)
            {
                item.gameObject.SetActive(false);
            }
            if(mProgressAnimSeq != null && mProgressAnimSeq.active)
            {
                mProgressAnimSeq.Kill();
            }

            mShowAnimSeq = DOTween.Sequence();
            mShowAnimSeq.SetUpdate(true);

            mShowAnimSeq.Append(mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack));
            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.InQuad));

            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.up * 80, 0.1f).SetEase(Ease.OutQuad));
            mShowAnimSeq.Join(mLeftGoldRect.DOAnchorPos(Vector2.up * 60, 0.1f).SetEase(Ease.OutQuad));
            mShowAnimSeq.Join(mRightGoldRect.DOAnchorPos(Vector2.up * 60, 0.1f).SetEase(Ease.OutQuad));

            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));
            mShowAnimSeq.Join(mLeftGoldRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));
            mShowAnimSeq.Join(mRightGoldRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));

            mShowAnimSeq.OnComplete(() => PlayProgressAnim());
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

                EventManager.Inst.ActiveEvent("GetPackageReward", CoreContainer.RewardContainer.GetContainer());
                CoreContainer.RewardContainer.Clear();
            }); 
        }
        private void PlayProgressAnim()
        {
            mProgressAnimSeq = DOTween.Sequence();

            float value = 0;
            mProgressAnimSeq.Append(DOTween.To(() => value, x =>
            {
                value = x;
                mStackedGoldText.text = Mathf.RoundToInt(x).ToString();
            }, mContentData.GetCurrentStackedGold(), 0.5f));

            float sliderValue = (float)((float)mContentData.GetCurrentStageCount() /(float) mContentData.GetMaxCount());
            mProgressAnimSeq.Join(mSlider.DOValue(sliderValue, 0.5f));

            for(int i = 0; i <= mContentData.GetCurrentCheckPoint(); i++)
            {
                mCheckPointRectArray[i].localScale = Vector2.zero;
                mCheckPointRectArray[i].gameObject.SetActive(true);
                mProgressAnimSeq.Insert(0.1f * i, mCheckPointRectArray[i].DOScale(1, 0.2f).SetEase(Ease.OutBack));
            }

            mProgressAnimSeq.OnComplete(() => SetInteractable(true));
        }
        private void SetState()
        {
            if(mContentData.CanConfirm)
            {
                mPurchaseButton.image.sprite = mAfterPurchaseSprite;
                mPurchaseButtonText.text = "구매 완료";
                mPurchaseButton.onClick.RemoveAllListeners();
                if(mContentData.IsFull)
                {
                    RewardProgress();
                }
            }
            else
            {
                mPurchaseButtonText.text = IAPManager.Instance.GetProductPrice(EProductId.PiggyBank);
                mPurchaseButton.onClick.AddListener(() => IAPManager.Instance.PurchaseProduct(EProductId.PiggyBank));
            }    
        }
        private void RewardProgress()
        {
            mRewardProgressView.SetView(mContentData.GetCurrentStackedGold());
            mRewardProgressView.Show();
        }
        private void OnPiggyRewardConfirm()
        {
            SetInteractable(false);
            mContentData.PiggyBankRewardProgress();

            mShowButton.gameObject.SetActive(false);

            Hide();
        }
    }    
}

