using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class MainView : ViewBase
    {
        [Header("MainView 버튼")]
        [SerializeField] private Button mStageStartButton;

        [Header("MainView 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [SerializeField] private TMP_Text mCurrentStageText;

        [Header("하위 팝업 참조")]
        [SerializeField] private ProfilePopup mProfilePopup;

        [Header("프로필 이미지")]
        [SerializeField] private Image mProfileFrame;
        [SerializeField] private Image mProfileImage;

        [Header("프로필 아바타 및 프레임 스프라이트 리스트")]
        [SerializeField] private List<Sprite> mAvataSpriteList;
        [SerializeField] private List<Sprite> mFrameSpriteList;

        [Header("메인씬 로딩 시 연출 효과를 줄 렉트들")]
        [SerializeField] private RectTransform mLeftElementsRect;
        [SerializeField] private RectTransform mRightElementsRect;
        [SerializeField] private RectTransform[] mSizeAdjustElementRectArray;
        private CanvasGroup mLeftElementsRectCanvasGroup;
        private CanvasGroup mRightElementsRectCanvasGroup;
        private List<CanvasGroup> mSizeAdjustElementRectCanvasGroupList = new List<CanvasGroup>();
        public override void Initialize()
        {
            base.Initialize();

            PlayerDataManager.Inst?.Initialize();
            RefreshLocalData();          
            
            mStageStartButton.onClick.AddListener(OnStageButtonClick);
            mProfilePopup.SetProfilePopupValid(mAvataSpriteList, mFrameSpriteList);
            EventManager.Inst.AddEvent("MainSceneLoadComplete", _ => OnMainSceneLoadComplete());

            mLeftElementsRect.anchoredPosition = Vector2.left * 250;
            mRightElementsRect.anchoredPosition = Vector2.right * 250;
            foreach(var item in mSizeAdjustElementRectArray)
            {
                item.localScale = Vector2.zero;
                CanvasGroup group = item.GetComponent<CanvasGroup>();
                mSizeAdjustElementRectCanvasGroupList.Add(group);
                group.alpha = 0;
            }

            mLeftElementsRectCanvasGroup = mLeftElementsRect.GetComponent<CanvasGroup>();
            mLeftElementsRectCanvasGroup.alpha = 0;

            mRightElementsRectCanvasGroup = mRightElementsRect.GetComponent<CanvasGroup>();
            mRightElementsRectCanvasGroup.alpha = 0;
        }

        protected override void Refresh()
        {
            mGoldText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.Gold);
            mCurrentStageText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.CurrentStageForStageStart);
        }
        protected override void RefreshLocalData()
        {
            int index = PlayerDataManager.Inst.GetProfileImageIndex();
            mProfileImage.sprite = mAvataSpriteList[index];

            index = PlayerDataManager.Inst.GetProfileFrameIndex();
            mProfileFrame.sprite = mFrameSpriteList[index];

            Debug.Log($"[{name}] Refresh Local Data");
        }
        private void OnStageButtonClick()
        {
            SceneTransister.Inst.TransistScene("GameScene");
        }
        private void OnMainSceneLoadComplete()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mLeftElementsRect.DOAnchorPosX(0, 0.25f));
            seq.Join(mRightElementsRect.DOAnchorPosX(0, 0.25f));
            seq.Join(mLeftElementsRectCanvasGroup.DOFade(1, 0.3f));
            seq.Join(mRightElementsRectCanvasGroup.DOFade(1, 0.3f));

            int i = 0;
            foreach(var item in mSizeAdjustElementRectArray)
            {
                seq.Insert(0.25f / 1.5f + (0.2f / 1.5f * i), item.DOScale(Vector2.one, 0.5f / 1.5f));
                seq.Join(mSizeAdjustElementRectCanvasGroupList[i].DOFade(1, 0.7f / 1.5f));
                i++;
            }
        }
    }
}

