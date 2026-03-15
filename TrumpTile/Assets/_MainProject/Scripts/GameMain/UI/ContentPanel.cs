using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 배너 광고가 표시되는 씬에서 컨텐츠들을 배너 광고 높이만큼 위로 올려주기 위한 클래스입니다.
    /// </summary>
    public class ContentPanel : MonoBehaviour
    {
        private RectTransform mRectTransform;

        private void Awake()
        {
            mRectTransform = GetComponent<RectTransform>();

            //RectTransform을 배너 광고 높이만큼 올려줍니다. 
            //float bannerHeight = 광고 배너 높이
            //mRectTransform.offsetMin = new Vector2(0, bannerHeight);
        }
    }
}
