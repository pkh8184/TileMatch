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

            //1일 이상: "n일 m시간"(m>0) / "n일"(m==0), 1일 미만: "m시간"(1시간 미만이면 "0시간")
            string result;
            if(day > 0)
            {
                result = hour > 0 ? $"{day}일 {hour}시간" : $"{day}일";
            }
            else
            {
                result = $"{hour}시간";
            }

            if(mLimitTimeText != null) mLimitTimeText.text = result;
            if(mShowButtonLimitTimeText != null) mShowButtonLimitTimeText.text = result;
        }
    }    
}

