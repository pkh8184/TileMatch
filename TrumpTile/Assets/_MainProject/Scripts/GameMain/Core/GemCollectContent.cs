using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class GemCollectAnimPayload
    {
        public List<RewardDisplayInfo> RewardDisplayInfoList;
        public int CapturedRequiredGemCount;
        public int CapturedCurrentIndex;
    }
    [System.Serializable]
    public class GemCollectionReward
    {
        [SerializeReference, SubclassSelector] public ProductReward[] RewardArray;
    }
    [System.Serializable]
    public class GemCollectContent : TemporaryContent
    {
        [Header("보상 목록")]
        [SerializeField] private GemCollectionReward[] mRewardArray;
        [Header("보상 단계별 요구 젬 개수")]
        [SerializeField] private int[] mRequiredGemCountArray;

        private const int MAX_INDEX = 15;
        private int mCurrentIndex;
        private int mCurrentGemCount;
        private int mCapturedGemCount;
        private int mCapturedIndex;
        private int mPreviousGemCount;
        public int CapturedGemCount => mCapturedGemCount;
        public int CapturedIndex => mCapturedIndex;
        public int PreviousGemCount => mPreviousGemCount;
        public bool IsMaxIndex => mCurrentIndex >= MAX_INDEX;
        private List<GemCollectAnimPayload> mPlayloadList;
        public override void Initialize()
        {
            base.Initialize();

            mbIsActive = PlayerDataManager.Inst.IsGemCollectionActive;
           
            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
            mPreviousGemCount = mCurrentGemCount - CoreContainer.RewardContainer.Gem;
            mCapturedGemCount = mCurrentGemCount;
            mCapturedIndex = mCurrentIndex;

            RewardProgressForInit();
        }
        public override void Refresh()
        {
            base.Refresh();

            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
            mPreviousGemCount = mCurrentGemCount - CoreContainer.RewardContainer.Gem;
            mCapturedGemCount = mCurrentGemCount;
            mCapturedIndex = mCurrentIndex;

            RewardProgressForRefresh();
        }
        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();
                mbIsActive = true;
                
                if(!PlayerDataManager.Inst.UserData.GemCollectionUnlock)
                {
                    PlayerDataManager.Inst.UnlockGemCollection();
                    mbShowUnlockPopup = true;
                }
                else
                {
                    mbShowUnlockPopup = false;
                }
            }
            else
            {
                SetLock();
            }
        }
        private void RewardProgressForInit()
        {
            if(mCurrentIndex >= MAX_INDEX)
            {
                return;
            }
            if(mCurrentGemCount < mRequiredGemCountArray[mCurrentIndex])
            {
                return;
            }

            mPlayloadList = null;

            while(mCurrentGemCount >= mRequiredGemCountArray[mCurrentIndex])
            {
                foreach(var item in mRewardArray[mCurrentIndex].RewardArray)
                {
                    item.GrantReward();
                }
                mCurrentGemCount -= mRequiredGemCountArray[mCurrentIndex++];
                if(mCurrentIndex >= MAX_INDEX)
                {
                    Debug.Log("모든 보상 획득");
                    PlayerDataManager.Inst.UnActiveGemCollection();
                    return;
                }
            }
            PlayerDataManager.Inst.SetGemCount(mCurrentGemCount);
            PlayerDataManager.Inst.SetGemIndex(mCurrentIndex);        
        }
        private void RewardProgressForRefresh()
        {
            if(mCurrentIndex >= MAX_INDEX)
            {
                return;
            }
            if(mCurrentGemCount < mRequiredGemCountArray[mCurrentIndex])
            {
                return;
            }
            mPlayloadList = new List<GemCollectAnimPayload>();

            while(mCurrentGemCount >= mRequiredGemCountArray[mCurrentIndex])
            {
                List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
                foreach(var item in mRewardArray[mCurrentIndex].RewardArray)
                {
                    item.GrantReward();

                    RewardDisplayInfo info = item.GetRewardDisplayInfo();
                    infos.Add(info);
                }

                mPlayloadList.Add(new GemCollectAnimPayload{RewardDisplayInfoList = infos, CapturedRequiredGemCount = mRequiredGemCountArray[mCurrentIndex], CapturedCurrentIndex = mCurrentIndex});
               
                mCurrentGemCount -= mRequiredGemCountArray[mCurrentIndex++];
                if(mCurrentIndex >= MAX_INDEX)
                {
                    Debug.Log("모든 보상 획득");
                    PlayerDataManager.Inst.UnActiveGemCollection();
                    return;
                }
            }
            PlayerDataManager.Inst.SetGemCount(mCurrentGemCount);
            PlayerDataManager.Inst.SetGemIndex(mCurrentIndex);        
        }
        public int GetCurrentGem()
        {
            return mCurrentGemCount;
        }
        public int GetCapturedRequiredGemCount()
        {
            return mRequiredGemCountArray[mCapturedIndex];
        }
        public int GetCurrentRequiredGem()
        {
            return mRequiredGemCountArray[mCurrentIndex];
        }
        public int GetCurrentIndex()
        {
            return mCurrentIndex;
        }
        public GemCollectionReward[] GetRewards()
        {
            return mRewardArray;
        }
        public List<GemCollectAnimPayload> GetAnimPayload()
        {
            return mPlayloadList;
        }
    }   
}
