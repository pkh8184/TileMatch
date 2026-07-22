using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBLeaderNameData
	{
		public int Index;
        public string Nickname;
        public int Profile;
        public int Frame;
		public int Stage;
	}

	[CreateAssetMenu(fileName = "TBLeaderNameTable", menuName = "TrumpTile/Data/TB LeaderName Table")]
	public class TBLeaderNameTable : ScriptableObject
	{
		public TBLeaderNameData[] items;

        public int Total => items.Length;

		public TBLeaderNameData GetByIndex(int index)
		{
			if (items == null)
			{
				return null;
			}
            if(index >= Total)
            {
                return null;
            }
			return items[index];
		}
	}
}
