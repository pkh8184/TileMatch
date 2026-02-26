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
        private Vector2 mScrollPosition;
        private List<LevelData> mLevelsToValidate = new List<LevelData>();
        private List<ValidationResult> mValidationResults = new List<ValidationResult>();

        private bool mValidateTileCount = true;
        private bool mValidateMatchCount = true;
        private bool mValidateLevelNaming = true;
        private bool mValidateLayerSorting = true;
        private bool mValidateDuplicateTiles = true;
        private bool mValidateClearability = true;
        private bool mValidateBoardBounds = true;

        private string mLevelFolderPath = "Assets/Resources/Levels";
        private bool mIsValidating = false;

        // 검증 결과 통계
        private int mTotalLevels = 0;
        private int mPassedLevels = 0;
        private int mWarningLevels = 0;
        private int mErrorLevels = 0;

        [MenuItem("Tools/Tile Match/Level Validator")]
        public static void OpenWindow()
        {
            LevelValidatorWindow window = GetWindow<LevelValidatorWindow>();
            window.titleContent = new GUIContent("Level Validator", EditorGUIUtility.IconContent("d_console.infoicon").image);
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnGUI()
        {
            mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition);

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

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
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
            mLevelFolderPath = EditorGUILayout.TextField("Level Folder", mLevelFolderPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Level Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    mLevelFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("✅ Validation Options", EditorStyles.boldLabel);

            mValidateTileCount = EditorGUILayout.Toggle("타일 개수 (matchCount 배수)", mValidateTileCount);
            mValidateMatchCount = EditorGUILayout.Toggle("매칭 가능 타일 (3개씩 존재)", mValidateMatchCount);
            mValidateLevelNaming = EditorGUILayout.Toggle("레벨 이름/번호 일치", mValidateLevelNaming);
            mValidateLayerSorting = EditorGUILayout.Toggle("레이어/Sorting 검증", mValidateLayerSorting);
            mValidateDuplicateTiles = EditorGUILayout.Toggle("중복 타일 검증 (같은 위치+레이어)", mValidateDuplicateTiles);
            mValidateClearability = EditorGUILayout.Toggle("클리어 가능성 검증", mValidateClearability);
            mValidateBoardBounds = EditorGUILayout.Toggle("보드 범위 검증", mValidateBoardBounds);

            EditorGUILayout.EndVertical();
        }

        private void DrawValidateButtons()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3F, 0.7F, 1F);
            if (GUILayout.Button("🔍 Validate All Levels", GUILayout.Height(35)))
            {
                ValidateAllLevels();
            }

            GUI.backgroundColor = new Color(0.3F, 0.8F, 0.3F);
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
            if (mValidationResults.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📊 Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // 통과
            GUI.backgroundColor = new Color(0.3F, 0.8F, 0.3F);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("✅ PASS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{mPassedLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            // 경고
            GUI.backgroundColor = new Color(1F, 0.8F, 0.3F);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("⚠️ WARN", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{mWarningLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            // 오류
            GUI.backgroundColor = new Color(1F, 0.4F, 0.4F);
            EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
            EditorGUILayout.LabelField("❌ ERROR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{mErrorLevels}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Total: {mTotalLevels} levels");
            float passRate = mTotalLevels > 0 ? (float)mPassedLevels / mTotalLevels * 100F : 0F;
            EditorGUILayout.LabelField($"Pass Rate: {passRate:F1}%");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawResults()
        {
            if (mValidationResults.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 Validation Results", EditorStyles.boldLabel);

            foreach (ValidationResult result in mValidationResults)
            {
                DrawSingleResult(result);
            }
        }

        private void DrawSingleResult(ValidationResult result)
        {
            Color bgColor = result.status switch
            {
                EValidationStatus.Pass => new Color(0.2F, 0.6F, 0.2F, 0.3F),
                EValidationStatus.Warning => new Color(0.8F, 0.6F, 0.1F, 0.3F),
                EValidationStatus.Error => new Color(0.8F, 0.2F, 0.2F, 0.3F),
                _ => Color.gray
            };

            EditorGUILayout.BeginVertical("box");

            // 헤더
            EditorGUILayout.BeginHorizontal();

            string statusIcon = result.status switch
            {
                EValidationStatus.Pass => "✅",
                EValidationStatus.Warning => "⚠️",
                EValidationStatus.Error => "❌",
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
                foreach (ValidationIssue issue in result.issues)
                {
                    Color issueColor = issue.severity switch
                    {
                        EIssueSeverity.Error => Color.red,
                        EIssueSeverity.Warning => new Color(1F, 0.6F, 0F),
                        EIssueSeverity.Info => Color.cyan,
                        _ => Color.white
                    };

                    GUIStyle style = new GUIStyle(EditorStyles.label) { normal = { textColor = issueColor } };
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
            mValidationResults.Clear();
            mLevelsToValidate.Clear();

            // 폴더에서 모든 레벨 로드
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { mLevelFolderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    mLevelsToValidate.Add(level);
                }
            }

            // 레벨 번호순 정렬
            mLevelsToValidate = mLevelsToValidate.OrderBy(l => l.levelNumber).ToList();

            // 검증 실행
            mTotalLevels = mLevelsToValidate.Count;
            mPassedLevels = 0;
            mWarningLevels = 0;
            mErrorLevels = 0;

            for (int i = 0; i < mLevelsToValidate.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Validating Levels",
                    $"Level {i + 1}/{mTotalLevels}", (float)i / mTotalLevels);

                ValidationResult result = ValidateLevel(mLevelsToValidate[i]);
                mValidationResults.Add(result);

                switch (result.status)
                {
                    case EValidationStatus.Pass: mPassedLevels++; break;
                    case EValidationStatus.Warning: mWarningLevels++; break;
                    case EValidationStatus.Error: mErrorLevels++; break;
                }
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"[LevelValidator] 검증 완료: {mTotalLevels}개 레벨 중 " +
                      $"✅ {mPassedLevels} PASS, ⚠️ {mWarningLevels} WARN, ❌ {mErrorLevels} ERROR");
        }

        private void ValidateSelectedLevels()
        {
            mValidationResults.Clear();
            mLevelsToValidate.Clear();

            // 선택된 레벨들
            foreach (Object obj in Selection.objects)
            {
                if (obj is LevelData level)
                {
                    mLevelsToValidate.Add(level);
                }
            }

            if (mLevelsToValidate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "LevelData 에셋을 선택해주세요.", "OK");
                return;
            }

            mTotalLevels = mLevelsToValidate.Count;
            mPassedLevels = 0;
            mWarningLevels = 0;
            mErrorLevels = 0;

            foreach (LevelData level in mLevelsToValidate)
            {
                ValidationResult result = ValidateLevel(level);
                mValidationResults.Add(result);

                switch (result.status)
                {
                    case EValidationStatus.Pass: mPassedLevels++; break;
                    case EValidationStatus.Warning: mWarningLevels++; break;
                    case EValidationStatus.Error: mErrorLevels++; break;
                }
            }
        }

        private ValidationResult ValidateLevel(LevelData level)
        {
            ValidationResult result = new ValidationResult
            {
                levelData = level,
                levelName = $"Level {level.levelNumber}: {level.levelName}",
                issues = new List<ValidationIssue>()
            };

            // 1. 타일 개수 검증 (matchCount 배수)
            if (mValidateTileCount)
            {
                ValidateTileCount(level, result);
            }

            // 2. 매칭 가능 타일 검증 (각 타입이 matchCount개씩)
            if (mValidateMatchCount)
            {
                ValidateMatchCount(level, result);
            }

            // 3. 레벨 이름/번호 검증
            if (mValidateLevelNaming)
            {
                ValidateLevelNaming(level, result);
            }

            // 4. 레이어/Sorting 검증
            if (mValidateLayerSorting)
            {
                ValidateLayerSorting(level, result);
            }

            // 5. 중복 타일 검증
            if (mValidateDuplicateTiles)
            {
                ValidateDuplicateTiles(level, result);
            }

            // 6. 클리어 가능성 검증
            if (mValidateClearability)
            {
                ValidateClearability(level, result);
            }

            // 7. 보드 범위 검증
            if (mValidateBoardBounds)
            {
                ValidateBoardBounds(level, result);
            }

            // 최종 상태 결정
            if (result.issues.Any(i => i.severity == EIssueSeverity.Error))
                result.status = EValidationStatus.Error;
            else if (result.issues.Any(i => i.severity == EIssueSeverity.Warning))
                result.status = EValidationStatus.Warning;
            else
                result.status = EValidationStatus.Pass;

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
                    severity = EIssueSeverity.Error,
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
                    severity = EIssueSeverity.Error,
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
            Dictionary<string, int> typeCounts = level.tilePlacements
                .Where(t => !string.IsNullOrEmpty(t.tileTypeId))
                .GroupBy(t => t.tileTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (KeyValuePair<string, int> kvp in typeCounts)
            {
                if (kvp.Value % matchCount != 0)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = EIssueSeverity.Error,
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
                    severity = EIssueSeverity.Warning,
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
                        severity = EIssueSeverity.Warning,
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
                    severity = EIssueSeverity.Info,
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
                    severity = EIssueSeverity.Warning,
                    category = "Layer",
                    message = $"음수 레이어({minLayer})가 존재합니다."
                });
            }

            if (maxLayer >= level.maxLayers)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = EIssueSeverity.Warning,
                    category = "Layer",
                    message = $"타일 레이어({maxLayer})가 maxLayers({level.maxLayers})를 초과합니다."
                });
            }

            // 레이어 연속성 체크
            List<int> usedLayers = level.tilePlacements.Select(t => t.layer).Distinct().OrderBy(l => l).ToList();
            for (int i = 0; i < usedLayers.Count - 1; i++)
            {
                if (usedLayers[i + 1] - usedLayers[i] > 1)
                {
                    result.issues.Add(new ValidationIssue
                    {
                        severity = EIssueSeverity.Info,
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
                        severity = EIssueSeverity.Error,
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
            Dictionary<string, int> typeCounts = level.tilePlacements
                .Where(t => !string.IsNullOrEmpty(t.tileTypeId))
                .GroupBy(t => t.tileTypeId)
                .ToDictionary(g => g.Key, g => g.Count());

            bool bAllClearable = typeCounts.All(kvp => kvp.Value % matchCount == 0);

            if (!bAllClearable)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = EIssueSeverity.Error,
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
                    severity = EIssueSeverity.Error,
                    category = "Clearability",
                    message = $"클리어 불가능! 총 타일 수({totalTiles})가 matchCount({matchCount}) 배수가 아닙니다."
                });
            }
        }

        private void ValidateBoardBounds(LevelData level, ValidationResult result)
        {
            if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

            List<TilePlacement> outOfBounds = level.tilePlacements.Where(t =>
                t.gridX < 0 || t.gridX >= level.boardWidth ||
                t.gridY < 0 || t.gridY >= level.boardHeight
            ).ToList();

            if (outOfBounds.Count > 0)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = EIssueSeverity.Error,
                    category = "Bounds",
                    message = $"{outOfBounds.Count}개 타일이 보드 범위({level.boardWidth}x{level.boardHeight})를 벗어났습니다."
                });
            }
        }

        #endregion

        #region Utility Methods

        private void ExportReport()
        {
            if (mValidationResults.Count == 0)
            {
                EditorUtility.DisplayDialog("No Results", "먼저 검증을 실행해주세요.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Level Validation Report ===");
            sb.AppendLine($"Date: {System.DateTime.Now}");
            sb.AppendLine($"Total: {mTotalLevels} levels");
            sb.AppendLine($"Pass: {mPassedLevels}, Warning: {mWarningLevels}, Error: {mErrorLevels}");
            sb.AppendLine();

            foreach (ValidationResult result in mValidationResults)
            {
                string status = result.status switch
                {
                    EValidationStatus.Pass => "[PASS]",
                    EValidationStatus.Warning => "[WARN]",
                    EValidationStatus.Error => "[ERROR]",
                    _ => "[???]"
                };

                sb.AppendLine($"{status} {result.levelName}");

                foreach (ValidationIssue issue in result.issues)
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
            if (mValidationResults.Count == 0)
            {
                EditorUtility.DisplayDialog("No Results", "먼저 검증을 실행해주세요.", "OK");
                return;
            }

            int fixedCount = 0;

            foreach (ValidationResult result in mValidationResults.Where(r => r.status != EValidationStatus.Pass))
            {
                if (result.levelData == null) continue;

                bool bModified = false;

                // 중복 타일 제거
                if (mValidateDuplicateTiles)
                {
                    var duplicateGroups = result.levelData.tilePlacements
                        .GroupBy(t => new { t.gridX, t.gridY, t.layer })
                        .Where(g => g.Count() > 1)
                        .ToList();

                    foreach (var group in duplicateGroups)
                    {
                        // 첫 번째만 남기고 나머지 제거
                        List<TilePlacement> toRemove = group.Skip(1).ToList();
                        foreach (TilePlacement tile in toRemove)
                        {
                            result.levelData.tilePlacements.Remove(tile);
                            bModified = true;
                            fixedCount++;
                        }
                    }
                }

                // 레벨 번호 수정 (파일명 기준)
                if (mValidateLevelNaming)
                {
                    string assetPath = AssetDatabase.GetAssetPath(result.levelData);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    string numberStr = new string(fileName.Where(char.IsDigit).ToArray());

                    if (int.TryParse(numberStr, out int fileNumber))
                    {
                        if (fileNumber != result.levelData.levelNumber)
                        {
                            result.levelData.levelNumber = fileNumber;
                            bModified = true;
                            fixedCount++;
                        }
                    }
                }

                if (bModified)
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

        private enum EValidationStatus { Pass, Warning, Error }
        private enum EIssueSeverity { Info, Warning, Error }

        private class ValidationResult
        {
            public LevelData levelData;
            public string levelName;
            public EValidationStatus status;
            public List<ValidationIssue> issues;
        }

        private class ValidationIssue
        {
            public EIssueSeverity severity;
            public string category;
            public string message;
        }

        #endregion
    }
}
#endif
