using DG.Tweening;
using System;
using System.Collections;
using System.Text;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class TitleView : ViewBase
    {
        [Header("스튜디오 로고 이미지")]
        [SerializeField] private Image mStudioLogoUp;
        [SerializeField] private Image mStudioLogoDown;
        [Header("로고 이미지 페이드 인 & 아웃 시간")]
        [SerializeField] private float mLogoFadeDuration;

        [Header("로딩 바")]
        [SerializeField] private Slider mLoadingSlider;

        [Header("버전 체크 팝업")]
        [SerializeField] private PublicPopup mVersionCheckPopup;

        private TitleManager titleManager;
        public override void Initialize()
        {
            base.Initialize();

            StartCoroutine(Co_PlayStudioLogoFadeAnim());

            titleManager = FindObjectOfType<TitleManager>();
            EventManager.Inst.AddEvent(RequestEventKeys.LOADING_COMPLETE, PlayFadeOutAnimOnSceneChange);
            EventManager.Inst.AddEvent(RequestEventKeys.REQUIRED_VERSION_UPDATE, (obj) => mVersionCheckPopup.ShowWithOutButton());
            mVersionCheckPopup.AddActionToConfirmButton(GoToPlayStoreForUpdate);
            
        }
        private void PlayFadeOutAnimOnSceneChange(object obj)
        {
            Action onComplete = obj as Action;
            StartCoroutine(Co_PlayFadeOutAnimOnSceneChange(onComplete));
        }
        private IEnumerator Co_PlayFadeOutAnimOnSceneChange(Action onComplete)
        {
            yield return StartCoroutine(Co_FadeOutAnim());

            onComplete?.Invoke();
        }
        private IEnumerator Co_PlayStudioLogoFadeAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mStudioLogoUp.DOFade(1, mLogoFadeDuration));
            seq.Join(mStudioLogoDown.DOFade(1, mLogoFadeDuration));
            seq.OnComplete(() => mStudioLogoUp.GetComponent<Animator>().SetTrigger("LogoAnim"));
            yield return seq.WaitForCompletion();

            yield return new WaitForSeconds(2);

            Sequence seq2 = DOTween.Sequence();
            seq2.Append(mStudioLogoUp.DOFade(0, mLogoFadeDuration));
            seq2.Join(mStudioLogoDown.DOFade(0, mLogoFadeDuration));
            seq2.OnComplete(() => mStudioLogoUp.transform.parent.gameObject.SetActive(false));
            yield return seq2.WaitForCompletion();

            StartCoroutine(Co_PlayLoadingSliderAnim());
        }
        private IEnumerator Co_PlayLoadingSliderAnim()
        {
            while(mLoadingSlider.value < 1)
            {
                mLoadingSlider.value = titleManager.LoadingProgress / 100;
                yield return null;
            }
        }
        private void GoToPlayStoreForUpdate()
        {
            Debug.Log("업데이트 화면으로 이동");
            string packageName = Application.identifier;

            string marketUrl = $"market://details?id={packageName}";

            string webUrl = $"https://play.google.com/store/apps/details?id={packageName}";

            try
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    Application.OpenURL(marketUrl);
                }
                else
                {
                    Application.OpenURL(webUrl);
                }
            }
            catch
            {
                Application.OpenURL(webUrl);
            }

            Application.Quit();
        }
    }
}

