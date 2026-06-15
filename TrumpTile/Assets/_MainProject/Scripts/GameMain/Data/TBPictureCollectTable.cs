using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBPictureCollectData
	{
		public int PictureId;
		public int StageValue;
		public int PictureNameId;
		public int PictureDescriptionId;
		public int GoldRewardCount;
		public int HammerRewardCount;
		public int ClockRewardCount;
		public int HatRewardCount;
		public int BombRewardCount;
	}

	[CreateAssetMenu(fileName = "TBPictureCollectTable", menuName = "TrumpTile/Data/TB PictureCollect Table")]
	public class TBPictureCollectTable : ScriptableObject
	{
		public TBPictureCollectData[] items;

		public TBPictureCollectData GetById(int pictureId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBPictureCollectData item in items)
			{
				if (item.PictureId == pictureId)
				{
					return item;
				}
			}
			return null;
		}

		public TBPictureCollectData[] GetAll()
		{
			return items ?? new TBPictureCollectData[0];
		}
	}
}
