using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBStringMasterParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBStringMaster;
		protected override string     SaveRelativePath => "TBStringMaster/TBStringMasterTable.asset";

		[MenuItem("Tools/Parsers/TB_StringMaster")]
		public static void Parse()
		{
			new TBStringMasterParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBStringMasterParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBStringMasterData> items = new List<TBStringMasterData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBStringMasterData item = new TBStringMasterData();
				item.Key = GetInt(cells, columnMap, "Key");
				item.En  = GetString(cells, columnMap, "En");
				item.Ko  = GetString(cells, columnMap, "Ko");
				item.Ja  = GetString(cells, columnMap, "Ja");
				item.Zh  = GetString(cells, columnMap, "Zh");
				item.Vi  = GetString(cells, columnMap, "Vi");
				item.Hi  = GetString(cells, columnMap, "Hi");
				item.Ar  = GetString(cells, columnMap, "Ar");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBStringMasterData> items)
		{
			TBStringMasterTable table = AssetDatabase.LoadAssetAtPath<TBStringMasterTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBStringMasterTable>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.items = items.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBStringMasterParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
