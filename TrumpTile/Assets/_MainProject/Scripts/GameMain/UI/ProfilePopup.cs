using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class ProfilePopup : PopupBase
    {
        [Header("닉네임")]
        [SerializeField] private TMP_InputField mNickNameInputField;

        [Header("프로필 이미지 및 프레임 프리뷰")]
        [SerializeField] private Image mAvataPreviewImage;
        [SerializeField] private Image mFramePreviewImage;

        [Header("리스트 활성화 버튼")]
        [SerializeField] private Button mAvataButton;
        [SerializeField] private Button mFrameButton;
        [SerializeField] private Color mUnActiveColor;
        [Header("이미지 리스트 오브젝트")]
        [SerializeField] private GameObject mAvataListObject;
        [SerializeField] private GameObject mFrameListObject;

        [Header("버튼 리스트")]
        [SerializeField] private List<Button> mAvataButtonList;
        [SerializeField] private List<Button> mFrameButtonList;

        [Header("선택 표시")]
        [SerializeField] private RectTransform mMakerRect;

        [Header("적용 버튼")]
        [SerializeField] private Button mApplyButton;

        private int mCurrentAvataIndex = 0;
        private int mCurrentFrameIndex = 0;

        private List<Sprite> mAvataSpriteList;
        private List<Sprite> mFrameSpriteList;

        public override void Initialize()
        {
            base.Initialize();

            for(int i = 0; i < mAvataButtonList.Count; i++)
            {
                int index = i;
                mAvataButtonList[i].onClick.AddListener(() => SetAvataPreviewImage(index));
            }
            for (int i = 0; i < mFrameButtonList.Count; i++)
            {
                int index = i;
                mFrameButtonList[i].onClick.AddListener(() => SetFramePreviewImage(index));
            }

            mAvataButton.onClick.AddListener(() => OnSpriteListChange(true));
            mFrameButton.onClick.AddListener(() => OnSpriteListChange(false));
            mApplyButton.onClick.AddListener(() => ApplyChanges());
        }
        public void SetProfilePopupValid(List<Sprite> avata = null, List<Sprite> frame = null)
        {
            if(mAvataSpriteList == null)
            {
                  mAvataSpriteList = avata;
            }
            if(mFrameSpriteList == null)
            {
                  mFrameSpriteList = frame;
            }

            mCurrentAvataIndex = PlayerDataManager.Inst.GetProfileImageIndex();
            mCurrentFrameIndex = PlayerDataManager.Inst.GetProfileFrameIndex();

            mAvataPreviewImage.sprite = mAvataSpriteList[mCurrentAvataIndex];
            mFramePreviewImage.sprite = mFrameSpriteList[mCurrentFrameIndex];

            mNickNameInputField.text = PlayerDataManager.Inst.GetNickname();

            OnSpriteListChange(true);
        }
        private void OnSpriteListChange(bool isAvata)
        {
            mAvataListObject.SetActive(isAvata);
            mFrameListObject.SetActive(!isAvata);

            mAvataButton.image.color = mAvataListObject.activeSelf ? Color.white : mUnActiveColor;
            mFrameButton.image.color = mFrameListObject.activeSelf ? Color.white : mUnActiveColor;

            AdjustSelectedMark(isAvata);
        }
        private void SetAvataPreviewImage(int index)
        {
            mCurrentAvataIndex = index;
            mAvataPreviewImage.sprite = mAvataSpriteList[mCurrentAvataIndex];

            AdjustSelectedMark(true);
        }
        private void SetFramePreviewImage(int index)
        {
            mCurrentFrameIndex = index;
            mFramePreviewImage.sprite = mFrameSpriteList[mCurrentFrameIndex];

            AdjustSelectedMark(false);
        }
        private void ApplyChanges()         
        {
            PlayerDataManager.Inst?.SetProfileImageIndex(mCurrentAvataIndex);
            PlayerDataManager.Inst?.SetProfileFrameIndex(mCurrentFrameIndex);
            PlayerDataManager.Inst.SetNickName(mNickNameInputField.text);

            Debug.Log(PlayerDataManager.Inst.GetProfileImageIndex());

            EventManager.Inst.ActiveEvent(RequestEventKeys.REFRESH_PLAYER_LOCAL_DATA);
 
        }
        private void AdjustSelectedMark(bool isAvata)
        {
            if(isAvata)
            {
                mMakerRect.transform.SetParent(mAvataButtonList[mCurrentAvataIndex].transform);
            }
            else
            {
                mMakerRect.transform.SetParent(mFrameButtonList[mCurrentFrameIndex].transform);
            }
            mMakerRect.anchoredPosition = Vector2.zero;
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;

            mCurrentAvataIndex = PlayerDataManager.Inst.GetProfileImageIndex();
            mCurrentFrameIndex = PlayerDataManager.Inst.GetProfileFrameIndex();

            mAvataPreviewImage.sprite = mAvataSpriteList[mCurrentAvataIndex];
            mFramePreviewImage.sprite = mFrameSpriteList[mCurrentFrameIndex];
            
            AdjustSelectedMark(mAvataListObject.activeSelf);
            mMakerRect.localScale = Vector2.one;

            Sequence sq = DOTween.Sequence();

            ScrollRect scroll = mAvataListObject.GetComponent<ScrollRect>();
            scroll.enabled = false;

            PopupScaleAnimator.AppendPopIn(sq, mPopupObj.transform, mShowDuration);
            sq.OnComplete(() => 
            {
                scroll.enabled = true;
                SetInteractable(true);
            });
        }
    }
}

