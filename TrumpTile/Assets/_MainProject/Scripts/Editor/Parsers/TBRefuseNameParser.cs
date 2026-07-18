using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBRefuseNameParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBRefuseName;
		protected override string     SaveRelativePath => "TBRefuseName/TBRefuseNameTable.asset";

		[MenuItem("Tools/Parsers/TB_RefuseName")]
		public static void RunFromMenu() => new TBRefuseNameParser().Run();

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				return;
			}

			Dictionary<string, int> map = BuildColumnMap(data[0]);
			List<TBRefuseNameData> list = new List<TBRefuseNameData>();

			for (int i = 1; i < data.Length; i++)
			{
				string[] cells = data[i];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBRefuseNameData item = new TBRefuseNameData
				{
					Tag            = GetString(cells, map, "Tag"),
					
				};
				list.Add(item);
			}

			string dir = Path.GetDirectoryName(SavePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			TBRefuseNameTable table = AssetDatabase.LoadAssetAtPath<TBRefuseNameTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBRefuseNameTable>();
				AssetDatabase.CreateAsset(table, SavePath);
			}
			table.items = list.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();
		}
	}
}
