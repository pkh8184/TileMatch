using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using System.Collections.Generic;
using TMPro;

namespace TrumpTile.GameMain.UI
{
    public class RemoveAdsPopup : PopupBase, IPurchasable
    {
        [Header("오브젝트 애니메이션을 위한 참조")]
        [SerializeField] private RectTransform mIconRect;
        [Header("컨텐츠 UI 전용 컴포넌트")]
        [SerializeField] private PermanentContentUIController mContentController;
        [Header("구매 버튼")]
        [SerializeField] private Button mPurchaseButton;
        [Header("가격 텍스트")]
        [SerializeField] private TMP_Text mPriceText;
        [Header("말풍선 오브젝트들 / 버튼들")]
        [SerializeField] private RectTransform mGoldRect;
        [SerializeField] private RectTransform mRemoveAdsRect;
        [SerializeField] private RectTransform mSlotRect;
        [SerializeField] private Button mGoldButton;
        [SerializeField] private Button mRemoveAdsButton;
        [SerializeField] private Button mSlotButton;

        private Sequence mSeq;
        private Sequence mBallonSequence;
        // 인스펙터 직렬화된 Y를 Rect별 최초 1회 캐싱. 이후 매번 같은 기준으로 사용해 누적 누락.
        private readonly Dictionary<RectTransform, float> mBallonBaseYMap = new Dictionary<RectTransform, float>();
        public override void Initialize()
        {
            base.Initialize();

            if(PlayerDataManager.Inst.IsAdsRemoved)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }
            mContentController.PlayShowButtonAnim(mShowButton);

            if(mPurchaseButton != null)
            {
                mPurchaseButton.onClick.AddListener(OnPurchaseButtonClick);
            }
            mPriceText.text = IAPManager.Instance.GetProductPrice(EProductId.RemoveAds);

            mGoldButton.onClick.AddListener(OnGoldButton);
            mRemoveAdsButton.onClick.AddListener(OnRemoveAdsButton);
            mSlotButton.onClick.AddListener(OnSlotButton);
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
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf) return;
            List<RewardDisplayInfo> rewards = IAPManager.Instance.GetRewardDisplayInfos(EProductId.RemoveAds);
            
            foreach(var item in rewards)
            {
                CoreContainer.RewardContainer.AddReward(item);
            }

            HideOnPurchase();
        }
        private void HideOnPurchase()
        {   
            if (mPopupObj == null) return;

            mCurrentSeq?.Kill();

            mCurrentSeq = DOTween.Sequence();
            mCurrentSeq.SetUpdate(true);
            mCurrentSeq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            mCurrentSeq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);
                EventManager.Inst.ActiveEvent("PlayRewardAnim");
                mShowButton.gameObject.SetActive(false);
            });
        }
        private void OnPurchaseButtonClick()
        {
            IAPManager.Instance.PurchaseProduct(EProductId.RemoveAds);
        }
        protected override void PlayShowAnim()
        {
            base.PlayShowAnim();

            if(mSeq != null && mSeq.active)
            {
                mSeq.Kill();
            }
            mIconRect.anchoredPosition = Vector2.zero;
            mIconRect.localScale = Vector2.one;

            mSeq = DOTween.Sequence();
            mSeq.Append(mIconRect.DOAnchorPosY(30, 1.5f).SetEase(Ease.InOutSine));
            mSeq.Append(mIconRect.DOAnchorPosY(0, 1.5f).SetEase(Ease.InOutSine));
            mSeq.Insert(0, mIconRect.DOScale(Vector2.one * 1.1f, 1f).SetEase(Ease.InOutSine));
            mSeq.Insert(1f, mIconRect.DOScale(Vector2.one, 1f).SetEase(Ease.InOutSine));

            mSeq.SetLoops(-1);
        }
        private void OnGoldButton()
        {
            mRemoveAdsRect.gameObject.SetActive(false);
            mSlotRect.gameObject.SetActive(false);

            PlayBallonAnim(mGoldRect);
        }
        private void OnRemoveAdsButton()
        {
            mGoldRect.gameObject.SetActive(false);
            mSlotRect.gameObject.SetActive(false);

            PlayBallonAnim(mRemoveAdsRect);
        }
        private void OnSlotButton()
        {
            mGoldRect.gameObject.SetActive(false);
            mRemoveAdsRect.gameObject.SetActive(false);

            PlayBallonAnim(mSlotRect);
        }
        private void PlayBallonAnim(RectTransform ballonRect)
        {
            if(!mBallonBaseYMap.TryGetValue(ballonRect, out float baseY))
            {
                baseY = ballonRect.anchoredPosition.y;
                mBallonBaseYMap.Add(ballonRect, baseY);
            }

            if(mBallonSequence != null && mBallonSequence.IsActive())
            {
                mBallonSequence.Kill();
            }

            ballonRect.anchoredPosition = new Vector2(ballonRect.anchoredPosition.x, baseY);
            ballonRect.localScale = Vector3.zero;
            ballonRect.localRotation = Quaternion.Euler(0f, 0f, -8f);

            ballonRect.gameObject.SetActive(true);

            mBallonSequence = DOTween.Sequence();
            // 등장: 회전 풀기 + 스케일 튀어오름 + 위로 살짝 떠오름
            mBallonSequence.Append(ballonRect.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2.5f));
            mBallonSequence.Join(ballonRect.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutCubic));
            mBallonSequence.Join(ballonRect.DOAnchorPosY(baseY + 4f, 0.35f).SetEase(Ease.OutCubic));
            // 흔들림 1회 (살짝 내려갔다 다시 위로)
            mBallonSequence.Append(ballonRect.DOAnchorPosY(baseY - 2f, 0.25f).SetEase(Ease.InOutSine));
            mBallonSequence.Append(ballonRect.DOAnchorPosY(baseY + 4f, 0.25f).SetEase(Ease.InOutSine));
            // 잠시 유지
            mBallonSequence.AppendInterval(0.3f);
            // 사라짐: 안으로 빨려들어가듯
            mBallonSequence.Append(ballonRect.DOScale(0f, 0.22f).SetEase(Ease.InBack));
            mBallonSequence.OnComplete(() => ballonRect.gameObject.SetActive(false));
        }
    }    
}

