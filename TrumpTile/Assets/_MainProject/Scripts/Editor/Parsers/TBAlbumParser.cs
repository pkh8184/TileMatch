using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBAlbumParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBAlbum;
		protected override string     SaveRelativePath => "TBAlbum/TBAlbumTable.asset";

		[MenuItem("Tools/Parsers/TB_Album")]
		public static void Parse()
		{
			new TBAlbumParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBAlbumParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBAlbumData> items = new List<TBAlbumData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBAlbumData item = new TBAlbumData();
				item.AlbumGroupId    = GetInt(cells, columnMap, "AlbumGroupId");
				item.GroupNameId     = GetInt(cells, columnMap, "GroupNameId");
				item.GoldRewardCount = GetInt(cells, columnMap, "GoldRewardCount");
				item.Summary         = GetString(cells, columnMap, "Summary");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBAlbumData> items)
		{
			TBAlbumTable table = AssetDatabase.LoadAssetAtPath<TBAlbumTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBAlbumTable>();
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

			Debug.Log($"[TBAlbumParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
