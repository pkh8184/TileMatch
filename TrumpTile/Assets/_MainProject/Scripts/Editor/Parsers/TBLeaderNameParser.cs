using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBLeaderNameParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBLeaderName;
		protected override string     SaveRelativePath => "TBLeaderName/TBLeaderNameTable.asset";

		[MenuItem("Tools/Parsers/TB_LeaderName")]
		public static void Parse()
		{
			new TBLeaderNameParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBLeaderNameParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBLeaderNameData> items = new List<TBLeaderNameData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBLeaderNameData item = new TBLeaderNameData();
				item.Index              = GetInt(cells, columnMap, "Index");
				item.Nickname          = GetString(cells, columnMap, "Nickname");
				item.Profile         = GetInt(cells, columnMap, "Profile");
				item.Frame           = GetInt(cells, columnMap, "Frame");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBLeaderNameData> items)
		{
			TBLeaderNameTable table = AssetDatabase.LoadAssetAtPath<TBLeaderNameTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBLeaderNameTable>();
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

			Debug.Log($"[TBLeaderNameParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
