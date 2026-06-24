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

        public override void Initialize()
        {
            base.Initialize();

            SetDailyCheckData();
        }
        public override void Refresh()
        {
            base.Refresh();

            SetDailyCheckData();
        }
        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();

                if(!PlayerDataManager.Inst.UserData.DailyCheckUnlock)
                {
                    PlayerDataManager.Inst.UnlockDailyCheck();
                    mbShowUnlockPopup = true;
                }
                else
                {
                    mbShowUnlockPopup = false;
                }
            }
        }
        public void DailyCheckRewardProgress()
        {
            mRewardArray[mStreakCount].GrantReward();
            mbHasNewthing = false;
            PlayerDataManager.Inst.SetDailyCheckDone();
            CoreContainer.RewardContainer.AddReward(mRewardArray[mStreakCount].GetRewardDisplayInfo());
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
        private void SetDailyCheckData()
        {
            if(!PlayerDataManager.Inst.CanActiveDailyCheck())
            {
                mbHasNewthing = false;
                return;
            }

            mStreakCount = PlayerDataManager.Inst.StreakLoginCount - 1;
            mStreakCount %= mRewardArray.Length;
            mbHasNewthing = true;
        }
    }    
}

