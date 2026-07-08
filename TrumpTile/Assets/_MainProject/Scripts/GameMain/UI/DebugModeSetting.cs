using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class DebugModeSetting : ViewBase
    {
        [Header("데이터 리셋 버튼")]
        [SerializeField] private Button mDataResetButton;
         [Header("디버그 버튼 표시 플래그 (체크 시 디버그 설정 접근 가능, 에디터 or 디버그빌드에서만 접근 가능)")]
        [SerializeField] private bool mbDebugMode;
        [Header("스테이지 설정 인풋필드")]
        [SerializeField] private TMP_InputField mStageSetter;
        [SerializeField] private Button mStageSetButton;
        [Header("GameTime 디버그 (하루 이동 / 현재 날짜)")]
        [SerializeField] private Button mDayBackwardButton;
        [SerializeField] private Button mDayForwardButton;
        [SerializeField] private TMP_Text mCurrentDateText;
        public override void Initialize()
        {
            base.Initialize();

            InitDebugSet();
        }
        private void InitDebugSet()
        {
            bool debugMode = Debug.isDebugBuild && mbDebugMode;
#if UNITY_EDITOR
            debugMode = true;
#endif
            mShowButton.gameObject.SetActive(debugMode);
            if(!debugMode)
            {
                return;
            }
            mDataResetButton.onClick.AddListener(ResetData);
            mStageSetButton.onClick.AddListener(SetStage);
            mStageSetter.onEndEdit.AddListener(AdjustmentStageNumber);

            if(mDayBackwardButton != null)
            {
                mDayBackwardButton.onClick.AddListener(() => ShiftDay(-1));
            }
            if(mDayForwardButton != null)
            {
                mDayForwardButton.onClick.AddListener(() => ShiftDay(1));
            }
            RefreshDateText();
        }
        //GameTime 오프셋을 하루 단위로 이동시키고, 앱 재시작 없이 일일 리셋(출석/룰렛)을 재평가한다.
        private void ShiftDay(int dayOffset)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameTime.AddOffset(TimeSpan.FromDays(dayOffset));
            PlayerDataManager.Inst.DebugRecheckDailyReset();
            RefreshAllViews();
#endif
            RefreshDateText();
        }
        //모든 UIBase를 재초기화해 갱신된 데이터를 화면에 반영한다.
        private void RefreshAllViews()
        {
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Deinitialize();
                item.Initialize();
            }

            EventManager.Inst.ActiveEvent("MainSceneLoadComplete");
        }
        private void RefreshDateText()
        {
            if(mCurrentDateText == null)
            {
                return;
            }
            mCurrentDateText.text = "현재 : " + GameTime.Now.ToString("yyyy-MM-dd");
        }
        private void ResetData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PlayerDataManager.Inst.LoadUserDataForDebug();
            EventManager.Inst.ActiveEvent("ContentDataRefresh");

            RefreshAllViews();
        }
        private void SetStage()
        {
            if(!int.TryParse(mStageSetter.text, out int result))
            {
                return;
            }
            if(result > CoreData.MAX_STAGE)
            {
                return;
            }
            if(result < 1)
            {
                return;
            }
            PlayerDataManager.Inst.UserData.CurrentStage = result;
            PlayerDataManager.Inst.UserData.IsChampionsActive = false;

            RefreshAllViews();
        }
        private void AdjustmentStageNumber(string str)
        {
            if(!int.TryParse(str, out int result))
            {
                mStageSetter.text = "1";
                return;
            }
            if(result > CoreData.MAX_STAGE)
            {
                mStageSetter.text = CoreData.MAX_STAGE.ToString();
            }
            else if(result < 1)
            {
                mStageSetter.text = "1";
            }
        }
    }   
}
