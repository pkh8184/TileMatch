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
        [Header("Profile Popup 텍스트")]
        [SerializeField] private TMP_Text mNickName;
        [SerializeField] private TMP_Text mCurrentStage;
        [SerializeField] private TMP_Text mFirstLoginDate;
        [SerializeField] private TMP_Text mFirstTryClearCount;
        [SerializeField] private TMP_Text mClearStageCount;
        [SerializeField] private TMP_Text mCompletedHousingChapterCount;
        [SerializeField] private TMP_Text mStreakFirstTryClearCount;
        [SerializeField] private TMP_Text mStreakLoginCount;

        [Header("Profile Popup 이미지")]
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
            mCurrentStage.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.CurrentStage);
            mFirstLoginDate.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.FirstLoginDate);
            mFirstTryClearCount.text = PlayerDataManager.Inst.GetDataToString (EPlayerDataType.FirstTryClearCount);
            mClearStageCount.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.ClearedStage);
            mCompletedHousingChapterCount.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.CompletedChapterCount);
            mStreakFirstTryClearCount.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.MaxStreakClearStageCount);
            mStreakLoginCount.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.MaxStreakLoginCount);
        }
    }
}

