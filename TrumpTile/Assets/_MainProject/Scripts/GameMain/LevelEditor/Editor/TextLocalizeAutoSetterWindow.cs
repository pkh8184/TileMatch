#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrumpTile.LevelEditor.Editor
{
	/// <summary>
	/// 프로젝트 내 모든 씬 / 프리팹의 TMP를 순회하며,
	/// text가 TBStringMaster의 Ko 또는 En과 일치하면 TextLocalizeSetter를 붙이고 mKey를 지정한다.
	/// - 이미 붙어있고 mKey가 다르면 mKey만 갱신
	/// - text가 테이블에 없으면 실패로 기록(경로 확인용)
	/// </summary>
	public class TextLocalizeAutoSetterWindow : EditorWindow
	{
		private const string DEFAULT_SCENE_FOLDER = "Assets/_MainProject/Scenes";
		private const string DEFAULT_PREFAB_FOLDER = "Assets/_MainProject/Prefabs";

		private enum EApplyResult
		{
			Added,
			Updated,
			Unchanged,
		}

		[Serializable]
		private class FailureRecord
		{
			public string Source;      // "Scene" / "Prefab"
			public string AssetPath;   // .unity / .prefab 에셋 경로
			public string ObjectPath;  // 하이어라키 경로
			public string Text;        // 매칭 실패한 텍스트
		}

		private TBStringMasterTable mMasterTable;
		private string mSceneSearchFolder = DEFAULT_SCENE_FOLDER;
		private string mPrefabSearchFolder = DEFAULT_PREFAB_FOLDER;
		private bool bIncludeScenes = true;
		private bool bIncludePrefabs = true;

		private readonly Dictionary<string, int> mLookup = new Dictionary<string, int>();

		private int mAddedCount;
		private int mUpdatedCount;
		private int mUnchangedCount;
		private readonly List<FailureRecord> mFailures = new List<FailureRecord>();

		private Vector2 mFailureScroll;
		private bool bHasRun;

		[MenuItem("Tools/TMP/Text Localize Auto Setter")]
		static void Open()
		{
			GetWindow<TextLocalizeAutoSetterWindow>("Text Localize Auto Setter");
		}

		private void OnEnable()
		{
			if (mMasterTable == null)
			{
				mMasterTable = FindMasterTable();
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Text Localize Auto Setter", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"모든 씬/프리팹의 TMP text를 TBStringMaster의 Ko·En과 비교합니다.\n" +
				"일치 → TextLocalizeSetter 부착 + mKey 지정 (이미 있으면 mKey만 갱신)\n" +
				"테이블에 없는 텍스트 → 실패 목록에 경로로 기록",
				MessageType.Info
			);

			EditorGUILayout.Space();
			mMasterTable = (TBStringMasterTable)EditorGUILayout.ObjectField(
				"StringMaster Table", mMasterTable, typeof(TBStringMasterTable), false
			);

			EditorGUILayout.Space();
			bIncludeScenes = EditorGUILayout.Toggle("씬 포함", bIncludeScenes);
			using (new EditorGUI.DisabledScope(!bIncludeScenes))
			{
				mSceneSearchFolder = EditorGUILayout.TextField("씬 검색 폴더", mSceneSearchFolder);
			}
			bIncludePrefabs = EditorGUILayout.Toggle("프리팹 포함", bIncludePrefabs);
			using (new EditorGUI.DisabledScope(!bIncludePrefabs))
			{
				mPrefabSearchFolder = EditorGUILayout.TextField("프리팹 검색 폴더", mPrefabSearchFolder);
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("모든 씬/프리팹을 열고 저장합니다. 실행 전 커밋/백업을 권장합니다.", MessageType.Warning);
			if (GUILayout.Button("실행", GUILayout.Height(30)))
			{
				Run();
			}

			DrawResult();
		}

		private void DrawResult()
		{
			if (!bHasRun)
			{
				return;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
			EditorGUILayout.LabelField(
				$"신규 {mAddedCount} / 갱신 {mUpdatedCount} / 변경없음 {mUnchangedCount} / 실패 {mFailures.Count}"
			);

			if (mFailures.Count == 0)
			{
				return;
			}

			EditorGUILayout.Space();
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField($"실패 목록 ({mFailures.Count}) — 테이블에 없는 텍스트", EditorStyles.boldLabel);
				if (GUILayout.Button("전체 복사", GUILayout.Width(80)))
				{
					CopyFailuresToClipboard();
				}
			}

			mFailureScroll = EditorGUILayout.BeginScrollView(mFailureScroll, GUILayout.MinHeight(200));
			foreach (FailureRecord record in mFailures)
			{
				using (new EditorGUILayout.HorizontalScope("box"))
				{
					using (new EditorGUILayout.VerticalScope())
					{
						EditorGUILayout.LabelField($"\"{record.Text}\"", EditorStyles.boldLabel);
						EditorGUILayout.LabelField($"[{record.Source}] {record.AssetPath}", EditorStyles.miniLabel);
						EditorGUILayout.LabelField(record.ObjectPath, EditorStyles.wordWrappedMiniLabel);
					}
					if (GUILayout.Button("Ping", GUILayout.Width(50)))
					{
						PingAsset(record.AssetPath);
					}
				}
			}
			EditorGUILayout.EndScrollView();
		}

		private void Run()
		{
			if (mMasterTable == null)
			{
				mMasterTable = FindMasterTable();
			}
			if (mMasterTable == null)
			{
				EditorUtility.DisplayDialog("오류", "TBStringMasterTable을 찾을 수 없습니다. 직접 지정해주세요.", "확인");
				return;
			}

			BuildLookup();
			if (mLookup.Count == 0)
			{
				EditorUtility.DisplayDialog("오류", "테이블에 유효한 Ko/En 항목이 없습니다.", "확인");
				return;
			}

			if (!EditorUtility.DisplayDialog(
				"확인",
				"모든 씬/프리팹을 열고 수정 후 저장합니다.\n되돌리기 어렵습니다. 계속할까요?",
				"실행", "취소"))
			{
				return;
			}

			mAddedCount = 0;
			mUpdatedCount = 0;
			mUnchangedCount = 0;
			mFailures.Clear();
			bHasRun = false;

			try
			{
				if (bIncludePrefabs)
				{
					ProcessAllPrefabs();
				}
				if (bIncludeScenes)
				{
					ProcessAllScenes();
				}
				AssetDatabase.SaveAssets();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			bHasRun = true;
			Debug.Log($"[TextLocalizeAutoSetter] 완료 — 신규 {mAddedCount}, 갱신 {mUpdatedCount}, 변경없음 {mUnchangedCount}, 실패 {mFailures.Count}");
			if (mFailures.Count > 0)
			{
				Debug.LogWarning($"[TextLocalizeAutoSetter] 실패(테이블 미존재) {mFailures.Count}건. 창의 실패 목록에서 경로 확인.");
			}
			Repaint();
		}

		private void ProcessAllPrefabs()
		{
			string[] guids = FindAssets("t:Prefab", mPrefabSearchFolder, "프리팹");
			for (int i = 0; i < guids.Length; i++)
			{
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (EditorUtility.DisplayCancelableProgressBar("프리팹 처리 중", path, (float)i / guids.Length))
				{
					break;
				}

				GameObject root = PrefabUtility.LoadPrefabContents(path);
				bool bChanged = false;
				try
				{
					bChanged = ProcessRoot(root, "Prefab", path);
					if (bChanged)
					{
						PrefabUtility.SaveAsPrefabAsset(root, path);
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
		}

		private void ProcessAllScenes()
		{
			string[] guids = FindAssets("t:Scene", mSceneSearchFolder, "씬");
			if (guids.Length == 0)
			{
				return;
			}

			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				Debug.LogWarning("[TextLocalizeAutoSetter] 씬 처리 취소 — 현재 씬 저장 안 함.");
				return;
			}

			SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[i]);
					if (EditorUtility.DisplayCancelableProgressBar("씬 처리 중", path, (float)i / guids.Length))
					{
						break;
					}

					Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
					bool bChanged = false;
					GameObject[] roots = scene.GetRootGameObjects();
					foreach (GameObject root in roots)
					{
						if (ProcessRoot(root, "Scene", path))
						{
							bChanged = true;
						}
					}

					if (bChanged)
					{
						EditorSceneManager.MarkSceneDirty(scene);
						EditorSceneManager.SaveScene(scene);
					}
				}
			}
			finally
			{
				if (previousSetup != null && previousSetup.Length > 0)
				{
					EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
				}
			}
		}

		/// <summary>
		/// 루트 하위 모든 TMP를 처리. 하나라도 변경되면 true 반환.
		/// </summary>
		private bool ProcessRoot(GameObject root, string source, string assetPath)
		{
			bool bChanged = false;
			TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
			foreach (TMP_Text tmp in texts)
			{
				// 프리팹 인스턴스(중첩 프리팹 포함) 하위는 원본 프리팹에서 처리되므로 스킵.
				if (PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject))
				{
					continue;
				}

				string text = tmp.text;
				if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim()))
				{
					continue;
				}

				int key;
				if (!mLookup.TryGetValue(Normalize(text), out key))
				{
					mFailures.Add(new FailureRecord
					{
						Source = source,
						AssetPath = assetPath,
						ObjectPath = GetHierarchyPath(tmp.transform),
						Text = text,
					});
					continue;
				}

				EApplyResult result = ApplySetter(tmp.gameObject, key);
				switch (result)
				{
					case EApplyResult.Added:
						mAddedCount++;
						bChanged = true;
						break;
					case EApplyResult.Updated:
						mUpdatedCount++;
						bChanged = true;
						break;
					case EApplyResult.Unchanged:
						mUnchangedCount++;
						break;
				}
			}
			return bChanged;
		}

		/// <summary>
		/// TextLocalizeSetter 부착 및 mKey 지정. 이미 있으면 mKey 갱신(같으면 변경없음).
		/// </summary>
		private EApplyResult ApplySetter(GameObject go, int key)
		{
			TextLocalizeSetter setter = go.GetComponent<TextLocalizeSetter>();
			bool bExisted = setter != null;
			if (!bExisted)
			{
				setter = go.AddComponent<TextLocalizeSetter>();
			}

			SerializedObject so = new SerializedObject(setter);
			SerializedProperty prop = so.FindProperty("mKey");
			if (prop == null)
			{
				Debug.LogError("[TextLocalizeAutoSetter] TextLocalizeSetter에서 'mKey' 필드를 찾지 못했습니다.");
				return bExisted ? EApplyResult.Unchanged : EApplyResult.Added;
			}

			if (bExisted && prop.intValue == key)
			{
				return EApplyResult.Unchanged;
			}

			prop.intValue = key;
			so.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(setter);
			return bExisted ? EApplyResult.Updated : EApplyResult.Added;
		}

		private void BuildLookup()
		{
			mLookup.Clear();
			if (mMasterTable.items == null)
			{
				return;
			}

			int ambiguous = 0;
			foreach (TBStringMasterData item in mMasterTable.items)
			{
				ambiguous += AddLookup(item.Ko, item.Key);
				ambiguous += AddLookup(item.En, item.Key);
			}
			if (ambiguous > 0)
			{
				Debug.LogWarning($"[TextLocalizeAutoSetter] 동일 텍스트가 서로 다른 Key로 중복된 항목 {ambiguous}건 — 먼저 등록된 Key를 사용합니다.");
			}
		}

		/// <summary>동일 텍스트가 다른 Key로 충돌하면 1, 아니면 0 반환.</summary>
		private int AddLookup(string text, int key)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim()))
			{
				return 0;
			}

			string normalized = Normalize(text);
			if (mLookup.TryGetValue(normalized, out int existing))
			{
				return existing != key ? 1 : 0;
			}
			mLookup[normalized] = key;
			return 0;
		}

		private static string Normalize(string text)
		{
			return text.Trim();
		}

		private string[] FindAssets(string filter, string folder, string label)
		{
			if (AssetDatabase.IsValidFolder(folder))
			{
				return AssetDatabase.FindAssets(filter, new string[] { folder });
			}
			Debug.LogWarning($"[TextLocalizeAutoSetter] {label} 폴더 '{folder}'가 유효하지 않아 전체 프로젝트를 검색합니다.");
			return AssetDatabase.FindAssets(filter);
		}

		private void CopyFailuresToClipboard()
		{
			StringBuilder sb = new StringBuilder();
			foreach (FailureRecord record in mFailures)
			{
				sb.AppendLine($"\"{record.Text}\"\t[{record.Source}] {record.AssetPath}\t{record.ObjectPath}");
			}
			EditorGUIUtility.systemCopyBuffer = sb.ToString();
			Debug.Log($"[TextLocalizeAutoSetter] 실패 목록 {mFailures.Count}건 클립보드 복사 완료.");
		}

		private static void PingAsset(string assetPath)
		{
			UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (asset != null)
			{
				EditorGUIUtility.PingObject(asset);
				Selection.activeObject = asset;
			}
		}

		private static string GetHierarchyPath(Transform t)
		{
			string path = t.name;
			Transform parent = t.parent;
			while (parent != null)
			{
				path = parent.name + "/" + path;
				parent = parent.parent;
			}
			return path;
		}

		private static TBStringMasterTable FindMasterTable()
		{
			string[] guids = AssetDatabase.FindAssets("t:TBStringMasterTable");
			if (guids.Length == 0)
			{
				return null;
			}
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);
			return AssetDatabase.LoadAssetAtPath<TBStringMasterTable>(path);
		}
	}
}
#endif
