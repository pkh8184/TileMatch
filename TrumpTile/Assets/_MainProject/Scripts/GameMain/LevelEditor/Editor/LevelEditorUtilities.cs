#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrumpTile.GameMain.Core;
using TrumpTile.LevelEditor;

namespace TrumpTile.LevelEditor.Editor
{
    /// <summary>
    /// 레벨 에디터 유틸리티 메뉴
    /// </summary>
    public static class LevelEditorMenus
    {
        [MenuItem("Tools/Tile Match/Create Card Tile Assets")]
        public static void CreateCardTileAssets()
        {
            string path = "Assets/Data/TileTypes";

            // 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder("Assets/Data", "TileTypes");

            int created = 0;

            foreach (ECardSuit suit in System.Enum.GetValues(typeof(ECardSuit)))
            {
                // 무늬별 폴더
                string suitFolder = $"{path}/{suit}";
                if (!AssetDatabase.IsValidFolder(suitFolder))
                    AssetDatabase.CreateFolder(path, suit.ToString());

                foreach (ECardRank rank in System.Enum.GetValues(typeof(ECardRank)))
                {
                    // TileTypeData ScriptableObject 생성
                    TileTypeData config = ScriptableObject.CreateInstance<TileTypeData>();
                    config.typeId = $"{suit}_{rank}";
                    config.suit = suit;
                    config.rank = rank;
                    config.weight = 1;

                    string assetPath = $"{suitFolder}/{suit}_{rank}.asset";

                    if (!AssetDatabase.LoadAssetAtPath<TileTypeData>(assetPath))
                    {
                        AssetDatabase.CreateAsset(config, assetPath);
                        created++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete",
                $"Created {created} card tile assets in {path}", "OK");
        }

        [MenuItem("Tools/Tile Match/Validate All Levels")]
        public static void ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData");

            int valid = 0;
            int invalid = 0;
            List<string> errors = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (level != null)
                {
                    string errorMessage;
                    if (level.Validate(out errorMessage))
                    {
                        valid++;
                    }
                    else
                    {
                        invalid++;
                        errors.Add($"• {level.levelName}: {errorMessage}");
                    }
                }
            }

            string message = $"Validation Complete\n\n" +
                $"✓ Valid: {valid}\n" +
                $"✗ Invalid: {invalid}";

            if (errors.Count > 0)
            {
                message += $"\n\nErrors:\n{string.Join("\n", errors.Take(10))}";
                if (errors.Count > 10)
                    message += $"\n... and {errors.Count - 10} more";
            }

            EditorUtility.DisplayDialog("Level Validation", message, "OK");
        }

        [MenuItem("Tools/Tile Match/Export Levels to JSON")]
        public static void ExportLevelsToJSON()
        {
            string exportPath = EditorUtility.SaveFolderPanel("Export Levels", "", "LevelExport");

            if (string.IsNullOrEmpty(exportPath))
                return;

            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            int exported = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (level != null)
                {
                    string json = JsonUtility.ToJson(level, true);
                    string fileName = $"{level.levelName}.json";
                    File.WriteAllText(Path.Combine(exportPath, fileName), json);
                    exported++;
                }
            }

            EditorUtility.DisplayDialog("Export Complete",
                $"Exported {exported} levels to {exportPath}", "OK");
        }

        [MenuItem("Tools/Tile Match/Import Levels from JSON")]
        public static void ImportLevelsFromJSON()
        {
            string importPath = EditorUtility.OpenFolderPanel("Import Levels", "", "");

            if (string.IsNullOrEmpty(importPath))
                return;

            string[] jsonFiles = Directory.GetFiles(importPath, "*.json");
            int imported = 0;

            string outputFolder = "Assets/Data/Levels/Imported";
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Levels"))
                AssetDatabase.CreateFolder("Assets/Data", "Levels");
            if (!AssetDatabase.IsValidFolder(outputFolder))
                AssetDatabase.CreateFolder("Assets/Data/Levels", "Imported");

            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    LevelData level = ScriptableObject.CreateInstance<LevelData>();
                    JsonUtility.FromJsonOverwrite(json, level);

                    string assetPath = $"{outputFolder}/{level.levelName}.asset";
                    AssetDatabase.CreateAsset(level, assetPath);
                    imported++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to import {filePath}: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Import Complete",
                $"Imported {imported} levels", "OK");
        }

        [MenuItem("Tools/Tile Match/Batch Resize Levels")]
        public static void BatchResizeLevels()
        {
            BatchResizeWindow.OpenWindow();
        }
    }

    /// <summary>
    /// 일괄 리사이즈 윈도우
    /// </summary>
    public class BatchResizeWindow : EditorWindow
    {
        private int mNewWidth = 8;
        private int mNewHeight = 8;
        private int mNewMaxLayers = 4;
        private bool mResizeWidth = false;
        private bool mResizeHeight = false;
        private bool mResizeLayers = false;

        public static void OpenWindow()
        {
            BatchResizeWindow window = GetWindow<BatchResizeWindow>();
            window.titleContent = new GUIContent("Batch Resize");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Resize Levels", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Resize all levels in the project. Tiles outside the new bounds will be removed.", MessageType.Warning);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            mResizeWidth = EditorGUILayout.Toggle(mResizeWidth, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!mResizeWidth);
            mNewWidth = EditorGUILayout.IntSlider("New Width", mNewWidth, 4, 12);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mResizeHeight = EditorGUILayout.Toggle(mResizeHeight, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!mResizeHeight);
            mNewHeight = EditorGUILayout.IntSlider("New Height", mNewHeight, 4, 12);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            mResizeLayers = EditorGUILayout.Toggle(mResizeLayers, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!mResizeLayers);
            mNewMaxLayers = EditorGUILayout.IntSlider("New Max Layers", mNewMaxLayers, 1, 5);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUI.BeginDisabledGroup(!mResizeWidth && !mResizeHeight && !mResizeLayers);
            if (GUILayout.Button("Apply to All Levels", GUILayout.Height(30)))
            {
                ApplyResize();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ApplyResize()
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                "This will modify all level assets. This cannot be undone. Continue?",
                "Yes", "Cancel"))
                return;

            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            int modified = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (level != null)
                {
                    bool bChanged = false;

                    if (mResizeWidth && level.boardWidth != mNewWidth)
                    {
                        level.boardWidth = mNewWidth;
                        level.tilePlacements.RemoveAll(t => t.gridX >= mNewWidth);
                        bChanged = true;
                    }

                    if (mResizeHeight && level.boardHeight != mNewHeight)
                    {
                        level.boardHeight = mNewHeight;
                        level.tilePlacements.RemoveAll(t => t.gridY >= mNewHeight);
                        bChanged = true;
                    }

                    if (mResizeLayers && level.maxLayers != mNewMaxLayers)
                    {
                        level.maxLayers = mNewMaxLayers;
                        level.tilePlacements.RemoveAll(t => t.layer >= mNewMaxLayers);
                        bChanged = true;
                    }

                    if (bChanged)
                    {
                        EditorUtility.SetDirty(level);
                        modified++;
                    }
                }
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Complete", $"Modified {modified} levels", "OK");
            Close();
        }
    }

    /// <summary>
    /// 레벨 브라우저 윈도우
    /// </summary>
    public class LevelBrowserWindow : EditorWindow
    {
        private List<LevelData> mAllLevels = new List<LevelData>();
        private Vector2 mScrollPosition;
        private string mSearchFilter = "";
        private ELevelDifficulty? mDifficultyFilter = null;
        private ESortMode mSortMode = ESortMode.LevelNumber;
        private bool mSortAscending = true;

        private enum ESortMode { LevelNumber, Name, Difficulty, TileCount }

        [MenuItem("Tools/Tile Match/Level Browser")]
        public static void OpenWindow()
        {
            LevelBrowserWindow window = GetWindow<LevelBrowserWindow>();
            window.titleContent = new GUIContent("Level Browser", EditorGUIUtility.IconContent("d_Folder Icon").image);
            window.minSize = new Vector2(600, 400);
            window.Show();
            window.RefreshLevelList();
        }

        private void OnEnable()
        {
            RefreshLevelList();
        }

        private void RefreshLevelList()
        {
            mAllLevels.Clear();

            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                    mAllLevels.Add(level);
            }

            ApplySorting();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawLevelList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                RefreshLevelList();

            GUILayout.Space(10);

            GUILayout.Label($"Total: {mAllLevels.Count} levels", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Editor", EditorStyles.toolbarButton, GUILayout.Width(80)))
                LevelEditorWindow.OpenWindow();

            if (GUILayout.Button("Generator", EditorStyles.toolbarButton, GUILayout.Width(70)))
                LevelGeneratorWindow.OpenWindow();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();

            // 검색
            GUILayout.Label("Search:", GUILayout.Width(50));
            mSearchFilter = EditorGUILayout.TextField(mSearchFilter, GUILayout.Width(150));

            // 난이도 필터
            GUILayout.Space(20);
            GUILayout.Label("Difficulty:", GUILayout.Width(60));

            string[] options = new string[] { "All", "Tutorial", "Easy", "Normal", "Hard", "Expert" };
            int selected = mDifficultyFilter.HasValue ? (int)mDifficultyFilter.Value + 1 : 0;
            selected = EditorGUILayout.Popup(selected, options, GUILayout.Width(80));
            mDifficultyFilter = selected == 0 ? null : (ELevelDifficulty?)(selected - 1);

            // 정렬
            GUILayout.Space(20);
            GUILayout.Label("Sort:", GUILayout.Width(35));

            EditorGUI.BeginChangeCheck();
            mSortMode = (ESortMode)EditorGUILayout.EnumPopup(mSortMode, GUILayout.Width(100));
            if (GUILayout.Button(mSortAscending ? "↑" : "↓", GUILayout.Width(25)))
                mSortAscending = !mSortAscending;

            if (EditorGUI.EndChangeCheck())
                ApplySorting();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawLevelList()
        {
            mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition);

            List<LevelData> filteredLevels = GetFilteredLevels();

            foreach (LevelData level in filteredLevels)
            {
                DrawLevelItem(level);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelItem(LevelData level)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 레벨 번호
            GUILayout.Label($"#{level.levelNumber:D3}", EditorStyles.boldLabel, GUILayout.Width(50));

            // 이름
            GUILayout.Label(level.levelName, GUILayout.Width(150));

            // 난이도 (색상 표시)
            Color diffColor = GetDifficultyColor(level.difficulty);
            GUI.backgroundColor = diffColor;
            GUILayout.Label(level.difficulty.ToString(), "box", GUILayout.Width(70));
            GUI.backgroundColor = Color.white;

            // 크기
            GUILayout.Label($"{level.boardWidth}x{level.boardHeight}", GUILayout.Width(50));

            // 타일 수
            GUILayout.Label($"{level.tilePlacements?.Count ?? 0} tiles", GUILayout.Width(70));

            // 유효성
            string errorMsg;
            bool bIsValid = level.Validate(out errorMsg);
            GUILayout.Label(bIsValid ? "✓" : "✗", GUILayout.Width(20));

            GUILayout.FlexibleSpace();

            // 액션 버튼
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                Selection.activeObject = level;
                EditorGUIUtility.PingObject(level);
            }

            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                LevelEditorWindow.OpenWindow();
                // 에디터에 레벨 로드 (에디터 윈도우에서 처리 필요)
            }

            if (GUILayout.Button("Preview", GUILayout.Width(55)))
            {
                LevelPreviewWindow.OpenWithLevel(level);
            }

            EditorGUILayout.EndHorizontal();
        }

        private List<LevelData> GetFilteredLevels()
        {
            IEnumerable<LevelData> filtered = mAllLevels.AsEnumerable();

            // 검색 필터
            if (!string.IsNullOrEmpty(mSearchFilter))
            {
                string lower = mSearchFilter.ToLower();
                filtered = filtered.Where(l =>
                    l.levelName.ToLower().Contains(lower) ||
                    l.levelNumber.ToString().Contains(lower));
            }

            // 난이도 필터
            if (mDifficultyFilter.HasValue)
            {
                filtered = filtered.Where(l => l.difficulty == mDifficultyFilter.Value);
            }

            return filtered.ToList();
        }

        private void ApplySorting()
        {
            switch (mSortMode)
            {
                case ESortMode.LevelNumber:
                    mAllLevels = mSortAscending ?
                        mAllLevels.OrderBy(l => l.levelNumber).ToList() :
                        mAllLevels.OrderByDescending(l => l.levelNumber).ToList();
                    break;
                case ESortMode.Name:
                    mAllLevels = mSortAscending ?
                        mAllLevels.OrderBy(l => l.levelName).ToList() :
                        mAllLevels.OrderByDescending(l => l.levelName).ToList();
                    break;
                case ESortMode.Difficulty:
                    mAllLevels = mSortAscending ?
                        mAllLevels.OrderBy(l => l.difficulty).ToList() :
                        mAllLevels.OrderByDescending(l => l.difficulty).ToList();
                    break;
                case ESortMode.TileCount:
                    mAllLevels = mSortAscending ?
                        mAllLevels.OrderBy(l => l.tilePlacements?.Count ?? 0).ToList() :
                        mAllLevels.OrderByDescending(l => l.tilePlacements?.Count ?? 0).ToList();
                    break;
            }
        }

        private Color GetDifficultyColor(ELevelDifficulty difficulty)
        {
            return difficulty switch
            {
                ELevelDifficulty.Tutorial => new Color(0.5F, 0.8F, 1F),
                ELevelDifficulty.Easy => new Color(0.5F, 1F, 0.5F),
                ELevelDifficulty.Normal => new Color(1F, 1F, 0.5F),
                ELevelDifficulty.Hard => new Color(1F, 0.7F, 0.4F),
                ELevelDifficulty.Expert => new Color(1F, 0.4F, 0.4F),
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// 프로젝트 창에서 레벨 데이터 아이콘 커스터마이징
    /// </summary>
    [InitializeOnLoad]
    public static class LevelDataProjectIcon
    {
        static LevelDataProjectIcon()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.EndsWith(".asset"))
                return;

            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);

            if (level == null)
                return;

            // 아이콘 영역 (왼쪽)
            if (selectionRect.height <= 20) // 리스트 뷰
            {
                Rect iconRect = new Rect(selectionRect.xMax - 50, selectionRect.y, 50, selectionRect.height);

                // 난이도 색상 표시
                Color diffColor = level.difficulty switch
                {
                    ELevelDifficulty.Tutorial => new Color(0.5F, 0.8F, 1F, 0.5F),
                    ELevelDifficulty.Easy => new Color(0.5F, 1F, 0.5F, 0.5F),
                    ELevelDifficulty.Normal => new Color(1F, 1F, 0.5F, 0.5F),
                    ELevelDifficulty.Hard => new Color(1F, 0.7F, 0.4F, 0.5F),
                    ELevelDifficulty.Expert => new Color(1F, 0.4F, 0.4F, 0.5F),
                    _ => Color.clear
                };

                EditorGUI.DrawRect(iconRect, diffColor);

                // 레벨 번호
                GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 9
                };
                GUI.Label(iconRect, $"L{level.levelNumber}", style);
            }
        }
    }
}
#endif
