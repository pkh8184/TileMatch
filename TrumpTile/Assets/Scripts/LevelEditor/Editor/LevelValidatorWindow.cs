#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileMatch.LevelEditor
{
    /// <summary>
    /// 레벨 검토/검증 윈도우
    /// </summary>
    public class LevelValidatorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<LevelData> levelsToValidate = new List<LevelData>();
        private List<ValidationResult> validationResults = new List<ValidationResult>();
        
        private bool validateTileCount = true;
        private bool validateMatchCount = true;
        private bool validateLevelNaming = true;
        private bool validateLayerSorting = true;
        private bool validateDuplicateTiles = true;
        private bool validateClearability = true;
        private bool validateBoardBounds = true;
        
        private string levelFolderPath = "Assets/Resources/Levels";
        private bool isValidating = false;
        
        // 검증 결과 통계
        private int totalLevels = 0;
        private int passedLevels = 0;
        private int warningLevels = 0;
        private int errorLevels = 0;

        [MenuItem("Tools/Tile Match/Level Validator")]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelValidatorWindow>();
            window.titleContent = new GUIContent("Level Validator", EditorGUIUtility.IconContent("d_console.infoicon").image);
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawSettings();
            DrawValidateButtons();
            DrawStatistics();
            DrawResults();

            EditorGUILayout.EndScrollView();
        }

        #region Draw Methods

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🔍 Level Validator", headerStyle);

            EditorGUILayout.HelpBox(
                "레벨 데이터의 오류를 검토합니다.\n" +
                "• 타일 개수 검증 (matchCount 배수)\n" +
                "• 레이어/Sorting 검증\n" +
                "• 중복 타일 검증\n" +
                "• 클리어 가능성 검증\n" +
                "• 레벨 이름 검증",
                MessageType.Info);

            EditorGUILayout.Space(5);
        }

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📁 Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            levelFolderPath = EditorGUILayout.TextField("Level Folder", levelFolderPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Level Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    levelFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("✅ Validation Options", EditorStyles.boldLabel);

            validateTileCount = EditorGUILayout.Toggle("타일 개수 (matchCount 배수)", validateTileCount);
            validateMatchCount = EditorGUILayout.Toggle("매칭 가능 타일 (3개씩 존재)", validateMatchCount);
            validateLevelNaming = EditorGUILayout.Toggle("레벨 이름/번호 일치", validateLevelNaming);
            validateLayerSorting = EditorGUILayout.Toggle("레이어/Sorting 검증", validateLayerSorting);
            validateDuplicateTiles = EditorGUILayout.Toggle("중복 타일 검증 (같은 위치+레이어)", validateDuplicateTiles);
            validateClearability = EditorGUILayout.Toggle("클리어 가능성 검증", validateClearability);
            validateBoardBounds = EditorGUILayout.Toggle("보드 범위 검증", validateBoardBounds);

            EditorGUILayout.EndVertical();
        }

        private void DrawValidateButtons()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("🔍 Validate All Levels", GUILayout.Height(35)))
            {
                ValidateAllLevels();
            }

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("✅ Validate Selected", GUILayout.Height(35)))
            {
                ValidateSelectedLevels();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📋 Export Report"))
            {
                ExportReport();
            }

            if (GUILayout.Button("🔧 Auto Fix (Safe)"))
            {
                AutoFixSafeIssues();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatistics()
        {
            if (validationResults.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 통과
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("✅ PASS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{passedLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            // 경고
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("⚠️ WARN", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{warningLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            // 오류
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("❌ ERROR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{errorLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Total: {totalLevels} levels");
            float passRate = totalLevels > 0 ? (float)passedLevels / totalLevels * 100f : 0f;
            EditorGUILayout.LabelField($"Pass Rate: {passRate:F1}%");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawResults()
        {
            if (validationResults.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 Validation Results", EditorStyles.boldLabel);

            foreach (var result in validationResults)
            {
                DrawSingleResult(result);
            }
        }

        private void DrawSingleResult(ValidationResult result)
        {
            Color bgColor = result.status switch
            {
                ValidationStatus.Pass => new Color(0.2f, 0.6f, 0.2f, 0.3f),
                ValidationStatus.Warning => new Color(0.8f, 0.6f, 0.1f, 0.3f),
                ValidationStatus.Error => new Color(0.8f, 0.2f, 0.2f, 0.3f),
                _ => Color.gray
            };

            EditorGUILayout.BeginVertical("box");

            // 헤더
            EditorGUILayout.BeginHorizontal();

            string statusIcon = result.status switch
            {
                ValidationStatus.Pass => "✅",
                ValidationStatus.Warning => "⚠️",
                ValidationStatus.Error => "❌",
                _ => "❓"
            };

            EditorGUILayout.LabelField($"{statusIcon} {result.levelName}", EditorStyles.boldLabel, GUILayout.Width(200));

            if (result.levelData != null)
            {
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = result.levelData;
                    EditorGUIUtility.PingObject(result.levelData);
                }
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    // 레벨 에디터에서 열기
                    LevelEditorWindow.OpenWindow();
                }
            }

            EditorGUILayout.EndHorizontal();

            // 이슈 목록
            if (result.issues.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var issue in result.issues)
                {
                    Color issueColor = issue.severity switch
                    {
                        IssueSeverity.Error => Color.red,
                        IssueSeverity.Warning => new Color(1f, 0.6f, 0f),
                        IssueSeverity.Info => Color.cyan,
                        _ => Color.white
                    };

                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = issueColor } };
                    EditorGUILayout.LabelField($"• {issue.message}", style);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Validation Logic

        private void ValidateAllLevels()
        {
            validationResults.Clear();
            levelsToValidate.Clear();

            // 폴더에서 모든 레벨 로드
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { levelFolderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    levelsToValidate.Add(level);
                }
            }

            // 레벨 번호순 정렬
            levelsToValidate = levelsToValidate.OrderBy(l => l.levelNumber).ToList();

            // 검증 실행
            totalLevels = levelsToValidate.Count;
            passedLevels = 0;
            warningLevels = 0;
            errorLevels = 0;

            for (int i = 0; i < levelsToValidate.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Validating Levels", 
                    $"Level {i + 1}/{totalLevels}", (float)i / totalLevels);

                var result = ValidateLevel(levelsToValidate[i]);
                validationResults.Add(result);

                switch (result.status)
                {
                    case ValidationStatus.Pass: passedLevels++; break;
                    case ValidationStatus.Warning: warningLevels++; break;
                    case ValidationStatus.Error: errorLevels++; break;
                }
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"[LevelValidator] 검증 완료: {totalLevels}개 레벨 중 " +
                      $"✅ {passedLevels} PASS, ⚠️ {warningLevels} WARN, ❌ {errorLevels} ERROR");
        }

        private void ValidateSelectedLevels()
        {
            validationResults.Clear();
            levelsToValidate.Clear();

            // 선택된 레벨들
            foreach (var obj in Selection.objects)
            {
                if (obj is LevelData level)
                {
                    levelsToValidate.Add(level);
                }
            }

            if (levelsToValidate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "LevelData 에셋을 선택해주세요.", "OK");
                return;
            }

            totalLevels = levelsToValidate.Count;
            passedLevels = 0;
            warningLevels = 0;
            errorLevels = 0;

            foreach (var level in levelsToValidate)
            {
                var result = ValidateLevel(level);
                validationResults.Add(result);

                switch (result.status)
                {
                    case ValidationStatus.Pass: passedLevels++; break;
                    case ValidationStatus.Warning: warningLevels++; break;
                    case ValidationStatus.Error: errorLevels++; break;
                }
            }
        }

        private ValidationResult ValidateLevel(LevelData level)
        {
            var result = new ValidationResult
            {
                levelData = level,
                levelName = $"Level {level.levelNumber}: {level.levelName}",
                issues = new List<ValidationIssue>()
            };

            // 1. 타일 개수 검증 (matchCount 배수)
            if (validateTileCount)
            {
                ValidateTileCount(level, result);
            }

            // 2. 매칭 가능 타일 검증 (각 타입이 matchCount개씩)
            if (validateMatchCount)
            {
                ValidateMatchCount(level, result);
            }

            // 3. 레벨 이름/번호 검증
            if (validateLevelNaming)
            {
                ValidateLevelNaming(level, result);
            }

            // 4. 레이어/Sorting 검증
            if (validateLayerSorting)
            {
                ValidateLayerSorting(level, result);
            }

            // 5. 중복 타일 검증
            if (validateDuplicateTiles)
            {
                ValidateDuplicateTiles(level, result);
            }

            // 6. 클리어 가능성 검증
            if (validateClearability)
            {
                ValidateClearability(level, result);
            }

            // 7. 보드 범위 검증
            if (validateBoardBounds)
            {
                ValidateBoardBounds(level, result);
            }

            // 최종 상태 결정
            if (result.issues.Any(i => i.severity == IssueSeverity.Error))
                result.status = ValidationStatus.Error;
            else if (result.issues.Any(i => i.severity == IssueSeverity.Warning))
                result.status = ValidationStatus.Warning;
            else
                result.status = ValidationStatus.Pass;

            return result;
        }

        #endregion

        #region Individual Validations

        private void ValidateTileCount(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "TileCount",
                    message = "타일이 없습니다!"
                });
                return;
            }

            int tileCount = level.tilePlacements.Count;
            int matchCount = level.matchCount > 0 ? level.matchCount : 3;

            if (tileCount % matchCount != 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "TileCount",
                    message = $"타일 개수({tileCount})가 matchCount({matchCount})의 배수가 아닙니다!"
                });
            }
        }

        private void ValidateMatchCount(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            int matchCount = level.matchCount > 0 ? level.matchCount : 3;

            // 타일 타입별 개수
            var typeCounts = level.tilePlacements
                .Where(t => !string.IsNullOrEmpty(t.tileTypeId))
                .GroupBy(t => t.tileTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in typeCounts)
            {
                if (kvp.Value % matchCount != 0)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Error,
                        category = "MatchCount",
                        message = $"타일 '{kvp.Key}'의 개수({kvp.Value})가 {matchCount}의 배수가 아닙니다!"
                    });
                }
            }

            // 빈 타일 타입 체크
            int emptyTypeCount = level.tilePlacements.Count(t => string.IsNullOrEmpty(t.tileTypeId));
            if (emptyTypeCount > 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "MatchCount",
                    message = $"타일 타입이 지정되지 않은 타일이 {emptyTypeCount}개 있습니다."
                });
            }
        }

        private void ValidateLevelNaming(LevelData level, ValidationResult result)
        {
            // 에셋 경로에서 파일명 추출
            string assetPath = AssetDatabase.GetAssetPath(level);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            // 파일명에서 숫자 추출
            string numberStr = new string(fileName.Where(char.IsDigit).ToArray());
            if (int.TryParse(numberStr, out int fileNumber))
            {
                if (fileNumber != level.levelNumber)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Warning,
                        category = "Naming",
                        message = $"파일명({fileName})과 레벨 번호({level.levelNumber})가 일치하지 않습니다."
                    });
                }
            }

            // 레벨 이름 체크
            if (string.IsNullOrEmpty(level.levelName))
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Info,
                    category = "Naming",
                    message = "레벨 이름이 비어있습니다."
                });
            }
        }

        private void ValidateLayerSorting(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            // 레이어 범위 체크
            int maxLayer = level.tilePlacements.Max(t => t.layer);
            int minLayer = level.tilePlacements.Min(t => t.layer);

            if (minLayer < 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "Layer",
                    message = $"음수 레이어({minLayer})가 존재합니다."
                });
            }

            if (maxLayer >= level.maxLayers)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    category = "Layer",
                    message = $"타일 레이어({maxLayer})가 maxLayers({level.maxLayers})를 초과합니다."
                });
            }

            // 레이어 연속성 체크
            var usedLayers = level.tilePlacements.Select(t => t.layer).Distinct().OrderBy(l => l).ToList();
            for (int i = 0; i < usedLayers.Count - 1; i++)
            {
                if (usedLayers[i + 1] - usedLayers[i] > 1)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Info,
                        category = "Layer",
                        message = $"레이어 {usedLayers[i]}와 {usedLayers[i + 1]} 사이에 빈 레이어가 있습니다."
                    });
                }
            }
        }

        private void ValidateDuplicateTiles(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            // 같은 위치 + 같은 레이어에 중복 타일 체크
            var duplicates = level.tilePlacements
                .GroupBy(t => new { t.gridX, t.gridY, t.layer })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                foreach (var dup in duplicates)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = IssueSeverity.Error,
                        category = "Duplicate",
                        message = $"중복 타일: 위치({dup.Key.gridX}, {dup.Key.gridY}), 레이어 {dup.Key.layer}에 {dup.Count()}개 타일"
                    });
                }
            }
        }

        private void ValidateClearability(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            int matchCount = level.matchCount > 0 ? level.matchCount : 3;

            // 각 타일 타입이 matchCount 배수인지 (이미 ValidateMatchCount에서 체크하지만 한번 더)
            var typeCounts = level.tilePlacements
                .Where(t => !string.IsNullOrEmpty(t.tileTypeId))
                .GroupBy(t => t.tileTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            bool allClearable = typeCounts.All(kvp => kvp.Value % matchCount == 0);

            if (!allClearable)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "Clearability",
                    message = "클리어 불가능! 일부 타일 타입의 개수가 matchCount 배수가 아닙니다."
                });
            }

            // 총 타일 수가 matchCount 배수인지
            int totalTiles = level.tilePlacements.Count;
            if (totalTiles % matchCount != 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "Clearability",
                    message = $"클리어 불가능! 총 타일 수({totalTiles})가 matchCount({matchCount}) 배수가 아닙니다."
                });
            }
        }

        private void ValidateBoardBounds(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            var outOfBounds = level.tilePlacements.Where(t =>
                t.gridX < 0 || t.gridX >= level.boardWidth ||
                t.gridY < 0 || t.gridY >= level.boardHeight
            ).ToList();

            if (outOfBounds.Count > 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    category = "Bounds",
                    message = $"{outOfBounds.Count}개 타일이 보드 범위({level.boardWidth}x{level.boardHeight})를 벗어났습니다."
                });
            }
        }

        #endregion

        #region Utility Methods

        private void ExportReport()
        {
            if (validationResults.Count == 0)
            {
                EditorUtility.DisplayDialog("No Results", "먼저 검증을 실행해주세요.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Level Validation Report ===");
            sb.AppendLine($"Date: {System.DateTime.Now}");
            sb.AppendLine($"Total: {totalLevels} levels");
            sb.AppendLine($"Pass: {passedLevels}, Warning: {warningLevels}, Error: {errorLevels}");
            sb.AppendLine();

            foreach (var result in validationResults)
            {
                string status = result.status switch
                {
                    ValidationStatus.Pass => "[PASS]",
                    ValidationStatus.Warning => "[WARN]",
                    ValidationStatus.Error => "[ERROR]",
                    _ => "[???]"
                };

                sb.AppendLine($"{status} {result.levelName}");

                foreach (var issue in result.issues)
                {
                    sb.AppendLine($"  - [{issue.severity}] {issue.message}");
                }
                sb.AppendLine();
            }

            string path = EditorUtility.SaveFilePanel("Save Report", "", "LevelValidationReport.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Exported", $"리포트가 저장되었습니다:\n{path}", "OK");
            }
        }

        private void AutoFixSafeIssues()
        {
            if (validationResults.Count == 0)
            {
                EditorUtility.DisplayDialog("No Results", "먼저 검증을 실행해주세요.", "OK");
                return;
            }

            int fixedCount = 0;

            foreach (var result in validationResults.Where(r => r.status != ValidationStatus.Pass))
            {
                if (result.levelData == null) continue;

                bool modified = false;

                // 중복 타일 제거
                if (validateDuplicateTiles)
                {
                    var duplicateGroups = result.levelData.tilePlacements
                        .GroupBy(t => new { t.gridX, t.gridY, t.layer })
                        .Where(g => g.Count() > 1)
                        .ToList();

                    foreach (var group in duplicateGroups)
                    {
                        // 첫 번째만 남기고 나머지 제거
                        var toRemove = group.Skip(1).ToList();
                        foreach (var tile in toRemove)
                        {
                            result.levelData.tilePlacements.Remove(tile);
                            modified = true;
                            fixedCount++;
                        }
                    }
                }

                // 레벨 번호 수정 (파일명 기준)
                if (validateLevelNaming)
                {
                    string assetPath = AssetDatabase.GetAssetPath(result.levelData);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    string numberStr = new string(fileName.Where(char.IsDigit).ToArray());
                    
                    if (int.TryParse(numberStr, out int fileNumber))
                    {
                        if (fileNumber != result.levelData.levelNumber)
                        {
                            result.levelData.levelNumber = fileNumber;
                            modified = true;
                            fixedCount++;
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(result.levelData);
                }
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Auto Fix Complete", 
                $"{fixedCount}개 이슈가 자동 수정되었습니다.\n다시 검증을 실행해주세요.", "OK");

            // 다시 검증
            ValidateAllLevels();
        }

        #endregion

        #region Data Classes

        private enum ValidationStatus { Pass, Warning, Error }
        private enum IssueSeverity { Info, Warning, Error }

        private class ValidationResult
        {
            public LevelData levelData;
            public string levelName;
            public ValidationStatus status;
            public List<ValidationIssue> issues;
        }

        private class ValidationIssue
        {
            public IssueSeverity severity;
            public string category;
            public string message;
        }

        #endregion
    }
}
#endif
