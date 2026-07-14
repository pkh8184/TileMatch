using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBShopData
	{
		public int PackageNameId;
        public int SortingNum;
        public int RefreshTime;
        public int GoldCount;
        public int HammerCount;
        public int ClockCount;
        public int HatCount;
        public int BombCount;
	}

	[CreateAssetMenu(fileName = "TBShopTable", menuName = "TrumpTile/Data/TB Shop Table")]
	public class TBShopTable : ScriptableObject
	{
		public TBShopData[] items;

	}
}
