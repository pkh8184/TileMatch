using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBDailyPuzzleStageParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBDailyPuzzleStage;
		protected override string     SaveRelativePath => "TBDailyPuzzleStage/TBDailyPuzzleStageTable.asset";

		[MenuItem("Tools/Parsers/TB_DailyPuzzleStage")]
		public static void RunFromMenu() => new TBDailyPuzzleStageParser().Run();

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				return;
			}

			Dictionary<string, int> map = BuildColumnMap(data[0]);
			List<TBDailyPuzzleStageData> list = new List<TBDailyPuzzleStageData>();

			for (int i = 1; i < data.Length; i++)
			{
				string[] cells = data[i];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBDailyPuzzleStageData item = new TBDailyPuzzleStageData
				{
					Stage            = GetInt(cells, map, "Stage"),
					TimerLimit           = GetInt(cells, map, "TimerLimit"),
					ScoreStar3        = GetInt(cells, map, "ScoreStar3"),
					ScoreStar2 = GetInt(cells, map, "ScoreStar2")
				};
				list.Add(item);
			}

			string dir = Path.GetDirectoryName(SavePath);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			TBDailyPuzzleStageTable table = AssetDatabase.LoadAssetAtPath<TBDailyPuzzleStageTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBDailyPuzzleStageTable>();
				AssetDatabase.CreateAsset(table, SavePath);
			}
			table.items = list.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();
		}
	}
}
