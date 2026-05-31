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

            foreach (var item in uiBaseArray)
            {
                item.Initialize();
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

