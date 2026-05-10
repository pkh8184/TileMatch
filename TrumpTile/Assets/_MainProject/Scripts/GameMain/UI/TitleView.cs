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
        [SerializeField] private Image mStudioLogo;

        [Header("로고 이미지 페이드 인 & 아웃 시간")]
        [SerializeField] private float mLogoFadeDuration;

        [Header("로딩 바")]
        [SerializeField] private Slider mLoadingSlider;

        [Header("버전 체크 팝업")]
        [SerializeField] private PublicPopup mVersionCheckPopup;
        [Header("버전 텍스트")]
        [SerializeField] private TMP_Text mVersionText;

        private TitleManager titleManager;
        public override void Initialize()
        {
            base.Initialize();

            mVersionText.text = Application.version;

            StartCoroutine(Co_PlayStudioLogoFadeAnim());

            titleManager = FindObjectOfType<TitleManager>();
            EventManager.Inst.AddEvent(RequestEventKeys.REQUIRED_VERSION_UPDATE, (obj) => mVersionCheckPopup.ShowWithOutButton());
            mVersionCheckPopup.AddActionToConfirmButton(GoToPlayStoreForUpdate);
            
        }
        private IEnumerator Co_PlayStudioLogoFadeAnim()
        {
            Sequence seq = DOTween.Sequence();

            mStudioLogo.fillAmount = 0;
            seq.Append(mStudioLogo.DOFillAmount(1, 0.5f));
            seq.AppendInterval(0.5f);
            seq.OnComplete(() => mStudioLogo.transform.parent.gameObject.SetActive(false));

            yield return seq.WaitForCompletion();

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

