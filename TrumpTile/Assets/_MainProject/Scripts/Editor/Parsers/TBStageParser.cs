using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBStageParser : SheetParserBase
	{
		protected override ESheetType SheetType       => ESheetType.TBStage;
		protected override string     SaveRelativePath => "Temp/TBStageTableTemp.asset";

		[MenuItem("Tools/Parsers/TB_Stage")]
		public static void Parse()
		{
			new TBStageParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBStageParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBStageDataTemp> stages = new List<TBStageDataTemp>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBStageDataTemp stage = new TBStageDataTemp();
				stage.StageLevel         = GetInt(cells, columnMap, "StageLevel");
				stage.NextStageRefId     = GetInt(cells, columnMap, "NextStageRefId");
				stage.StageDifficult     = GetInt(cells, columnMap, "StageDifficult");
				stage.TimerLimit         = GetFloat(cells, columnMap, "TimerLimit");
				stage.ScoreStar3         = GetInt(cells, columnMap, "ScoreStar3");
				stage.ScoreStar2         = GetInt(cells, columnMap, "ScoreStar2");
				stage.RewardGoldCount    = GetInt(cells, columnMap, "RewardGoldCount");
				stage.TutorialType       = GetInt(cells, columnMap, "TutorialType");
				stage.BackgroundImageSrc = GetString(cells, columnMap, "BackgroundImageSrc");
				stage.BgmSoundSrc        = GetString(cells, columnMap, "BgmSoundSrc");
				stage.Summary            = GetInt(cells, columnMap, "Summary");

				stages.Add(stage);
			}

			SaveTable(stages);
		}

		private void SaveTable(List<TBStageDataTemp> stages)
		{
			TBStageTableTemp table = AssetDatabase.LoadAssetAtPath<TBStageTableTemp>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBStageTableTemp>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.stages = stages.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBStageParser] 저장 완료 → {SavePath} ({stages.Count}개)");
		}
	}
}
