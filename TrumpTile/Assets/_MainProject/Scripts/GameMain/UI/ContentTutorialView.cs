using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using DG.Tweening;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ContentTutorialView : ViewBase
    {
        [Header("스케일 애니메이션 적용할 이미지 렉트")]
        [SerializeField] private RectTransform[] mImageRectArray;

        [Header("페이드 인 애니메이션 적용할 화살표 이미지")]
        [SerializeField] private Image[] mArrowImageArray;

        [Header("애니메이션 시간")]
        [SerializeField] private float mImageRectDuration; 
        [SerializeField] private float mArrowImageDuration;

        private Sequence seq;
        public override void Show()
        {
            AdManager.Inst.HideBannerAd();
            base.Show();
    
            if(seq != null && seq.active)
            {
                seq.Kill();
            }
            seq = DOTween.Sequence();

            foreach(var item in mImageRectArray)
            {
                item.localScale = Vector2.zero;
            }
            foreach(var item in mArrowImageArray)
            {
                item.color = new Color(1,1,1,0);
            }
            
            int index = 0;
            while(true)
            {
                if(index < mImageRectArray.Length)
                {
                    seq.Append(mImageRectArray[index].DOScale(1, mImageRectDuration));
                }
                if(index < mArrowImageArray.Length)
                {
                    seq.Append(mArrowImageArray[index].DOFade(1, mArrowImageDuration));
                }
                index++;
                if(index >= mImageRectArray.Length && index >= mArrowImageArray.Length)
                {
                    break;
                }       
            }
        }
        public override void Hide()
        {
            base.Hide();
            AdManager.Inst.ShowBannerAd();
        }
    }    
}

