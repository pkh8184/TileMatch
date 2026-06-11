using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ViewBase : UIBase
    {
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
    }
}
