using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class IngameGameOverSlotPopup : IngameGameOverPopup
    {
        [Header("애니메이션 타겟 렉트")]
        [SerializeField] private RectTransform[] mAnimTargetRect;

        private CanvasGroup[] mAnimTargetImage;
        public override void Initialize()
        {
            base.Initialize();

            mAnimTargetImage = new CanvasGroup[mAnimTargetRect.Length];
            for(int i = 0; i < mAnimTargetRect.Length; i++)
            {
                mAnimTargetImage[i] = mAnimTargetRect[i].GetComponent<CanvasGroup>();
            }
        }
        public override void Show()
        {
            base.Show();

            StartCoroutine(Co_PlaySlotAnim());
        }
        private IEnumerator Co_PlaySlotAnim()
        {
            while(true)
            {
                Sequence seq = DOTween.Sequence();

                for(int i = 0; i < mAnimTargetRect.Length; i++)
                {
                    mAnimTargetRect[i].anchoredPosition = Vector2.zero;
                    mAnimTargetImage[i].alpha = 1;
                }

                for(int i = 0; i < mAnimTargetRect.Length; i++)
                {
                    seq.Insert(0.1f * i, mAnimTargetRect[i].DOAnchorPosY(100, 0.3f));
                    seq.Join(mAnimTargetImage[i].DOFade(0, 0.3f));
                }

                seq.AppendInterval(1f);

                yield return seq.WaitForCompletion();   
            }
        }
    }   
}
