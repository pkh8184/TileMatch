using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrumpTile.GameMain.Core
{
    public class MainManager : MonoBehaviour
    {
        private void Awake()
        {
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);

            if (uiBaseArray != null)
            {
                foreach (UIBase uiBase in uiBaseArray)
                {
                    uiBase.Initialize();
                }    
            }
            else
            {
                Debug.Log("UIBase를 찾지 못했습니다.");
            }
            
            _ = AdManager.Inst;
        }
        private IEnumerator Start()
        {
            yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
            AudioEvent.Play(EAudioKey.BGM_Main);
            
            EventManager.Inst.ActiveEvent("MainSceneLoadComplete");
        }
    }
}

