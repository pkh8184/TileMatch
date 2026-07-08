#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using TrumpTile.GameMain.Core;

namespace TrumpTile.LevelEditor.Editor
{
    public class LevelConverterWindow : EditorWindow
    {
        private const string CHAMPIONS_PATH = "Assets/_MainProject/SODatas/ChampionsLevels";
        private const string DAILY_PATH = "Assets/_MainProject/SODatas/DailyLevels";
        private const string LEVEL_PATH = "Assets/_MainProject/SODatas/Levels";
        private Vector2 mOutScroll;
        private Vector2 mInScroll;
        private List<LevelData> mLevelDataList;
        private bool[] mCheckArray;
        private bool[] mLevelListFoldOutArray;
        private bool mSelectedLevelFoldOut;
        [MenuItem("Tools/Tile Match/Level Converter")]
        public static void OpenWindow()
        {
            LevelConverterWindow window = GetWindow<LevelConverterWindow>();
            window.titleContent = new GUIContent("Level Converter", EditorGUIUtility.IconContent("d_console.infoicon").image);
            window.minSize = new Vector2(600, 500);
            window.Show();
        }
        private void OnEnable()
        {
            Initialize();
        }
        private void OnGUI()
        {
            mOutScroll = EditorGUILayout.BeginScrollView(mOutScroll);

            DrawLevelListView();
            EditorGUILayout.Space(10);
            DrawSelectButtonArea();

            EditorGUILayout.EndScrollView();
        }
        private void Initialize()
        {
            mLevelDataList = new List<LevelData>();
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LEVEL_PATH });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if(level.difficulty == EDifficultyType.Bonus) continue;
                mLevelDataList.Add(level);
            }
            mCheckArray = new bool[mLevelDataList.Count];
            mLevelListFoldOutArray = new bool[mLevelDataList.Count];
        }
        private void DrawLevelListView()
        {
            EditorGUILayout.LabelField($"레벨 경로 : Assets/_MainProject/SODatas/Levels, 총 레벨 개수 : {mLevelDataList.Count} (보너스 레벨은 선택에서 제외됩니다.)");
            mInScroll = GUILayout.BeginScrollView(mInScroll, GUILayout.Height(300));
            for(int i = 0; i < mLevelDataList.Count; i++)
            {
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                mLevelListFoldOutArray[i] = EditorGUILayout.Foldout(mLevelListFoldOutArray[i], $"{mLevelDataList[i].levelName}");
                mCheckArray[i] = EditorGUILayout.Toggle(mCheckArray[i]);
                EditorGUILayout.EndHorizontal();

                if(mLevelListFoldOutArray[i])
                {
                    EditorGUI.indentLevel++;
                    int count = mLevelDataList[i].layerList.Sum(x => x.tilePlacementList.Count);
                    bool isAllRandom = mLevelDataList[i].layerList.All(x => x.tilePlacementList.All(y => y.tileTypeId.Contains("Random")));
                    EditorGUILayout.LabelField($"TotalTile : {count}\nDifficulty : {mLevelDataList[i].difficulty}\nIsAllRandomTile : {isAllRandom}", GUILayout.Height(50));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            mSelectedLevelFoldOut = EditorGUILayout.Foldout(mSelectedLevelFoldOut, $"Selected Levels (Total : {mCheckArray.Count(x => x)})");
            if(mSelectedLevelFoldOut)
            {
                EditorGUILayout.BeginVertical();
                for(int i = 0; i < mCheckArray.Length; i++)
                {
                    if(mCheckArray[i])
                    {
                        EditorGUILayout.LabelField($"{mLevelDataList[i].levelName}");
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
        private void DrawSelectButtonArea()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                SelectAllLevel(true);
            }
            if (GUILayout.Button("UnSelect All"))
            {
                SelectAllLevel(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert To Champions Level"))
            {
                ConvertToChampions();
            }
            if (GUILayout.Button("Convert To Daily Level"))
            {
                
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        private void SelectAllLevel(bool isSelect)
        {
            for(int i = 0; i < mCheckArray.Length; i++)
            {
                mCheckArray[i] = isSelect;
            }
            Repaint();
        }
        private void ConvertToChampions()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { CHAMPIONS_PATH });
            List<LevelData> list = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(so => so.levelNumber)
            .ToList();

            int startLevel = list[list.Count - 1].levelNumber;
            for(int i = 0; i < mCheckArray.Length; i++)
            {
                if(mCheckArray[i])
                {
                    LevelData level = CreateInstance<LevelData>();
                    level = mLevelDataList[i].Clone();

                    level.levelNumber = ++startLevel;
                    level.levelName = $"ChampionsLevel_{level.levelNumber}";
                    level.ChampionsLevel = true;

                    string path = CHAMPIONS_PATH + $"/{level.levelName}.asset";
                    AssetDatabase.CreateAsset(level, path);
                    AssetDatabase.SaveAssets();

                    AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
                    AddressableAssetGroup group = settings.FindGroup("ChampionsLevels");
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    settings.CreateOrMoveEntry(guid, group).address = level.levelName;
                }
            }

            Debug.Log($"[LevelConverterWindow] Total {mCheckArray.Count(x => x)} Levels Convert To Champions Level Is Complete");
        }
        private void ConvertToDaily()
        {
            
        }
    }
}
#endif