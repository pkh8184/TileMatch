using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class TitleView : ViewBase
    {
        [Header("TitleView 이미지")]
        [SerializeField] private Image mStudioLogo;

        [SerializeField] private float mFadeDuration;

        private IEnumerator Start()
        {
            mStudioLogo.DOFade(1, mFadeDuration);
            yield return new WaitForSeconds(mFadeDuration);
            mStudioLogo.DOFade(0, mFadeDuration);
            yield return new WaitForSeconds(mFadeDuration);

            mStudioLogo.transform.parent.gameObject.SetActive(false);
        }
    }
}

