using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class SlotTutorialPopup : PopupBase
    {
        [Header("OK 버튼")]
        [SerializeField] private Button mOKButton;
        [Header("애니메이션 타겟 렉트 트랜스폼")]
        [SerializeField] private RectTransform mAnimTargetRectTransform;
        [Header("이동 위치 렉트 트랜스폼")]
        [SerializeField] private RectTransform mMoveTargetRectTransform;
        [Header("인디케이터 렉트 트랜스폼")]
        [SerializeField] private RectTransform mIndicatorRectTransform;
        [Header("X 표시 아이콘 이미지")]
        [SerializeField] private Image mXIconImage;

        public override void Initialize()
        {
            base.Initialize();

            mOKButton.onClick.AddListener(() => Hide());
        }

        protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack));

            seq.OnComplete(() => StartCoroutine(Co_PlaySlotTutorialAnim()));
        }
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                GameManager.Instance.tutorialComplete = true;
                gameObject.SetActive(false);
            });
        }
        private IEnumerator Co_PlaySlotTutorialAnim()
        {
            while (true)
            {
                Sequence seq = DOTween.Sequence();
             
                seq.AppendInterval(0.5f);

                seq.Append(mIndicatorRectTransform.DOLocalMove(mAnimTargetRectTransform.localPosition + new Vector3(60, 0, 0), 0.5f));
                seq.Append(mIndicatorRectTransform.DOScale(0.85f, 0.1f));
                seq.Append(mIndicatorRectTransform.DOScale(1, 0.1f));

                seq.Append(mAnimTargetRectTransform.DOPunchRotation(new Vector3(0, 0, 10f), 0.5f, 10, 0.5f));

                seq.AppendInterval(0.3f);
                seq.Append(mAnimTargetRectTransform.DOMove(mMoveTargetRectTransform.position, 0.5f));
                seq.AppendInterval(0.3f);

                seq.Append(mXIconImage.DOFade(1, 0.2f));
                seq.Append(mXIconImage.DOFade(0, 0.2f));
                seq.Append(mXIconImage.DOFade(1, 0.2f));
                seq.Append(mXIconImage.DOFade(0, 0.2f));

                yield return seq.WaitForCompletion();

                yield return new WaitForSeconds(1f);

                mAnimTargetRectTransform.anchoredPosition = Vector2.zero;
                mAnimTargetRectTransform.rotation = Quaternion.identity;
            }
        }
    }
}
