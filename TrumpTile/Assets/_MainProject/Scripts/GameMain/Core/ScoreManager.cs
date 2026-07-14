using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{   
    public class StageScoreData
    {
        public float TimeLimit;
        public float Star3;
        public float Star2;
    }
    public class ScoreManager : MonoBehaviour
    {
        [Header("노멀 스테이지 테이블")]
        [SerializeField] private TBStageTableTemp mStageTable;
        [Header("데일리퍼즐 스테이지 테이블")]
        [SerializeField] private TBDailyPuzzleStageTable mDailyPuzzleStageTable;

        public StageScoreData GetStageScoreData(int index)
        {
            index--;

            StageScoreData data = new StageScoreData();
            TBStageDataTemp temp = mStageTable.GetStageData(index);
            if(temp == null)
            {
                data.TimeLimit = 100;
                data.Star3 = 60;
                data.Star2 = 30;

                return data;
            }
            data.TimeLimit = temp.TimerLimit;
            data.Star3 = temp.ScoreStar3;
            data.Star2 = temp.ScoreStar2;

            return data;
        }
        public StageScoreData GetDailyPuzzleStageScoreData(int index)
        {
            StageScoreData data = new StageScoreData();
            TBDailyPuzzleStageData temp = mDailyPuzzleStageTable.GetStageData(index);
            if(temp == null)
            {
                data.TimeLimit = 100;
                data.Star3 = 60;
                data.Star2 = 30;

                return data;
            }
            data.TimeLimit = temp.TimerLimit;
            data.Star3 = temp.ScoreStar3;
            data.Star2 = temp.ScoreStar2;

            return data;
        }
    }   
}
