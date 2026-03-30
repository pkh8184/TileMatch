using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 해당 팝업에 표시되는 UI는 모두 로컬 데이터를 읽어온 후 갱신됩니다.
    /// </summary>
    public class ProfileImagePopup : PopupBase
    {
        [Header("ProfileImage Popup 텍스트")]
        [SerializeField] private TMP_Text mNickName;

        [Header("ProfileImage Popup 이미지")]
        [SerializeField] private Image mProfileImage;
        [SerializeField] private Image mProfileFrame;

        [Header("ProfileImage Popup 버튼")]
        [SerializeField] private Button mIconListButton;
        [SerializeField] private Button mFrameListButton;
        [SerializeField] private Button[] mIconButtonArray;
        [SerializeField] private Button[] mFrameButtonArray;
        [SerializeField] private Button mConfirmButton;

        [Header("아이콘, 프레임 항목들 표시하는 게임오브젝트")]
        [SerializeField] private GameObject mIconListGameObject;
        [SerializeField] private GameObject mFrameListGameObject;

        private int mImageIndex;
        private int mFrameIndex;
        public override void Initialize()
        {
            base.Initialize();

            mIconListButton.onClick.AddListener(() => ShowList(true));
            mFrameListButton.onClick.AddListener(() => ShowList(false));

            for (int i = 0; i < mIconButtonArray.Length; i++)
            {
                int index = i;
                mIconButtonArray[index].onClick.AddListener(() => SetIconIndex(index));
            }
            for (int i = 0; i < mFrameButtonArray.Length; i++)
            {
                int index = i;
                mFrameButtonArray[index].onClick.AddListener(() => SetFrameIndex(index));
            }

            mConfirmButton.onClick.AddListener(ConfirmImage);
        }

        public override void Show()
        {
            RefreshLocalData();
            base.Show();
        }

        protected override void RefreshLocalData()
        {
            if (!CanRefresh())
            {
                return;
            }

            mImageIndex = PlayerDataManager.Inst.UserData.ProfileImageIndex;
            mFrameIndex = PlayerDataManager.Inst.UserData.ProfileFrameIndex;

            mNickName.text = PlayerDataManager.Inst.UserData.NickName;
            mProfileImage.sprite = PlayerDataManager.Inst.GetProfileImage();
            mProfileFrame.sprite = PlayerDataManager.Inst.GetProfileFrame();
        }
        private void ShowList(bool isIcon)
        {
            if(isIcon)
            {
                mFrameListGameObject.SetActive(false);
                mIconListGameObject.SetActive(true);
            }
            else
            {
                mIconListGameObject.SetActive(false);
                mFrameListGameObject.SetActive(true);
            }
        }

        private void ConfirmImage()
        {
            PlayerDataManager.Inst.SetProfileImageIndex(mImageIndex);
            PlayerDataManager.Inst.SetProfileFrameIndex(mFrameIndex);

            EventManager.Inst.ActiveEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA);
        }
        private void SetIconIndex(int index)
        {
            mImageIndex = index;
        }
        private void SetFrameIndex(int index)
        {
            mFrameIndex = index;
        }
    }
}

