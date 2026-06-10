using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using DG.Tweening;
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

        public override void Show()
        {
            base.Show();

            foreach(var item in mImageRectArray)
            {
                item.localScale = Vector2.zero;
            }
            foreach(var item in mArrowImageArray)
            {
                item.color = new Color(1,1,1,0);
            }

            Sequence sq = DOTween.Sequence();
            
            int index = 0;
            while(true)
            {
                if(index < mImageRectArray.Length)
                {
                    sq.Append(mImageRectArray[index].DOScale(1, mImageRectDuration));
                }
                if(index < mArrowImageArray.Length)
                {
                    sq.Append(mArrowImageArray[index].DOFade(1, mArrowImageDuration));
                }
                index++;
                if(index >= mImageRectArray.Length && index >= mArrowImageArray.Length)
                {
                    break;
                }       
            }
        }
    }    
}

