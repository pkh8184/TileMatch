using System;
using System.Collections.Generic;
using System.Collections;
using TrumpTile.GameMain.Core;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameView : ViewBase
    {
        [Header("스테이지 시작 시 표시될 레벨네임 오브젝트들")]
        [SerializeField] private Image mLevelNameBackground;
        [SerializeField] private CanvasGroup mLevelNameCanvasGroup;
        [SerializeField] private RectTransform mTopLevelNameRect;
        
        public override void Initialize()
        {
            base.Initialize();
            mSceneTransister.gameObject.SetActive(true);
            mLevelNameCanvasGroup.alpha = 0;
            //임시
            EventManager.Inst.AddEvent("IngameLoadingComplete", PlayFadeInAfterLoadLevel);
        }
        private void PlayFadeInAfterLoadLevel(object obj)
        {
            Action onComplete = obj as Action;
            
            mLevelNameBackground.gameObject.SetActive(true);

            StartCoroutine(Co_PlayFadeInAnimAfterLoadLevel(onComplete));
        }
        private IEnumerator Co_PlayFadeInAnimAfterLoadLevel(Action onComplete)
        {
            yield return StartCoroutine(Co_FadeInAnim());

            onComplete?.Invoke();

            mLevelNameCanvasGroup.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
            StartCoroutine(Co_PlayLevelNameAnim());
            
        }
        private IEnumerator Co_PlayLevelNameAnim()
        {
            Sequence sq = DOTween.Sequence();

            sq.Append(mLevelNameCanvasGroup.DOFade(1, 0.5f));
            RectTransform rt = mLevelNameCanvasGroup.transform.GetComponent<RectTransform>();
            sq.Append(rt.DOPunchScale(Vector3.one * 0.1f, 0.4f, 5, 0.5f));
            sq.AppendInterval(0.5f);

            float targetX = -(Screen.width / 2f + rt.rect.width);
            sq.Append(rt.DOAnchorPosX(targetX, 0.5f).SetEase(Ease.InQuad));
            sq.Append(mLevelNameBackground.DOFade(0, 0.3f));

            sq.AppendInterval(0.3f);
            sq.Append(mTopLevelNameRect.DOAnchorPosX(0, 0.15f).SetEase(Ease.OutQuad));
            sq.OnComplete(() => GameManager.Instance.LoadingAnimComplete = true);

            yield return sq.WaitForCompletion();          
        }
    }
    
}

