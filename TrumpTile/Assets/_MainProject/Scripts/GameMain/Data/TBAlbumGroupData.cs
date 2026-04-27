using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBAlbumGroupData
	{
		public int AlbumGroupId;
		public int GroupNameId;
	}

	[CreateAssetMenu(fileName = "TBAlbumGroupTable", menuName = "TrumpTile/Data/TB AlbumGroup Table")]
	public class TBAlbumGroupTable : ScriptableObject
	{
		public TBAlbumGroupData[] items;

		public TBAlbumGroupData GetById(int albumGroupId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBAlbumGroupData group in items)
			{
				if (group.AlbumGroupId == albumGroupId)
				{
					return group;
				}
			}
			return null;
		}
	}
}
