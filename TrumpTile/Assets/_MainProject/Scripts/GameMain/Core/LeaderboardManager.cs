using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TrumpTile.FirebaseLibrary;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Core
{
    public class LeaderboardManager : Singleton_GameObject<LeaderboardManager>
    {
        private const int DEFAULT_COUNT = 100;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public async Task<LeaderboardResult> GetLeaderboardAsync(int n = DEFAULT_COUNT)
        {
            Dictionary<object, object> raw = await FirebaseFunctionsService.RequestGetLeaderboardAsync(n);
            if (raw == null)
            {
                Debug.LogError("[LeaderboardManager] getLeaderboard 요청 실패");
                return null;
            }
            return ParseResult(raw);
        }

        private LeaderboardResult ParseResult(Dictionary<object, object> raw)
        {
            LeaderboardResult result = new LeaderboardResult();
            result.topN = new List<LeaderboardEntryData>();

            if (raw.ContainsKey("topN") && raw["topN"] is List<object> topNList)
            {
                foreach (object item in topNList)
                {
                    if (item is Dictionary<object, object> entryDict)
                    {
                        result.topN.Add(ParseEntry(entryDict));
                    }
                }
            }

            if (raw.ContainsKey("myEntry") && raw["myEntry"] is Dictionary<object, object> myDict)
            {
                result.myEntry = ParseEntry(myDict);
            }

            return result;
        }

        private LeaderboardEntryData ParseEntry(Dictionary<object, object> dict)
        {
            LeaderboardEntryData entry = new LeaderboardEntryData();
            entry.rank = dict.ContainsKey("rank") ? Convert.ToInt32(dict["rank"]) : 0;
            entry.nickname = dict.ContainsKey("nickname") ? dict["nickname"].ToString() : "USER";
            entry.profileImageIndex = dict.ContainsKey("profileImageIndex") ? Convert.ToInt32(dict["profileImageIndex"]) : 0;
            entry.profileFrameIndex = dict.ContainsKey("profileFrameIndex") ? Convert.ToInt32(dict["profileFrameIndex"]) : 0;
            entry.currentStage = dict.ContainsKey("currentStage") ? Convert.ToInt32(dict["currentStage"]) : 1;
            return entry;
        }
    }
}
