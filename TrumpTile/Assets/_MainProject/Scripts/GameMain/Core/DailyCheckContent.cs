using System.Collections;
using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Messages;
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
        public override void Initialize()
        {
            base.Initialize();
            //유저 데이터에서 출석일수 읽어오기 (마지막 접속 일자와 현재 접속 일자 비교해야함)
            //유저 데이터에서 오늘자 출석 보상을 받았는지 읽어오기
            //받았다면 
            //mbHasNewthing = false;
            if(!PlayerDataManager.Inst.IsFirstLoginToday)
            {
                return;
            }

            mStreakCount = PlayerDataManager.Inst.StreakLoginCount - 1;
            mbHasNewthing = true;
        }
        public override void Refresh()
        {
            base.Refresh();

            if(PlayerDataManager.Inst.CurrentStage < mLevelToUnlock)
            {
                return;
            }
            mbIsUnlocked = true;
        }
        public void DailyCheckRewardProgress()
        {
            mRewardArray[mStreakCount].GrantReward();
            mbHasNewthing = false;

            EventManager.Inst.ActiveEvent("DailyCheckRewardConfirm");
        }
        public ProductReward GetTodayReward()
        {
            return mRewardArray[mStreakCount];
        }
        public List<ProductReward> GetPreviewRewardList()
        {
            List<ProductReward> previewList = new List<ProductReward>();
            int index = mStreakCount - 1;
            for(int i = 0; i < 3; i++)
            {
                if(index < 0)
                {
                    index = mRewardArray.Length - 1;
                }
                previewList.Add(mRewardArray[index]);
                
                index--;
            }
            return previewList;
        }
    }    
}

