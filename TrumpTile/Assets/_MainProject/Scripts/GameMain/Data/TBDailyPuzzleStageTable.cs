using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBDailyPuzzleStageData
	{
		public int Stage;
		public int TimerLimit;
        public int ScoreStar3;
        public int ScoreStar2;
	}

	[CreateAssetMenu(fileName = "TBDailyPuzzleStageTable", menuName = "TrumpTile/Data/TB DailyPuzzleStage Table")]
	public class TBDailyPuzzleStageTable : ScriptableObject
	{
		public TBDailyPuzzleStageData[] items;
		
		public TBDailyPuzzleStageData GetStageData(int index)
		{
			if(index < 0 || index >= items.Length) return null;
			return items[index];
		}
	}
}
