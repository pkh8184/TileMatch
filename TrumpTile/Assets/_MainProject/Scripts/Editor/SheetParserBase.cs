using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public abstract class SheetParserBase
	{
		protected const string SODATA_BASE_PATH = "Assets/_MainProject/SODatas/";

		protected abstract ESheetType SheetType { get; }
		protected abstract string SaveRelativePath { get; }
		protected abstract void ParseAndSave(string[][] data);

		protected string SavePath => SODATA_BASE_PATH + SaveRelativePath;

		public async void Run()
		{
			await RunAsync();
		}

		public async Task RunAsync()
		{
			try
			{
				await Execute();
			}
			catch (Exception e)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogError($"[SheetParser] 오류: {e.Message}");
			}
		}

		private async Task Execute()
		{
			string url = EditorPrefs.GetString(SpreadsheetImporter.PREF_URL_KEY, "");
			if (string.IsNullOrEmpty(url))
			{
				EditorUtility.DisplayDialog(
					"오류",
					"URL이 설정되지 않았습니다.\nTools > Spreadsheet Importer에서 URL을 먼저 설정하세요.",
					"확인");
				return;
			}

			string sheetName = GetSheetName();

			string sheetUrl = $"{url}?sheet={sheetName}";

			EditorUtility.DisplayProgressBar($"Parsing {sheetName}", "데이터 가져오는 중...", 0.3F);
			string json = await FetchJsonAsync(sheetUrl);

			EditorUtility.DisplayProgressBar($"Parsing {sheetName}", "시트 추출 중...", 0.6F);
			string[][] data = ExtractSheet(json, sheetName);

			if (data == null)
			{
				EditorUtility.ClearProgressBar();
				Debug.LogError($"[SheetParser] '{sheetName}' 시트를 찾을 수 없습니다.");
				return;
			}

			EditorUtility.DisplayProgressBar($"Parsing {sheetName}", "SO 변환 중...", 0.9F);
			ParseAndSave(data);

			EditorUtility.ClearProgressBar();
			Debug.Log($"[SheetParser] '{sheetName}' 파싱 완료 ({data.Length - 1}행)");
		}

		private string GetSheetName()
		{
			FieldInfo field = typeof(ESheetType).GetField(SheetType.ToString());
			SheetNameAttribute attr = field?.GetCustomAttribute<SheetNameAttribute>();
			return attr?.Name ?? SheetType.ToString();
		}

		private static async Task<string> FetchJsonAsync(string url)
		{
			using (HttpClient client = new HttpClient())
			{
				client.Timeout = TimeSpan.FromSeconds(30);
				HttpResponseMessage response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			}
		}

		private static string[][] ExtractSheet(string json, string sheetName)
		{
			string searchKey = "\"" + sheetName + "\"";
			int keyIdx = json.IndexOf(searchKey, StringComparison.Ordinal);
			if (keyIdx < 0)
			{
				return null;
			}

			int colonIdx = json.IndexOf(':', keyIdx + searchKey.Length);
			if (colonIdx < 0)
			{
				return null;
			}

			int arrayStart = json.IndexOf('[', colonIdx);
			if (arrayStart < 0)
			{
				return null;
			}

			int arrayEnd = FindArrayEnd(json, arrayStart);
			if (arrayEnd < 0)
			{
				return null;
			}

			return ParseRows(json, arrayStart, arrayEnd);
		}

		private static int FindArrayEnd(string json, int startIdx)
		{
			int depth = 0;
			for (int i = startIdx; i < json.Length; i++)
			{
				if (json[i] == '"')
				{
					i++;
					while (i < json.Length && json[i] != '"')
					{
						if (json[i] == '\\')
						{
							i++;
						}
						i++;
					}
					continue;
				}

				if (json[i] == '[')
				{
					depth++;
				}
				else if (json[i] == ']')
				{
					depth--;
					if (depth == 0)
					{
						return i;
					}
				}
			}
			return -1;
		}

		private static string[][] ParseRows(string json, int start, int end)
		{
			List<string[]> rows = new List<string[]>();
			int i = start + 1;

			while (i < end)
			{
				SkipWhitespaceAndCommas(json, ref i);
				if (i >= end)
				{
					break;
				}

				if (json[i] == '[')
				{
					int rowEnd = FindArrayEnd(json, i);
					if (rowEnd < 0)
					{
						break;
					}

					rows.Add(ParseCells(json, i + 1, rowEnd));
					i = rowEnd + 1;
				}
				else
				{
					i++;
				}
			}

			return rows.ToArray();
		}

		private static string[] ParseCells(string json, int start, int end)
		{
			List<string> cells = new List<string>();
			int i = start;

			while (i < end)
			{
				SkipWhitespaceAndCommas(json, ref i);
				if (i >= end)
				{
					break;
				}

				cells.Add(ParseValue(json, ref i));
			}

			return cells.ToArray();
		}

		private static string ParseValue(string json, ref int i)
		{
			if (json[i] == '"')
			{
				i++;
				int valueStart = i;
				while (i < json.Length && json[i] != '"')
				{
					if (json[i] == '\\')
					{
						i++;
					}
					i++;
				}
				string value = json.Substring(valueStart, i - valueStart);
				if (i < json.Length)
				{
					i++;
				}
				return value;
			}
			else
			{
				int valueStart = i;
				while (i < json.Length && json[i] != ',' && json[i] != ']')
				{
					i++;
				}
				return json.Substring(valueStart, i - valueStart).Trim();
			}
		}

		private static void SkipWhitespaceAndCommas(string json, ref int i)
		{
			while (i < json.Length &&
				(json[i] == ' ' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t' || json[i] == ','))
			{
				i++;
			}
		}

		#region Parser Helpers

		protected static Dictionary<string, int> BuildColumnMap(string[] headers)
		{
			Dictionary<string, int> map = new Dictionary<string, int>();
			for (int i = 0; i < headers.Length; i++)
			{
				if (!string.IsNullOrEmpty(headers[i]))
				{
					map[headers[i]] = i;
				}
			}
			return map;
		}

		protected static bool IsEmptyRow(string[] cells)
		{
			foreach (string cell in cells)
			{
				if (!string.IsNullOrEmpty(cell))
				{
					return false;
				}
			}
			return true;
		}

		protected static int GetInt(string[] cells, Dictionary<string, int> map, string key)
		{
			if (!map.TryGetValue(key, out int idx) || idx >= cells.Length)
			{
				return 0;
			}
			return int.TryParse(cells[idx], out int val) ? val : 0;
		}

		protected static float GetFloat(string[] cells, Dictionary<string, int> map, string key)
		{
			if (!map.TryGetValue(key, out int idx) || idx >= cells.Length)
			{
				return 0F;
			}
			return float.TryParse(cells[idx], System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out float val) ? val : 0F;
		}

		protected static string GetString(string[] cells, Dictionary<string, int> map, string key)
		{
			if (!map.TryGetValue(key, out int idx) || idx >= cells.Length)
			{
				return "";
			}
			return cells[idx];
		}

		#endregion
	}
}
