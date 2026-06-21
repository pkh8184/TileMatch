using System.Collections;
using System.Collections.Generic;
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
        public override void Initialize()
        {
            base.Initialize();

            InitDebugSet();
        }
        private void InitDebugSet()
        {
            bool debugMode = Debug.isDebugBuild;
#if UNITY_EDITOR
            debugMode = true;
#endif
            mShowButton.gameObject.SetActive(debugMode);
            if(!debugMode)
            {
                return;
            }
            mDataResetButton.onClick.AddListener(ResetData);
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
    }   
}
