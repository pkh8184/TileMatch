using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupBase : UIBase
    {
        protected static int mOpenPopupCount = 0;
        public static bool IsAnyPopupOpen => mOpenPopupCount > 0;

        [Header("Popup 켜기, 끄기 애니메이션 길이(초)")]
        [SerializeField] protected float mShowDuration = 1f;
        [SerializeField] protected float mHideDuration = 1f;

        [Header("Show / Hide 애니메이션을 적용할 실제 팝업창")]
        [SerializeField] protected GameObject mPopupObj;

        protected Sequence mCurrentSeq;

        public override void Initialize()
        {
            base.Initialize();

            mOpenPopupCount = 0;
        }

        public override void Show()
        {
            base.Show();
            mOpenPopupCount++;

            PlayShowAnim();
        }

        public override void Hide()
        {
            SetInteractable(false);
            PlayHideAnim();
        }

        protected virtual void OnDestroy()
        {
            mCurrentSeq?.Kill();
        }

        protected virtual void PlayShowAnim()
        {   
            if (mPopupObj == null) return;
            
            mCurrentSeq?.Kill();
            mPopupObj.transform.localScale = Vector2.zero;

            mCurrentSeq = DOTween.Sequence();
            mCurrentSeq.SetUpdate(true);
            mCurrentSeq.Append(mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack));
            mCurrentSeq.OnComplete(() => SetInteractable(true));
        }

        protected virtual void PlayHideAnim()
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
            });
        }
    }
}
