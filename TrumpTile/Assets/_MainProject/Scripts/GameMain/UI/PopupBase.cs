using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace TrumpTile.GameMain.UI
{
    public class PopupBase : UIBase
    {
        protected static int mOpenPopupCount = 0;
        public static bool IsAnyPopupOpen => mOpenPopupCount > 0;

        [Header("Popup 켜기, 끄기 애니메이션 길이(초)")]
        [SerializeField] protected float mShowDuration = 1f;
        [SerializeField] protected float mHideDuration = 1f;

        [Header("Show / Hide 애니메이션을 적용할 실제 팝업창")]
        [SerializeField] protected GameObject mPopupObj;
        public override void Show()
        {
            base.Show();
            mOpenPopupCount++;
            PlayShowAnim();
        }
        public override void Hide()
        {
            PlayHideAnim();
        }

        protected virtual void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;

            mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
        protected virtual void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);
            });         
        }
    }
}
