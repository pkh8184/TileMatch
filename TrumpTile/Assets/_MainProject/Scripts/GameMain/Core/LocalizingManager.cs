using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using TrumpTile.GameMain.UI;

namespace TrumpTile.GameMain.Core
{
    /// <summary>
    /// 씬이 로드될 때 현재 언어가 아랍어인 경우
    /// 텍스트를 오른쪽에서부터 시작되게 합니다.
    /// </summary>
    public class LocalizingManager : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += (scene, mode) =>
            {
                bool isRTL = LocalizationSettings.SelectedLocale.Identifier.Code == "ar";
                foreach (var tmp in FindObjectsOfType<TMP_Text>(true))
                {
                    if(tmp.GetComponent<IgnoreRTL>())
                    {
                        continue;
                    }
                    tmp.isRightToLeftText = isRTL;
                }
            };
        }
    }
}

