using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        private Sequence mShowAnimSeq; 
        public override void Initialize()
        {
            base.Initialize();

            mContentController.PlayShowButtonAnim(mShowButton);
        }
        public override void Show()
        {
            base.Show();

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
    }    
}
