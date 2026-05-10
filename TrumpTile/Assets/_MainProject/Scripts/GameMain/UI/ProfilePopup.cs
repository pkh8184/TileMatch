using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
    public class ProfilePopup : PopupBase
    {
        [Header("닉네임")]
        [SerializeField] private TMP_Text mNickName;

        [Header("프로필 이미지 및 프레임 프리뷰")]
        [SerializeField] private Image mProfileImage;
        [SerializeField] private Image mProfileFrame;

        //[Header("ProfileImagePopup 참조")]
        //[SerializeField] private ProfileImagePopup mProfileImagePopup;

        public override void Initialize()
        {
            base.Initialize();
        }
        protected override void Refresh()
        {
           
            
        }
    }
}

