using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        }
        private void ResetData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PlayerDataManager.Inst.LoadUserDataForDebug();
            EventManager.Inst.ActiveEvent("ContentDataRefresh");
            
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Deinitialize();
                item.Initialize();
            }

            EventManager.Inst.ActiveEvent("MainSceneLoadComplete");
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

            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
            foreach (UIBase item in uiBaseArray)
            {
                item.Deinitialize();
                item.Initialize();
            }

            EventManager.Inst.ActiveEvent("MainSceneLoadComplete");
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
