using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBAlbumPictureData
	{
		public int AlbumGroupId;
		public int PictureId;
		public int StageValue;
		public int PictureNameId;
		public int PictureDescriptionId;
		public int HammerRewardCount;
		public int MagicStickRewardCount;
		public int MagicHatRewardCount;
		public int BombRewardCount;
	}

	[CreateAssetMenu(fileName = "TBAlbumPictureTable", menuName = "TrumpTile/Data/TB AlbumPicture Table")]
	public class TBAlbumPictureTable : ScriptableObject
	{
		public TBAlbumPictureData[] items;

		public TBAlbumPictureData GetById(int pictureId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBAlbumPictureData pic in items)
			{
				if (pic.PictureId == pictureId)
				{
					return pic;
				}
			}
			return null;
		}

		public TBAlbumPictureData[] GetByGroupId(int albumGroupId)
		{
			if (items == null)
			{
				return new TBAlbumPictureData[0];
			}
			List<TBAlbumPictureData> result = new List<TBAlbumPictureData>();
			foreach (TBAlbumPictureData pic in items)
			{
				if (pic.AlbumGroupId == albumGroupId)
				{
					result.Add(pic);
				}
			}
			return result.ToArray();
		}

		public TBAlbumPictureData GetByStageValue(int stageValue)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBAlbumPictureData pic in items)
			{
				if (pic.StageValue == stageValue)
				{
					return pic;
				}
			}
			return null;
		}
	}
}
