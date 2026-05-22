using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class PiggyPackagePopup : PopupBase
    {
        [Header("애니메이션 효과를 줄 이미지들")]
        [SerializeField] private RectTransform mPigRect;
        [SerializeField] private RectTransform mLeftGoldRect;
        [SerializeField] private RectTransform mRightGoldRect;
        [SerializeField] private TemporaryContentUIController mContentController;

        private Sequence mShowAnimSeq;
        public override void Initialize()
        {
            base.Initialize();

            mContentController.PlayShowButtonAnim(mShowButton);
        }
         protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;
            mPigRect.anchoredPosition = Vector2.up * 500;
            if(mShowAnimSeq != null && mShowAnimSeq.active)
            {
                mShowAnimSeq.Kill();
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
        }
    }    
}

