using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBPictureCollectParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBPictureCollect;
		protected override string     SaveRelativePath => "TBPictureCollect/TBPictureCollectTable.asset";

		[MenuItem("Tools/Parsers/TB_PictureCollect")]
		public static void RunFromMenu() => new TBPictureCollectParser().Run();

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				return;
			}

			Dictionary<string, int> map = BuildColumnMap(data[0]);
			List<TBPictureCollectData> list = new List<TBPictureCollectData>();

			for (int i = 1; i < data.Length; i++)
			{
				string[] cells = data[i];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBPictureCollectData item = new TBPictureCollectData
				{
					PictureId            = GetInt(cells, map, "PictureId"),
					StageValue           = GetInt(cells, map, "StageValue"),
					PictureNameId        = GetInt(cells, map, "PictureNameId"),
					PictureDescriptionId = GetInt(cells, map, "PictureDesciptionId"), // 시트 오타
					GoldRewardCount      = GetInt(cells, map, "GoldRewardCount"),
					HammerRewardCount    = GetInt(cells, map, "HamerRewardCount"),     // 시트 오타
					ClockRewardCount     = GetInt(cells, map, "ClockRewardCount"),
					HatRewardCount       = GetInt(cells, map, "HatRewardCount"),
					BombRewardCount      = GetInt(cells, map, "BombRewardCount"),
				};
				list.Add(item);
			}

			string dir = Path.GetDirectoryName(SavePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			TBPictureCollectTable table = AssetDatabase.LoadAssetAtPath<TBPictureCollectTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBPictureCollectTable>();
				AssetDatabase.CreateAsset(table, SavePath);
			}
			table.items = list.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();
		}
	}
}
