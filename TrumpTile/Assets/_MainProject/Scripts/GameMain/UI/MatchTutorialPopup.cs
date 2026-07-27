using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class MatchTutorialPopup : PopupBase
    {
        [Header("OK 버튼")]
        [SerializeField] private Button mOKButton;
        [Header("애니메이션 타겟 렉트 트랜스폼")]
        [SerializeField] private RectTransform[] mAnimTargetRectTransformArray;
        [Header("이동 위치 렉트 트랜스폼")]
        [SerializeField] private RectTransform[] mMoveTargetRectTransformArray;
        [Header("인디케이터 렉트 트랜스폼")]
        [SerializeField] private RectTransform mIndicatorRectTransform;
        [Header("매치 파티클")]
        [SerializeField] private ParticleSystem mMatchParticle;

        public override void Initialize()
        {
            base.Initialize();

            mOKButton.onClick.AddListener(() => Hide());
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;

            Sequence seq = DOTween.Sequence();
            PopupScaleAnimator.AppendPopIn(seq, mPopupObj.transform, mShowDuration);
            seq.OnComplete(() => StartCoroutine(Co_PlayMatchTutorialAnim()));
        }
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                MarkClosed();
                GameManager.Instance.tutorialComplete = true;
                gameObject.SetActive(false);
            });
        }
        private IEnumerator Co_PlayMatchTutorialAnim()
        {
            while(true)
            {
                Sequence seq = DOTween.Sequence();

                seq.AppendInterval(0.5f);
                for (int i = 0; i < mAnimTargetRectTransformArray.Length; i++)
                {
                    seq.Append(mIndicatorRectTransform.DOLocalMove(mAnimTargetRectTransformArray[i].localPosition + new Vector3(60,0,0), 0.5f));
                    seq.Append(mIndicatorRectTransform.DOScale(0.85f, 0.1f));
                    seq.Append(mIndicatorRectTransform.DOScale(1, 0.1f));

                    seq.Append(mAnimTargetRectTransformArray[i].DOPunchRotation(new Vector3(0, 0, 10f), 0.5f, 10, 0.5f));
                    
                    seq.AppendInterval(0.3f);
                    seq.Append(mAnimTargetRectTransformArray[i].DOMove(mMoveTargetRectTransformArray[i].position, 0.5f));
                    seq.Join(mAnimTargetRectTransformArray[i].DOScale(Vector2.one * 0.8f, 0.5f));
                    seq.AppendInterval(0.3f);
                }

                seq.Append(mAnimTargetRectTransformArray[0].DOMove(mMoveTargetRectTransformArray[1].position, 0.5f));
                seq.Join(mAnimTargetRectTransformArray[2].DOMove(mMoveTargetRectTransformArray[1].position, 0.5f));

                seq.Append(mAnimTargetRectTransformArray[0].DOScale(0, 0.2f));
                seq.Join(mAnimTargetRectTransformArray[1].DOScale(0, 0.2f));
                seq.Join(mAnimTargetRectTransformArray[2].DOScale(0, 0.2f));
                seq.OnComplete(() => mMatchParticle.Play());

                yield return seq.WaitForCompletion();

                yield return new WaitForSeconds(1f);

                foreach(var item in mAnimTargetRectTransformArray)
                {
                    item.anchoredPosition = Vector2.zero;
                    item.rotation = Quaternion.identity;
                    item.localScale = Vector2.one;

                    yield return null;
                }
            }
        }
    }
}
