using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class DailyCheckContent : ContentBase
    {
        [Header("지급 보상")]
        [SerializeReference, SubclassSelector] private ProductReward[] mRewardArray;
        private int mStreakCount;
        private bool mbGotTodayReward;
        public override void Initialize()
        {
            base.Initialize();

            //유저 데이터에서 출석일수 읽어오기 (마지막 접속 일자와 현재 접속 일자 비교해야함)
            //유저 데이터에서 오늘자 출석 보상을 받았는지 읽어오기
            //받았다면 
            //mbHasNewthing = false;
        }
        public void DailyCheckRewardProgress()
        {
            if(mbGotTodayReward)
            {
                return;
            }
            mRewardArray[mStreakCount - 1].GrantReward();
        }

    }    
}

