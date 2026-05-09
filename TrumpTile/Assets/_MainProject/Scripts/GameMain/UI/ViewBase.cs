using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ViewBase : UIBase
    {
        [Header("씬 전환 오브젝트")]
        [SerializeField] protected GameObject mSceneTransister;

        public override void Initialize()
        {
            base.Initialize();

           // EventManager.Inst.AddEvent("CompleteLoadAd", AdjustBannerAd);
        }

        //광고 관리 클래스에서 배너 광고 로드 완료 OnCall 이벤트에 연결
        protected virtual void AdjustBannerAd(object obj)
        {
            RectTransform contentRect = GetComponent<RectTransform>();
            Canvas canvas = GetComponentInParent<Canvas>();

            float bannerHeightPx = AdManager.Inst.GetBannerHeightForAdjustView() * Screen.dpi / 160f;
            float screenToCanvasRatio = canvas.GetComponent<RectTransform>().rect.height / Screen.height;
            float bannerHeightUI = bannerHeightPx * screenToCanvasRatio;

            contentRect.offsetMin = new Vector2(contentRect.offsetMin.x, bannerHeightUI);

            AdManager.Inst.ShowBannerAd();
        }
        protected IEnumerator Co_FadeInAnim()
        {
            mSceneTransister.gameObject.SetActive(true);
            mSceneTransister.GetComponent<Animator>().SetTrigger("FadeIn");

            yield return new WaitForSeconds(1f);

            mSceneTransister.gameObject.SetActive(false);
        }
        protected IEnumerator Co_FadeOutAnim()
        {
            mSceneTransister.gameObject.SetActive(true);
            mSceneTransister.GetComponent<Animator>().SetTrigger("FadeOut");

            yield return new WaitForSeconds(1f);
        }
    }
}
