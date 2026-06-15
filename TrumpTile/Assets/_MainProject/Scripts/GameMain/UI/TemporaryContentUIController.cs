using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

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
        [SerializeField] private TMP_Text mLimitTimeText;
        [SerializeField] private TMP_Text mShowButtonLimitTimeText;
        public override void PlayShowButtonAnim(Button button)
        {
            if(button == null)
            {
                return;
            }

            Sequence sq = DOTween.Sequence();
            Vector2 scale = button.transform.localScale;
            sq.Append(button.transform.DOScale(scale * mScaleAnimConfig.scale, mScaleAnimConfig.duration));
            sq.Append(button.transform.DOScale(scale, mScaleAnimConfig.duration));

            sq.AppendInterval(mScaleAnimConfig.interval); // 대기 간격

            if(mScaleAnimConfig.isLoop)
            {
                sq.SetLoops(-1);
            }  
        }
        public void SetLimitTimeText(float time)
        {
            int totalTime = (int)time;

            int day = totalTime / 86400;
            int hour = (totalTime % 86400) / 3600;

            string dayString = day > 0? $"{day}일 " : "";
            string hourString = hour >  0? $"{hour}시간" : "";

            string result = dayString + hourString;
            
            if(mLimitTimeText != null) mLimitTimeText.text = result;
            if(mShowButtonLimitTimeText != null) mShowButtonLimitTimeText.text = result;
        }
    }    
}

