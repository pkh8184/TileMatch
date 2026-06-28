using System;
using System.Collections.Generic;

namespace TrumpTile.GameMain.Data
{
    [Serializable]
    public class LeaderboardEntryData
    {
        public int rank;
        public string nickname;
        public int profileImageIndex;
        public int profileFrameIndex;
        public int currentStage;
    }

    [Serializable]
    public class LeaderboardResult
    {
        public List<LeaderboardEntryData> topN;
        public LeaderboardEntryData myEntry;
    }
}
