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

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Renumber Champions Levels (이름/주소 1부터 재정렬)"))
            {
                RenumberChampionsLevels();
            }

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
            // 기존 챔피언스 레벨 이름(ChampionsLevel_N)의 N 최대값을 찾아 이어서 넘버링
            // (levelNumber는 원본 값을 유지하므로 네이밍 기준은 levelName에서 파싱)
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { CHAMPIONS_PATH });
            int startLevel = 0;
            foreach (string existingGuid in guids)
            {
                LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(existingGuid));
                string suffix = existing.levelName.Replace("ChampionsLevel_", "");
                if (int.TryParse(suffix, out int number) && number > startLevel)
                {
                    startLevel = number;
                }
            }

            for(int i = 0; i < mCheckArray.Length; i++)
            {
                if(mCheckArray[i])
                {
                    LevelData level = mLevelDataList[i].Clone();

                    // levelName / 어드레서블 주소는 1부터 이어지는 연속 번호로 부여
                    // levelNumber 필드는 원본 레벨데이터 값을 그대로 유지 (건드리지 않음)
                    int championsNumber = ++startLevel;
                    level.levelName = $"ChampionsLevel_{championsNumber}";
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
        /// <summary>
        /// 기존 챔피언스 레벨들의 이름/어드레서블 주소를 현재 순서 기준 1부터 연속으로 재정렬.
        /// levelNumber 필드는 건드리지 않는다.
        /// </summary>
        private void RenumberChampionsLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { CHAMPIONS_PATH });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "챔피언스 레벨이 없습니다.", "확인");
                return;
            }

            List<LevelData> list = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(l => ParseChampionsNumber(l.levelName))
                .ToList();

            if (!EditorUtility.DisplayDialog(
                "챔피언스 레벨 재정렬",
                $"{list.Count}개의 이름/어드레서블 주소를 현재 순서 기준 1 ~ {list.Count} 로 재정렬합니다.\n(levelNumber 필드는 유지됩니다.)\n\n계속할까요?",
                "재정렬", "취소"))
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup("ChampionsLevels");
            if (group == null)
            {
                EditorUtility.DisplayDialog("오류", "'ChampionsLevels' 어드레서블 그룹을 찾을 수 없습니다.", "확인");
                return;
            }

            try
            {
                // 1패스: 임시 이름으로 변경 (하향 번호 재배치 시 파일명 충돌 방지)
                for (int i = 0; i < list.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("챔피언스 레벨 재정렬", "임시 이름 지정 중...", (float)i / list.Count * 0.5f);
                    string path = AssetDatabase.GetAssetPath(list[i]);
                    AssetDatabase.RenameAsset(path, $"__champions_tmp_{i}");
                }

                // 2패스: 1부터 연속 번호로 확정
                for (int i = 0; i < list.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("챔피언스 레벨 재정렬", "번호 부여 중...", 0.5f + (float)i / list.Count * 0.5f);
                    int number = i + 1;
                    string newName = $"ChampionsLevel_{number}";

                    string path = AssetDatabase.GetAssetPath(list[i]);
                    AssetDatabase.RenameAsset(path, newName);

                    list[i].levelName = newName; // levelNumber는 건드리지 않음
                    EditorUtility.SetDirty(list[i]);

                    string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(list[i]));
                    settings.CreateOrMoveEntry(guid, group).address = newName;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[LevelConverterWindow] Renumbered {list.Count} champions levels → ChampionsLevel_1 ~ ChampionsLevel_{list.Count}");
        }
        private int ParseChampionsNumber(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                return int.MaxValue;
            }
            string suffix = levelName.Replace("ChampionsLevel_", "");
            return int.TryParse(suffix, out int number) ? number : int.MaxValue;
        }
    }
}
#endif