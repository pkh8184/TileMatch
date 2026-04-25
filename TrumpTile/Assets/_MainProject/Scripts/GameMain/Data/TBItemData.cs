using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBItemData
	{
		public int    ItemId;
		public int    ItemNameId;
		public int    UnlockLevel;
		public int    ItemPrice;
		public int    ProvideCount;
		public int    UnlockDescriptionId;
		public int    ItemDescriptionId;
		public string ItemThumbnail;
	}

	[CreateAssetMenu(fileName = "TBItemTable", menuName = "TrumpTile/Data/TB Item Table")]
	public class TBItemTable : ScriptableObject
	{
		public TBItemData[] items;

		public TBItemData GetById(int itemId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBItemData item in items)
			{
				if (item.ItemId == itemId)
				{
					return item;
				}
			}
			return null;
		}
	}
}
