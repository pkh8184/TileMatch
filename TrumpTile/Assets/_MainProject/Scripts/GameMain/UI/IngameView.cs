using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameView : ViewBase
    {
        [Header("스테이지 시작 시 표시될 레벨네임 오브젝트들")]
        [SerializeField] private Image mLevelNameBackground;
        [SerializeField] private CanvasGroup mLevelNameCanvasGroup;
        [SerializeField] private RectTransform mTopLevelNameRect;

        [Header("레벨 배경 관련")]
        [SerializeField] private Image mBackgroundImage;
        [SerializeField] private Sprite mDefaultBackgroundSprite;

        [Header("타이머 관련")]
        [SerializeField] private TMP_Text mTimerText;
        [SerializeField] private Slider mTimerSlider;
        [SerializeField] private RectTransform mTimerIconRect;

        public override void Initialize()
        {
            base.Initialize();
            mLevelNameCanvasGroup.alpha = 0;

            //임시
            EventManager.Inst.AddEvent("IngameLoadingComplete", OnLoadLevelComplete);
            EventManager.Inst.AddEvent("TimerSettingComplete", OnTimerSettingComplete);

            mTopLevelNameRect.localScale = Vector3.zero;
        }
        private void OnLoadLevelComplete(object obj)
        {
            Sprite background = (Sprite)obj ? (Sprite)obj : mDefaultBackgroundSprite;
            mBackgroundImage.sprite = background;    

            mLevelNameBackground.gameObject.SetActive(true);
            
            mLevelNameBackground.color = new Color(0, 0, 0, 245f / 255f);

            mTopLevelNameRect.localScale = Vector3.zero;  

            mLevelNameCanvasGroup.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
            StartCoroutine(Co_PlayLevelNameAnim());
        }
        private void OnTimerSettingComplete(object obj)
        {
            StartCoroutine(Co_TimerTextProgress());
            StartCoroutine(Co_TimerSliderProgress());
        }
        private IEnumerator Co_PlayLevelNameAnim()
        {
            yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());

            Sequence sq = DOTween.Sequence();

            AudioEvent.Play(EAudioKey.SFX_LevelName_01);
            sq.Append(mLevelNameCanvasGroup.DOFade(1, 0.5f));
            RectTransform rt = mLevelNameCanvasGroup.transform.GetComponent<RectTransform>();

            rt.anchoredPosition = Vector2.zero;
            sq.Append(rt.DOPunchScale(Vector3.one * 0.1f, 0.4f, 5, 0.5f));
            sq.AppendInterval(0.5f);

            float targetX = -(Screen.width / 2f + rt.rect.width);
            sq.AppendCallback(() => AudioEvent.Play(EAudioKey.SFX_LevelName_02));
            sq.Append(rt.DOAnchorPosX(targetX, 0.5f).SetEase(Ease.InQuad));
            sq.Append(mLevelNameBackground.DOFade(0, 0.3f));

            sq.AppendInterval(0.3f);
            sq.Append(mTopLevelNameRect.DOScale(1, 0.3f).SetEase(Ease.OutBounce));
            sq.OnComplete(() => GameManager.Instance.LoadingAnimComplete = true);

            yield return sq.WaitForCompletion();

            mLevelNameBackground.gameObject.SetActive(false);
        }
        private IEnumerator Co_TimerTextProgress()
        {
            while(true)
            {
                mTimerText.text = GameManager.Instance.GetCurrentTimeString();
                yield return null;
            }
        }
        private IEnumerator Co_TimerSliderProgress()
        {
            Image sliderImage = mTimerSlider.fillRect.GetComponent<Image>();
            bool timerShakeStart = false;
            while (true)
            {
                float t = GameManager.Instance.GetCurrentTimeClamped();
                if(t <= 0.2f)
                {
                    if(timerShakeStart == false)
                    {
                        timerShakeStart = true;
                        mTimerIconRect.DOShakeAnchorPos(0.1f, 2.5f, 20).SetLoops(-1, LoopType.Restart);
                    }
                }
                else
                {
                    if(timerShakeStart == true)
                    {
                        timerShakeStart = false;
                        mTimerIconRect.DOKill();
                        mTimerIconRect.anchoredPosition = Vector2.zero;
                    }
                }

                mTimerSlider.value = t;

                Color green = new Color(0, 1, 0);
                Color yellow = new Color(1, 1, 0);
                Color red = new Color(1, 0, 0);

                Color result = t > 0.5f
                    ? Color.Lerp(yellow, green, (t - 0.5f) * 2f)
                    : Color.Lerp(red, yellow, t * 2f);

                sliderImage.color = result;

                yield return null;
            }
        }
    }
    
}

