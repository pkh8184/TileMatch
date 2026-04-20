using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBStageDataTemp
	{
		public int    StageLevel;
		public int    NextStageRefId;
		public int    StageDifficult;
		public float  TimerLimit;
		public int    ScoreStar3;
		public int    ScoreStar2;
		public int    RewardGoldCount;
		public int    TutorialType;
		public string BackgroundImageSrc;
		public string BgmSoundSrc;
		public int    Summary;
	}

	[CreateAssetMenu(fileName = "TBStageTableTemp", menuName = "TrumpTile/Data/Temp/TB Stage Table")]
	public class TBStageTableTemp : ScriptableObject
	{
		public TBStageDataTemp[] stages;
	}
}
