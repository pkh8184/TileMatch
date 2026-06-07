using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBPictureData
	{
		public int    AlbumGroupId;
		public int    PictureId;
		public int    StageValue;
		public int    PictureNameId;
		public int    PictureDescriptionId;
		public int    HammerRewardCount;
		public int    MagicStickRewardCount;
		public int    MagicHatRewardCount;
		public int    BombRewardCount;
		public string MainThumbnailSrc;
		public string PictureThumbnailSrc;
		public string PictureBackgroundSrc;
		public string Summary;
	}

	[CreateAssetMenu(fileName = "TBPictureTable", menuName = "TrumpTile/Data/TB Picture Table")]
	public class TBPictureTable : ScriptableObject
	{
		public TBPictureData[] items;

		public TBPictureData GetById(int pictureId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBPictureData item in items)
			{
				if (item.PictureId == pictureId)
				{
					return item;
				}
			}
			return null;
		}

		public TBPictureData[] GetByAlbumGroup(int albumGroupId)
		{
			if (items == null)
			{
				return new TBPictureData[0];
			}
			return System.Array.FindAll(items, p => p.AlbumGroupId == albumGroupId);
		}
	}
}
