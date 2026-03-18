using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ViewBase : UIBase
    {
        //광고 관리 클래스에서 배너 광고 로드 완료 OnCall 이벤트에 연결
        public void AdjustForBanner(float bannerHeightPixel)
        {
            RectTransform contentRect = GetComponent<RectTransform>();

            Canvas canvas = GetComponentInParent<Canvas>();

            float bannerHeightUI = bannerHeightPixel / canvas.scaleFactor;

            contentRect.offsetMin = new Vector2(contentRect.offsetMin.x, bannerHeightUI);
        }
    }
}
