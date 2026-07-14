using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBShopParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBShop;
		protected override string     SaveRelativePath => "TBShop/TBShopTable.asset";

		[MenuItem("Tools/Parsers/TB_Shop")]
		public static void RunFromMenu() => new TBShopParser().Run();

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				return;
			}

			Dictionary<string, int> map = BuildColumnMap(data[0]);
			List<TBShopData> list = new List<TBShopData>();

			for (int i = 1; i < data.Length; i++)
			{
				string[] cells = data[i];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBShopData item = new TBShopData
				{
					PackageNameId            = GetInt(cells, map, "PackageNameId"),
					SortingNum           = GetInt(cells, map, "SortingNum"),
					RefreshTime        = GetInt(cells, map, "RefreshTime"),
					GoldCount = GetInt(cells, map, "GoldCount"),
					HammerCount = GetInt(cells, map, "HammerCount"),
					ClockCount = GetInt(cells, map, "ClockCount"),
					HatCount = GetInt(cells, map, "HatCount"),
					BombCount = GetInt(cells, map, "BombCount")
				};
				list.Add(item);
			}

			string dir = Path.GetDirectoryName(SavePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			TBShopTable table = AssetDatabase.LoadAssetAtPath<TBShopTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBShopTable>();
				AssetDatabase.CreateAsset(table, SavePath);
			}
			table.items = list.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();
		}
	}
}
