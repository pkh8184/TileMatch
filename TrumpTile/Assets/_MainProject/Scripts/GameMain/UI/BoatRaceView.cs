using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class BoatRaceView : ViewBase
    {
        [SerializeField] private TemporaryContentUIController mContentController;

        public override void Initialize()
        {
            base.Initialize();

            mContentController.PlayShowButtonAnim(mShowButton);
        }
    }    
}
