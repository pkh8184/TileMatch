using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBRefuseNameData
	{
		public string Tag;
	}

	[CreateAssetMenu(fileName = "TBRefuseNameTable", menuName = "TrumpTile/Data/TB RefuseName Table")]
	public class TBRefuseNameTable : ScriptableObject
	{
		public TBRefuseNameData[] items;

		public bool CanNaming(string name)
		{
            foreach(var item in items)
            {
                if(name.Contains(item.Tag, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
			return true;
		}
	}
}
