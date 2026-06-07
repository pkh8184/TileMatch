using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBAlbumData
	{
		public int    AlbumGroupId;
		public int    GroupNameId;
		public int    GoldRewardCount;
		public string Summary;
	}

	[CreateAssetMenu(fileName = "TBAlbumTable", menuName = "TrumpTile/Data/TB Album Table")]
	public class TBAlbumTable : ScriptableObject
	{
		public TBAlbumData[] items;

		public TBAlbumData GetById(int albumGroupId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBAlbumData item in items)
			{
				if (item.AlbumGroupId == albumGroupId)
				{
					return item;
				}
			}
			return null;
		}
	}
}
