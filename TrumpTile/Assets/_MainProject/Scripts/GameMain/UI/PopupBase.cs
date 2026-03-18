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

        [Header("Popup 뒷배경 오브젝트")]
        [SerializeField] private GameObject background;
        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void Show()
        {
            base.Show();

            StartCoroutine(Co_PlayShowAnim());
        }
        protected override void Hide()
        {
            StartCoroutine(Co_PlayHideAnim());
        }

        private IEnumerator Co_PlayShowAnim()
        {
            background.SetActive(true);

            transform.localScale = Vector2.zero;

            transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(mShowDuration);

        }
        private IEnumerator Co_PlayHideAnim()
        {
            transform.DOScale(0, mHideDuration).SetEase(Ease.InBack);

            yield return new WaitForSeconds(mHideDuration);

            gameObject.SetActive(false);
            background.SetActive(false);
        }
    }
}
