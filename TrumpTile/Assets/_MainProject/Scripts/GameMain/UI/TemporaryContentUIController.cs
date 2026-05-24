using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    [System.Serializable]
    public struct ScaleAnimConfig
    {
        public float scale;
        public float duration;
        public float interval;
        public bool isLoop;
    }
    [System.Serializable]
    public class TemporaryContentUIController : ContentUIController
    {
        [SerializeField] private ScaleAnimConfig mScaleAnimConfig;
        public override void PlayShowButtonAnim(Button button)
        {
            if(button == null)
            {
                return;
            }

            Sequence sq = DOTween.Sequence();
            sq.Append(button.transform.DOScale(Vector2.one * mScaleAnimConfig.scale, mScaleAnimConfig.duration));
            sq.Append(button.transform.DOScale(Vector2.one, mScaleAnimConfig.duration));

            sq.AppendInterval(mScaleAnimConfig.interval); // 대기 간격

            if(mScaleAnimConfig.isLoop)
            {
                sq.SetLoops(-1);
            }  
        }
    }    
}

