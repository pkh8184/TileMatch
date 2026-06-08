using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBPictureParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBPicture;
		protected override string     SaveRelativePath => "TBPicture/TBPictureTable.asset";

		[MenuItem("Tools/Parsers/TB_Picture")]
		public static void Parse()
		{
			new TBPictureParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBPictureParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBPictureData> items = new List<TBPictureData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBPictureData item = new TBPictureData();
				item.AlbumGroupId          = GetInt(cells, columnMap, "AlbumGroupId");
				item.PictureId             = GetInt(cells, columnMap, "PictureId");
				item.StageValue            = GetInt(cells, columnMap, "StageValue");
				item.PictureNameId         = GetInt(cells, columnMap, "PictureNameId");
				item.PictureDescriptionId  = GetInt(cells, columnMap, "PictureDescriptionId");
				item.HammerRewardCount     = GetInt(cells, columnMap, "HamerRewardCount");
				item.MagicStickRewardCount = GetInt(cells, columnMap, "MagicStickRewardCount");
				item.MagicHatRewardCount   = GetInt(cells, columnMap, "MagicHatRewardCount");
				item.BombRewardCount       = GetInt(cells, columnMap, "BombRewardCount");
				item.MainThumbnailSrc      = GetString(cells, columnMap, "MainThumbnailSrc");
				item.PictureThumbnailSrc   = GetString(cells, columnMap, "PictureThumbnailSrc");
				item.PictureBackgroundSrc  = GetString(cells, columnMap, "PictureBackgroundSrc");
				item.Summary               = GetString(cells, columnMap, "Summary");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBPictureData> items)
		{
			TBPictureTable table = AssetDatabase.LoadAssetAtPath<TBPictureTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBPictureTable>();
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

			Debug.Log($"[TBPictureParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
