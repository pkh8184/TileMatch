using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    [System.Serializable]
    public struct RotateAnimConfig
    {
        public Vector3 angle;
        public float duration;
        public float interval;
        public bool isLoop;
    }
    [System.Serializable]
    public class PermanentContentUIController : ContentUIController
    {
        [SerializeField] private RotateAnimConfig mRotateAnimConfig;

        public override void PlayShowButtonAnim(Button button)
        {
            Sequence sq = DOTween.Sequence();
            sq.Append(button.transform.DORotate(mRotateAnimConfig.angle, mRotateAnimConfig.duration).SetEase(Ease.InOutSine));
            sq.Append(button.transform.DORotate(new Vector3(0, 0, 0), mRotateAnimConfig.duration).SetEase(Ease.InOutSine));

            sq.AppendInterval(mRotateAnimConfig.interval); // 대기 간격

            if(mRotateAnimConfig.isLoop)
            {
                sq.SetLoops(-1);
            }  
        }
    }    
}

