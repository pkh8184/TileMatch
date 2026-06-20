using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBStringMasterData
	{
		public int    Key;
		public string En;
		public string Ko;
		public string Ja;
		public string Zh;
		public string Vi;
		public string Hi;
		public string Ar;
	}

	[CreateAssetMenu(fileName = "TBStringMasterTable", menuName = "TrumpTile/Data/TB StringMaster Table")]
	public class TBStringMasterTable : ScriptableObject
	{
		public TBStringMasterData[] items;

		public TBStringMasterData GetByKey(int key)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBStringMasterData item in items)
			{
				if (item.Key == key)
				{
					return item;
				}
			}
			return null;
		}
	}
}
