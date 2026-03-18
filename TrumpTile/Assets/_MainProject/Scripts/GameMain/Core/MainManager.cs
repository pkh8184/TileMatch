using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrumpTile.GameMain.Core
{
    public class MainManager : MonoBehaviour
    {
        private void Awake()
        {
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);

            foreach (UIBase uiBase in uiBaseArray)
            {
                uiBase.Initialize();
            }
        }

        public void ForTest_BtnEvt_SceneTransition()
        {
            SceneManager.LoadScene("GameScene");
        }
    }
}

