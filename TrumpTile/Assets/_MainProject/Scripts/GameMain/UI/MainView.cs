using DG.Tweening;
using System.Collections;
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

        //플레이어의 로컬 데이터 -> 따로 로컬데이터매니저에서 관리하는 게 좋을듯 (03/18)
        private Image mProfileFrame;
        private Image mProfileImage;

        public override void Initialize()
        {
            base.Initialize();

            mStageStartButton.onClick.AddListener(OnStageButtonClick);
        }

        protected override void Refresh()
        {
            mGoldText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.Gold);
            mCurrentStageText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.CurrentStageForStageStart);
        }
        protected override void RefreshLocalData()
        {
            mProfileFrame.sprite = PlayerDataManager.Inst?.GetProfileFrame();
            mProfileImage.sprite = PlayerDataManager.Inst?.GetProfileImage();
        }
        private void OnStageButtonClick()
        {
            SceneTransister.Inst.TransistScene("GameScene");
        }
    }
}

