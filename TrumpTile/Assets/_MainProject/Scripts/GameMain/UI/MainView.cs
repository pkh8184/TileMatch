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

        [Header("일일 퍼즐")]
        [SerializeField] private Button mDailyPuzzleButton;
        [SerializeField] private TMP_Text mDailyPuzzleButtonText;

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

        public override void Initialize()
        {
            base.Initialize();

            PlayerDataManager.Inst?.Initialize();
            RefreshLocalData();          
            
            mStageStartButton.onClick.AddListener(OnStageButtonClick);
            mDailyPuzzleButton.onClick.AddListener(OnDailyPuzzleButtonClick);
            mDailyPuzzleButton.interactable = false;
            InitializeDailyPuzzleAsync();
            mProfilePopup.SetProfilePopupValid(mAvataSpriteList, mFrameSpriteList);
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

        private async void InitializeDailyPuzzleAsync()
        {
            await DailyPuzzleManager.Inst.InitializeAsync();
            RefreshDailyPuzzleButton();
        }

        private void RefreshDailyPuzzleButton()
        {
            bool bCleared = DailyPuzzleManager.Inst.IsTodayCleared;
            mDailyPuzzleButton.interactable = !bCleared;

            if (mDailyPuzzleButtonText != null)
            {
                mDailyPuzzleButtonText.text = bCleared ? "완료" : "일일 퍼즐";
            }
        }

        private void OnDailyPuzzleButtonClick()
        {
            if (!DailyPuzzleManager.Inst.IsInitialized)
            {
                Debug.LogWarning("[MainView] DailyPuzzleManager not initialized yet");
                return;
            }

            DailyPuzzleManager.Inst.EnterDailyPuzzle();
        }
    }
}

