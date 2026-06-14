using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
    public class RemoveAdsPopup : PopupBase
    {
        [Header("오브젝트 애니메이션을 위한 참조")]
        [SerializeField] private RectTransform mIconRect;
        [Header("컨텐츠 UI 전용 컴포넌트")]
        [SerializeField] private PermanentContentUIController mContentController;
        [Header("구매 버튼")]
        [SerializeField] private Button mPurchaseButton;
        private Sequence mSeq;
        public override void Initialize()
        {
            base.Initialize();

            mContentController.PlayShowButtonAnim(mShowButton);

            if(mPurchaseButton != null)
            {
                mPurchaseButton.onClick.AddListener(OnPurchaseButtonClick);
            }
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
    }    
}

