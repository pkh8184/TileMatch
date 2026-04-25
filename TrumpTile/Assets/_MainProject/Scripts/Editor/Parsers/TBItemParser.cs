using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBItemParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBItem;
		protected override string     SaveRelativePath => "TBItem/TBItemTable.asset";

		[MenuItem("Tools/Parsers/TB_Item")]
		public static void Parse()
		{
			new TBItemParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBItemParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBItemData> items = new List<TBItemData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBItemData item = new TBItemData();
				item.ItemId              = GetInt(cells, columnMap, "ItemId");
				item.ItemNameId          = GetInt(cells, columnMap, "ItemNameId");
				item.UnlockLevel         = GetInt(cells, columnMap, "UnlockLevel");
				item.ItemPrice           = GetInt(cells, columnMap, "ItemPrice");
				item.ProvideCount        = GetInt(cells, columnMap, "ProvideCount");
				item.UnlockDescriptionId = GetInt(cells, columnMap, "UnlockDescriptionId");
				item.ItemDescriptionId   = GetInt(cells, columnMap, "ItemDescriptionId");
				item.ItemThumbnail       = GetString(cells, columnMap, "ItemThumbnail");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBItemData> items)
		{
			TBItemTable table = AssetDatabase.LoadAssetAtPath<TBItemTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBItemTable>();
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

			Debug.Log($"[TBItemParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
