#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrumpTile.LevelEditor.Editor
{
    public class LevelLoader : EditorWindow
    {
        private int mLevelIndex = 1;

        [MenuItem("Tools/Level Loader")]
        public static void Open()
        {
            GetWindow<LevelLoader>("Level Loader");
        }

        private void OnGUI()
        {
            mLevelIndex = EditorGUILayout.IntField("레벨 번호", mLevelIndex);

            if (GUILayout.Button("게임씬 플레이"))
            {
                bool exists = AssetDatabase.AssetPathToGUID("Assets/_MainProject/SODatas/Levels/" + string.Format("Level_{0:D3}.asset", mLevelIndex)) != string.Empty;
                if(exists)
                {
                    EditorPrefs.SetInt("DebugLevelIndex", mLevelIndex);

                    EditorSceneManager.OpenScene("Assets/_MainProject/Scenes/GameScene.unity");

                    EditorApplication.isPlaying = true;
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", "레벨 데이터가 존재하지 않습니다.", "확인");
                }
            }
        }
    }
}

#endif
