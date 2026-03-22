using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class TitleView : ViewBase
    {
        [Header("스튜디오 로고 이미지")]
        [SerializeField] private Image mStudioLogo;
        [Header("로고 이미지 페이드 인 & 아웃 시간")]
        [SerializeField] private float mLogoFadeDuration;

        [Header("가이드 메세지")]
        [SerializeField] private TMP_Text mGuideText;
        [Header("가이드 메세지 내용 목록")]
        [SerializeField] private string[] mGuideArray;

        [Header("로딩 바")]
        [SerializeField] private Slider mLoadingSlider;

        [Header("버전 체크 팝업")]
        [SerializeField] private PublicPopup mVersionCheckPopup;

        [Header("씬 이동 시 Fade Out 이미지")]
        [SerializeField] private Image mFadeImage;
        [Header("씬 전환 시 Fade 애니메이션 시간")]
        [SerializeField] private float mFadeDuration;

        private TitleManager titleManager;
        public override void Initialize()
        {
            base.Initialize();

            StartCoroutine(Co_PlayStudioLogoFadeAnim());
            mGuideText.text = mGuideArray[UnityEngine.Random.Range(0, mGuideArray.Length)];

            titleManager = FindObjectOfType<TitleManager>();
            titleManager.OnRequiredVersionUpdate += mVersionCheckPopup.ShowWithOutButton;
            titleManager.OnLoadingComplete += PlayFadeOutAnimOnSceneChange;
            mVersionCheckPopup.AddActionToConfirmButton(GoToPlayStoreForUpdate);
            
        }
        private void PlayFadeOutAnimOnSceneChange(object obj)
        {
            Action onComplete = obj as Action;
            StartCoroutine(Co_PlayFadeOutAnimOnSceneChange(onComplete));
        }
        private IEnumerator Co_PlayFadeOutAnimOnSceneChange(Action onComplete)
        {
            mFadeImage.gameObject.SetActive(true);

            mFadeImage.DOFade(1, mFadeDuration);
            yield return new WaitForSeconds(mFadeDuration);

            onComplete?.Invoke();
        }
        private IEnumerator Co_PlayStudioLogoFadeAnim()
        {
            mStudioLogo.transform.parent.gameObject.SetActive(true);

            mStudioLogo.DOFade(1, mLogoFadeDuration);
            yield return new WaitForSeconds(mLogoFadeDuration);
            mStudioLogo.DOFade(0, mLogoFadeDuration);
            yield return new WaitForSeconds(mLogoFadeDuration);

            mStudioLogo.transform.parent.gameObject.SetActive(false);

            StartCoroutine(Co_PlayLoadingSliderAnim());
        }
        private IEnumerator Co_PlayLoadingSliderAnim()
        {
            while(true)
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

