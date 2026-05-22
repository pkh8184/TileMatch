using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class RemoveAdsPopup : PopupBase
    {
        [Header("컨텐츠 UI 전용 컴포넌트")]
        [SerializeField] private PermanentContentUIController mContentController;

        public override void Initialize()
        {
            base.Initialize();

            mContentController.PlayShowButtonAnim(mShowButton);
        }
    }    
}

