using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class TemporaryContentUnlockPopup : ContentUnlockPopup
    {
        [Header("활성 시간 텍스트")]
        [SerializeField] private TMP_Text mActiveTimeText;

        [Header("대상 컨텐츠 이름")]
        [SerializeField] private string mContentName;

        [SerializeField]
        public override void Initialize()
        {
            base.Initialize();

            int time = (int)ContentManager.Inst.GetContentData<ContentBase>(mContentName).GetContentInfo().ActiveTime;
            int day = time / 86400;
            mActiveTimeText.text = day + "일";
        }
    }   
}
