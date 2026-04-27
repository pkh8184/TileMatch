using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBAlbumGroupParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBAlbumGroup;
		protected override string     SaveRelativePath => "TBAlbum/TBAlbumGroupTable.asset";

		[MenuItem("Tools/Parsers/TB_AlbumGroup")]
		public static void Parse()
		{
			new TBAlbumGroupParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBAlbumGroupParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBAlbumGroupData> groups = new List<TBAlbumGroupData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBAlbumGroupData group = new TBAlbumGroupData();
				group.AlbumGroupId = GetInt(cells, columnMap, "AlbumGroupId");
				group.GroupNameId  = GetInt(cells, columnMap, "GroupNameId");

				groups.Add(group);
			}

			SaveTable(groups);
		}

		private void SaveTable(List<TBAlbumGroupData> groups)
		{
			TBAlbumGroupTable table = AssetDatabase.LoadAssetAtPath<TBAlbumGroupTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBAlbumGroupTable>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.items = groups.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBAlbumGroupParser] 저장 완료 → {SavePath} ({groups.Count}개)");
		}
	}
}
