#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace TrumpTile.LevelEditor.Editor
{
    public class TMPAutoSizeSetter
    {
        [MenuItem("Tools//Set All TMP AutoSize")]
        static void SetAllTMPAutoSize()
        {
            TMP_Text[] allTMP = Object.FindObjectsOfType<TMP_Text>(true);
            foreach (var tmp in allTMP)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10;
                tmp.fontSizeMax = tmp.fontSize;
                tmp.enableWordWrapping = false;
                EditorUtility.SetDirty(tmp);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()
            );
            Debug.Log($"TMP AutoSize 적용 완료 : {allTMP.Length}개");
        }
    }    
}

#endif
