using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ContentUnlockPopup : PopupBase
    {
        [Header("계속 버튼")]
        [SerializeField] private Button mContinueButton;
        public override void Initialize()
        {
            base.Initialize();

            mContinueButton.onClick.AddListener(Hide);
        }
    }   
}
