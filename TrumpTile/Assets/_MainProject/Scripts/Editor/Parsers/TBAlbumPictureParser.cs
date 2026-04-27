using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBAlbumPictureParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBAlbumPicture;
		protected override string     SaveRelativePath => "TBAlbum/TBAlbumPictureTable.asset";

		[MenuItem("Tools/Parsers/TB_AlbumPicture")]
		public static void Parse()
		{
			new TBAlbumPictureParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBAlbumPictureParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBAlbumPictureData> pictures = new List<TBAlbumPictureData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBAlbumPictureData pic = new TBAlbumPictureData();
				pic.AlbumGroupId          = GetInt(cells, columnMap, "AlbumGroupId");
				pic.PictureId             = GetInt(cells, columnMap, "PictureId");
				pic.StageValue            = GetInt(cells, columnMap, "StageValue");
				pic.PictureNameId         = GetInt(cells, columnMap, "PictureNameId");
				pic.PictureDescriptionId  = GetInt(cells, columnMap, "PictureDescriptionId");
				pic.HammerRewardCount     = GetInt(cells, columnMap, "HamerRewardCount");
				pic.MagicStickRewardCount = GetInt(cells, columnMap, "MagicStickRewardCount");
				pic.MagicHatRewardCount   = GetInt(cells, columnMap, "MagicHatRewardCount");
				pic.BombRewardCount       = GetInt(cells, columnMap, "BombRewardCount");

				pictures.Add(pic);
			}

			SaveTable(pictures);
		}

		private void SaveTable(List<TBAlbumPictureData> pictures)
		{
			TBAlbumPictureTable table = AssetDatabase.LoadAssetAtPath<TBAlbumPictureTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBAlbumPictureTable>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.items = pictures.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBAlbumPictureParser] 저장 완료 → {SavePath} ({pictures.Count}개)");
		}
	}
}
