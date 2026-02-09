#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TileMatch.LevelEditor
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
            
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
            {
                // 무늬별 폴더
                string suitFolder = $"{path}/{suit}";
                if (!AssetDatabase.IsValidFolder(suitFolder))
                    AssetDatabase.CreateFolder(path, suit.ToString());
                
                foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
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
        private int newWidth = 8;
        private int newHeight = 8;
        private int newMaxLayers = 4;
        private bool resizeWidth = false;
        private bool resizeHeight = false;
        private bool resizeLayers = false;
        
        public static void OpenWindow()
        {
            var window = GetWindow<BatchResizeWindow>();
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
            resizeWidth = EditorGUILayout.Toggle(resizeWidth, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!resizeWidth);
            newWidth = EditorGUILayout.IntSlider("New Width", newWidth, 4, 12);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            resizeHeight = EditorGUILayout.Toggle(resizeHeight, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!resizeHeight);
            newHeight = EditorGUILayout.IntSlider("New Height", newHeight, 4, 12);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            resizeLayers = EditorGUILayout.Toggle(resizeLayers, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!resizeLayers);
            newMaxLayers = EditorGUILayout.IntSlider("New Max Layers", newMaxLayers, 1, 5);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            EditorGUI.BeginDisabledGroup(!resizeWidth && !resizeHeight && !resizeLayers);
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
                    bool changed = false;
                    
                    if (resizeWidth && level.boardWidth != newWidth)
                    {
                        level.boardWidth = newWidth;
                        level.tilePlacements.RemoveAll(t => t.gridX >= newWidth);
                        changed = true;
                    }
                    
                    if (resizeHeight && level.boardHeight != newHeight)
                    {
                        level.boardHeight = newHeight;
                        level.tilePlacements.RemoveAll(t => t.gridY >= newHeight);
                        changed = true;
                    }
                    
                    if (resizeLayers && level.maxLayers != newMaxLayers)
                    {
                        level.maxLayers = newMaxLayers;
                        level.tilePlacements.RemoveAll(t => t.layer >= newMaxLayers);
                        changed = true;
                    }
                    
                    if (changed)
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
        private List<LevelData> allLevels = new List<LevelData>();
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private LevelDifficulty? difficultyFilter = null;
        private SortMode sortMode = SortMode.LevelNumber;
        private bool sortAscending = true;
        
        private enum SortMode { LevelNumber, Name, Difficulty, TileCount }
        
        [MenuItem("Tools/Tile Match/Level Browser")]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelBrowserWindow>();
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
            allLevels.Clear();
            
            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                    allLevels.Add(level);
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
            
            GUILayout.Label($"Total: {allLevels.Count} levels", EditorStyles.toolbarButton);
            
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
            searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(150));
            
            // 난이도 필터
            GUILayout.Space(20);
            GUILayout.Label("Difficulty:", GUILayout.Width(60));
            
            string[] options = new string[] { "All", "Tutorial", "Easy", "Normal", "Hard", "Expert" };
            int selected = difficultyFilter.HasValue ? (int)difficultyFilter.Value + 1 : 0;
            selected = EditorGUILayout.Popup(selected, options, GUILayout.Width(80));
            difficultyFilter = selected == 0 ? null : (LevelDifficulty?)(selected - 1);
            
            // 정렬
            GUILayout.Space(20);
            GUILayout.Label("Sort:", GUILayout.Width(35));
            
            EditorGUI.BeginChangeCheck();
            sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(100));
            if (GUILayout.Button(sortAscending ? "↑" : "↓", GUILayout.Width(25)))
                sortAscending = !sortAscending;
            
            if (EditorGUI.EndChangeCheck())
                ApplySorting();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawLevelList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var filteredLevels = GetFilteredLevels();
            
            foreach (var level in filteredLevels)
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
            bool isValid = level.Validate(out errorMsg);
            GUILayout.Label(isValid ? "✓" : "✗", GUILayout.Width(20));
            
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
            var filtered = allLevels.AsEnumerable();
            
            // 검색 필터
            if (!string.IsNullOrEmpty(searchFilter))
            {
                string lower = searchFilter.ToLower();
                filtered = filtered.Where(l => 
                    l.levelName.ToLower().Contains(lower) ||
                    l.levelNumber.ToString().Contains(lower));
            }
            
            // 난이도 필터
            if (difficultyFilter.HasValue)
            {
                filtered = filtered.Where(l => l.difficulty == difficultyFilter.Value);
            }
            
            return filtered.ToList();
        }
        
        private void ApplySorting()
        {
            switch (sortMode)
            {
                case SortMode.LevelNumber:
                    allLevels = sortAscending ?
                        allLevels.OrderBy(l => l.levelNumber).ToList() :
                        allLevels.OrderByDescending(l => l.levelNumber).ToList();
                    break;
                case SortMode.Name:
                    allLevels = sortAscending ?
                        allLevels.OrderBy(l => l.levelName).ToList() :
                        allLevels.OrderByDescending(l => l.levelName).ToList();
                    break;
                case SortMode.Difficulty:
                    allLevels = sortAscending ?
                        allLevels.OrderBy(l => l.difficulty).ToList() :
                        allLevels.OrderByDescending(l => l.difficulty).ToList();
                    break;
                case SortMode.TileCount:
                    allLevels = sortAscending ?
                        allLevels.OrderBy(l => l.tilePlacements?.Count ?? 0).ToList() :
                        allLevels.OrderByDescending(l => l.tilePlacements?.Count ?? 0).ToList();
                    break;
            }
        }
        
        private Color GetDifficultyColor(LevelDifficulty difficulty)
        {
            return difficulty switch
            {
                LevelDifficulty.Tutorial => new Color(0.5f, 0.8f, 1f),
                LevelDifficulty.Easy => new Color(0.5f, 1f, 0.5f),
                LevelDifficulty.Normal => new Color(1f, 1f, 0.5f),
                LevelDifficulty.Hard => new Color(1f, 0.7f, 0.4f),
                LevelDifficulty.Expert => new Color(1f, 0.4f, 0.4f),
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
                    LevelDifficulty.Tutorial => new Color(0.5f, 0.8f, 1f, 0.5f),
                    LevelDifficulty.Easy => new Color(0.5f, 1f, 0.5f, 0.5f),
                    LevelDifficulty.Normal => new Color(1f, 1f, 0.5f, 0.5f),
                    LevelDifficulty.Hard => new Color(1f, 0.7f, 0.4f, 0.5f),
                    LevelDifficulty.Expert => new Color(1f, 0.4f, 0.4f, 0.5f),
                    _ => Color.clear
                };
                
                EditorGUI.DrawRect(iconRect, diffColor);
                
                // 레벨 번호
                var style = new GUIStyle(EditorStyles.miniLabel)
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
