using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrumpTile.Editor
{
    /// <summary>
    /// 빌드 툴 에디터 윈도우
    /// - 키스토어 비밀번호를 로컬 파일(BuildConfig.local.json)에 저장 (git 미포함)
    /// - Debug / Release 빌드 타입 선택
    /// - APK / AAB 출력 형식 선택
    /// </summary>
    public class BuildTool : EditorWindow
    {
        // ─── 상수 ───────────────────────────────────────────────────────────────
        private const string CONFIG_PATH = "BuildConfig.local.json";
        private const string MENU_PATH   = "Build/Build Tool";

        // ─── 내부 데이터 ────────────────────────────────────────────────────────
        [Serializable]
        private class BuildConfig
        {
            public string keystorePass  = "";
            public string keyaliasName  = "";
            public string keyaliasPass  = "";
        }

        private enum EBuildType  { Debug, Release }
        private enum EOutputType { APK, AAB }

        // ─── 필드 ───────────────────────────────────────────────────────────────
        private BuildConfig  mConfig        = new BuildConfig();
        private EBuildType   mBuildType     = EBuildType.Debug;
        private EOutputType  mOutputType    = EOutputType.APK;
        private bool         mShowPasswords = false;

        // 씬 선택 (Debug 전용)
        private string[] mAllScenePaths    = Array.Empty<string>();
        private string[] mAllSceneNames    = Array.Empty<string>();
        private bool[]   mSceneSelections  = Array.Empty<bool>();
        private Vector2  mSceneScrollPos;

        // 버전 관리
        private int    mVersionCode;
        private int    mVersionMajor;
        private int    mVersionMinor;
        private int    mVersionPatch;

        // ─── 진입점 ─────────────────────────────────────────────────────────────
        [MenuItem(MENU_PATH)]
        public static void OpenWindow()
        {
            BuildTool window = GetWindow<BuildTool>("Build Tool");
            window.minSize = new Vector2(400F, 520F);
            window.LoadConfig();
            window.LoadVersion();
            window.InitSceneList();
            window.Show();
        }

        // ─── GUI ────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            GUILayout.Space(8F);
            DrawSectionLabel("빌드 설정");

            // 빌드 타입
            EBuildType prevBuildType = mBuildType;
            mBuildType  = (EBuildType)  EditorGUILayout.EnumPopup("빌드 타입",   mBuildType);
            if (prevBuildType != mBuildType)
                InitSceneList();

            // 출력 형식
            mOutputType = (EOutputType) EditorGUILayout.EnumPopup("출력 형식",   mOutputType);

            // Debug 전용 씬 선택
            if (mBuildType == EBuildType.Debug)
            {
                GUILayout.Space(8F);
                DrawSectionLabel("씬 선택 (Debug 전용)");
                DrawSceneSelection();
            }

            GUILayout.Space(12F);
            DrawSectionLabel("키스토어 정보 (로컬 전용 — git 미포함)");

            mShowPasswords = EditorGUILayout.Toggle("비밀번호 표시", mShowPasswords);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Keystore Password");
            mConfig.keystorePass = mShowPasswords
                ? EditorGUILayout.TextField(mConfig.keystorePass)
                : EditorGUILayout.PasswordField(mConfig.keystorePass);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Key Alias");
            mConfig.keyaliasName = EditorGUILayout.TextField(mConfig.keyaliasName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Key Alias Password");
            mConfig.keyaliasPass = mShowPasswords
                ? EditorGUILayout.TextField(mConfig.keyaliasPass)
                : EditorGUILayout.PasswordField(mConfig.keyaliasPass);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8F);

            if (GUILayout.Button("비밀번호 로컬 저장"))
            {
                SaveConfig();
                EditorUtility.DisplayDialog("저장 완료", "BuildConfig.local.json에 저장되었습니다.\n(git에는 포함되지 않습니다)", "확인");
            }

            GUILayout.Space(12F);
            DrawVersionSection();

            GUILayout.Space(16F);
            DrawSectionLabel("빌드 실행");

            bool bCanBuild = !string.IsNullOrEmpty(mConfig.keystorePass)
                          && !string.IsNullOrEmpty(mConfig.keyaliasName)
                          && !string.IsNullOrEmpty(mConfig.keyaliasPass);

            if (!bCanBuild)
            {
                EditorGUILayout.HelpBox("키스토어 정보를 모두 입력 후 저장하세요.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!bCanBuild);

            if (GUILayout.Button($"빌드 ({mBuildType} / {mOutputType})", GUILayout.Height(36F)))
            {
                Build();
            }

            EditorGUI.EndDisabledGroup();
        }

        // ─── 버전 관리 ──────────────────────────────────────────────────────────
        private void LoadVersion()
        {
            mVersionCode = PlayerSettings.Android.bundleVersionCode;

            string[] parts = PlayerSettings.bundleVersion.Split('.');
            mVersionMajor = parts.Length > 0 && int.TryParse(parts[0], out int major) ? major : 1;
            mVersionMinor = parts.Length > 1 && int.TryParse(parts[1], out int minor) ? minor : 0;
            mVersionPatch = parts.Length > 2 && int.TryParse(parts[2], out int patch) ? patch : 0;
        }

        private void SaveVersion()
        {
            mVersionCode  = Mathf.Max(1, mVersionCode);
            mVersionMajor = Mathf.Max(0, mVersionMajor);
            mVersionMinor = Mathf.Max(0, mVersionMinor);
            mVersionPatch = Mathf.Max(0, mVersionPatch);

            PlayerSettings.Android.bundleVersionCode = mVersionCode;
            PlayerSettings.bundleVersion             = $"{mVersionMajor}.{mVersionMinor}.{mVersionPatch}";
        }

        private void DrawVersionSection()
        {
            DrawSectionLabel("버전 관리");

            // VersionCode
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Version Code");
            if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(24F)))
                mVersionCode = Mathf.Max(1, mVersionCode - 1);
            mVersionCode = Mathf.Max(1, EditorGUILayout.IntField(mVersionCode, GUILayout.Width(60F)));
            if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(24F)))
                mVersionCode++;
            EditorGUILayout.EndHorizontal();

            // Bundle Version (major.minor.patch)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Bundle Version");

            EditorGUI.BeginChangeCheck();
            int newMajor = Mathf.Max(0, EditorGUILayout.IntField(mVersionMajor, GUILayout.Width(36F)));
            if (EditorGUI.EndChangeCheck() && newMajor != mVersionMajor)
            {
                mVersionMajor = newMajor;
                mVersionMinor = 0;
                mVersionPatch = 0;
            }

            GUILayout.Label(".", GUILayout.Width(8F));

            EditorGUI.BeginChangeCheck();
            int newMinor = Mathf.Max(0, EditorGUILayout.IntField(mVersionMinor, GUILayout.Width(36F)));
            if (EditorGUI.EndChangeCheck() && newMinor != mVersionMinor)
            {
                mVersionMinor = newMinor;
                mVersionPatch = 0;
            }

            GUILayout.Label(".", GUILayout.Width(8F));
            mVersionPatch = Mathf.Max(0, EditorGUILayout.IntField(mVersionPatch, GUILayout.Width(36F)));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2F);
            EditorGUILayout.HelpBox(
                mBuildType == EBuildType.Release
                    ? $"Release 빌드 시 → VersionCode +1, Patch +1 자동 증가"
                    : "Debug 빌드는 버전이 자동 변경되지 않습니다.",
                mBuildType == EBuildType.Release ? MessageType.Info : MessageType.None);

            if (GUILayout.Button("PlayerSettings에 즉시 반영"))
            {
                SaveVersion();
                EditorUtility.DisplayDialog("반영 완료",
                    $"VersionCode: {mVersionCode}\nVersion: {mVersionMajor}.{mVersionMinor}.{mVersionPatch}",
                    "확인");
            }
        }

        // ─── 씬 초기화 / 씬 선택 UI ────────────────────────────────────────────
        private void InitSceneList()
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            mAllScenePaths   = new string[buildScenes.Length];
            mAllSceneNames   = new string[buildScenes.Length];
            mSceneSelections = new bool[buildScenes.Length];

            for (int i = 0; i < buildScenes.Length; i++)
            {
                mAllScenePaths[i]   = buildScenes[i].path;
                mAllSceneNames[i]   = Path.GetFileNameWithoutExtension(buildScenes[i].path);
                mSceneSelections[i] = buildScenes[i].enabled;
            }
        }

        private void DrawSceneSelection()
        {
            if (mAllScenePaths.Length == 0)
            {
                EditorGUILayout.HelpBox("Build Settings에 등록된 씬이 없습니다.", MessageType.Info);
                return;
            }

            // 전체 선택 / 해제
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 선택", EditorStyles.miniButtonLeft))
            {
                for (int i = 0; i < mSceneSelections.Length; i++)
                    mSceneSelections[i] = true;
            }
            if (GUILayout.Button("전체 해제", EditorStyles.miniButtonRight))
            {
                for (int i = 0; i < mSceneSelections.Length; i++)
                    mSceneSelections[i] = false;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4F);

            // 씬 목록 스크롤뷰 (최대 5개 높이)
            float scrollHeight = Mathf.Min(mAllScenePaths.Length, 5) * (EditorGUIUtility.singleLineHeight + 2F);
            mSceneScrollPos = EditorGUILayout.BeginScrollView(mSceneScrollPos, GUILayout.Height(scrollHeight));
            for (int i = 0; i < mAllScenePaths.Length; i++)
            {
                mSceneSelections[i] = EditorGUILayout.ToggleLeft(
                    $"  {i}: {mAllSceneNames[i]}",
                    mSceneSelections[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        // ─── 빌드 로직 ──────────────────────────────────────────────────────────
        private void Build()
        {
            string outputPath = GetOutputPath();
            if (string.IsNullOrEmpty(outputPath))
                return;

            string[] scenes = mBuildType == EBuildType.Debug
                            ? GetSelectedDebugScenes()
                            : GetEnabledScenes();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("빌드 오류", "빌드할 씬을 하나 이상 선택하세요.", "확인");
                return;
            }

            if (mBuildType == EBuildType.Release)
            {
                mVersionCode++;
                mVersionPatch++;
            }
            SaveVersion();

            SetPlayerSettingsForBuild();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = outputPath,
                target           = BuildTarget.Android,
                options          = mBuildType == EBuildType.Debug
                                 ? BuildOptions.Development | BuildOptions.AllowDebugging
                                 : BuildOptions.None,
            };

            BuildReport  report  = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            ResetPlayerSettingsAfterBuild();

            if (summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "빌드 성공",
                    $"빌드 완료!\n경로: {outputPath}\n크기: {summary.totalSize / 1024 / 1024} MB",
                    "확인");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "빌드 실패",
                    $"빌드 실패\n에러 수: {summary.totalErrors}",
                    "확인");
            }
        }

        // ─── PlayerSettings 적용 / 복원 ─────────────────────────────────────────
        private void SetPlayerSettingsForBuild()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName      = "user.keystore";
            PlayerSettings.Android.keystorePass      = mConfig.keystorePass;
            PlayerSettings.Android.keyaliasName      = mConfig.keyaliasName;
            PlayerSettings.Android.keyaliasPass      = mConfig.keyaliasPass;

            EditorUserBuildSettings.buildAppBundle    = mOutputType == EOutputType.AAB;
        }

        private void ResetPlayerSettingsAfterBuild()
        {
            // 빌드 후 비밀번호를 메모리에서 지워 ProjectSettings 에 저장되지 않게 함
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
        }

        // ─── 헬퍼 ───────────────────────────────────────────────────────────────
        private string GetOutputPath()
        {
            string ext      = mOutputType == EOutputType.APK ? "apk" : "aab";
            string binDir   = Path.Combine(Directory.GetCurrentDirectory(), "Bin");
            string fileName = $"{Application.productName}_{mBuildType}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";

            Directory.CreateDirectory(binDir);
            return Path.Combine(binDir, fileName);
        }

        private string[] GetSelectedDebugScenes()
        {
            System.Collections.Generic.List<string> scenes = new System.Collections.Generic.List<string>();
            for (int i = 0; i < mAllScenePaths.Length; i++)
            {
                if (mSceneSelections[i])
                    scenes.Add(mAllScenePaths[i]);
            }
            return scenes.ToArray();
        }

        private static string[] GetEnabledScenes()
        {
            System.Collections.Generic.List<string> scenes = new System.Collections.Generic.List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                    scenes.Add(scene.path);
            }
            return scenes.ToArray();
        }

        private static void DrawSectionLabel(string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            GUILayout.Label(label, style);
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.y    += EditorGUIUtility.singleLineHeight;
            rect.height = 1F;
            EditorGUI.DrawRect(rect, new Color(0.5F, 0.5F, 0.5F, 0.5F));
            GUILayout.Space(4F);
        }

        // ─── 설정 저장 / 로드 ────────────────────────────────────────────────────
        private void SaveConfig()
        {
            string json = JsonUtility.ToJson(mConfig, true);
            File.WriteAllText(CONFIG_PATH, json);
        }

        private void LoadConfig()
        {
            if (!File.Exists(CONFIG_PATH))
                return;

            try
            {
                string json = File.ReadAllText(CONFIG_PATH);
                mConfig = JsonUtility.FromJson<BuildConfig>(json) ?? new BuildConfig();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BuildTool] BuildConfig.local.json 로드 실패: {e.Message}");
                mConfig = new BuildConfig();
            }
        }
    }
}
