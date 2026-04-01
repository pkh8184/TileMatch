using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace TrumpTile.GameMain.UI
{
    public class PopupBase : UIBase
    {
        [Header("Popup 켜기, 끄기 애니메이션 길이(초)")]
        [SerializeField] private float mShowDuration = 1f;
        [SerializeField] private float mHideDuration = 1f;

        [Header("Show / Hide 애니메이션을 적용할 실제 팝업창")]
        [SerializeField] private GameObject mPopupObj;
        public override void Show()
        {
            base.Show();

            PlayShowAnim();
        }
        public override void Hide()
        {
            PlayHideAnim();
        }

        private void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;

            mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack);
        }
        private void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() => gameObject.SetActive(false));
        }
    }
}
