using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class SpreadsheetImporter : EditorWindow
	{
		private const string MENU_PATH   = "Tools/Spreadsheet Importer";
		public const string  PREF_URL_KEY = "SpreadsheetImporter_URL";

		private string mUrl         = "";
		private string mStatusMsg   = "";
		private bool   bIsImporting = false;

		[MenuItem(MENU_PATH)]
		public static void OpenWindow()
		{
			SpreadsheetImporter window = GetWindow<SpreadsheetImporter>("Spreadsheet Importer");
			window.minSize = new Vector2(420F, 180F);
			window.maxSize = new Vector2(800F, 180F);
			window.mUrl   = EditorPrefs.GetString(PREF_URL_KEY, "");
			window.Show();
		}

		private void OnGUI()
		{
			GUILayout.Space(8F);
			DrawSectionLabel("Google Sheets Web App URL");

			GUILayout.Space(4F);
			EditorGUI.BeginChangeCheck();
			mUrl = EditorGUILayout.TextField("URL", mUrl);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetString(PREF_URL_KEY, mUrl);
			}

			GUILayout.Space(8F);

			EditorGUI.BeginDisabledGroup(bIsImporting || string.IsNullOrEmpty(mUrl));
			if (GUILayout.Button(bIsImporting ? "가져오는 중..." : "Import All", GUILayout.Height(32F)))
			{
				ImportAllAsync();
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.Space(8F);

			if (!string.IsNullOrEmpty(mStatusMsg))
			{
				EditorGUILayout.HelpBox(mStatusMsg, bIsImporting ? MessageType.Info : MessageType.None);
			}
		}

		private async void ImportAllAsync()
		{
			bIsImporting = true;
			mStatusMsg   = "전체 Import 중...";
			Repaint();

			try
			{
				ESheetType[] sheetTypes = (ESheetType[])Enum.GetValues(typeof(ESheetType));
				int total   = sheetTypes.Length;
				int current = 0;

				foreach (ESheetType sheetType in sheetTypes)
				{
					SheetParserBase parser = CreateParser(sheetType);
					if (parser == null)
					{
						Debug.LogWarning($"[SpreadsheetImporter] '{sheetType}' 파서 없음 → 스킵");
						current++;
						continue;
					}

					mStatusMsg = $"({current + 1}/{total}) {sheetType} 파싱 중...";
					Repaint();
					await parser.RunAsync();
					current++;
				}

				mStatusMsg = $"완료 ({total}개 시트)";
			}
			catch (Exception e)
			{
				mStatusMsg = $"오류: {e.Message}";
				Debug.LogError($"[SpreadsheetImporter] {e}");
			}
			finally
			{
				bIsImporting = false;
				Repaint();
			}
		}

		private static SheetParserBase CreateParser(ESheetType sheetType)
		{
			string typeName = $"TrumpTile.Editor.{sheetType}Parser";
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type parserType = assembly.GetType(typeName);
				if (parserType != null)
				{
					return (SheetParserBase)Activator.CreateInstance(parserType);
				}
			}
			return null;
		}

		private static void DrawSectionLabel(string label)
		{
			GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
			GUILayout.Label(label, style);
			Rect rect   = GUILayoutUtility.GetLastRect();
			rect.y     += EditorGUIUtility.singleLineHeight;
			rect.height = 1F;
			EditorGUI.DrawRect(rect, new Color(0.5F, 0.5F, 0.5F, 0.5F));
			GUILayout.Space(4F);
		}
	}
}
