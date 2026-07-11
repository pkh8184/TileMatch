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

        // 보상 단계 수는 요구 젬 배열 길이를 기준으로 한다.
        // (기존 하드코딩 상수 15가 실제 배열 길이와 어긋나 IndexOutOfRange를 유발했음)
        private int MaxIndex => mRequiredGemCountArray != null ? mRequiredGemCountArray.Length : 0;
        private int mCurrentIndex;
        private int mCurrentGemCount;
        private int mCapturedGemCount;
        private int mCapturedIndex;
        private int mPreviousGemCount;
        public int CapturedGemCount => mCapturedGemCount;
        public int CapturedIndex => mCapturedIndex;
        public int PreviousGemCount => mPreviousGemCount;
        public bool IsMaxIndex => mCurrentIndex >= MaxIndex;
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
            if(mCurrentIndex >= MaxIndex)
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
                if(mCurrentIndex >= MaxIndex)
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
            if(mCurrentIndex >= MaxIndex)
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
                if(mCurrentIndex >= MaxIndex)
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
            return GetRequiredGemAtSafe(mCapturedIndex);
        }
        public int GetCurrentRequiredGem()
        {
            return GetRequiredGemAtSafe(mCurrentIndex);
        }
        // 저장된 진행도(index)가 요구 젬 배열 길이를 넘어서도 크래시 없이 마지막 단계 값을 반환한다.
        private int GetRequiredGemAtSafe(int index)
        {
            if (mRequiredGemCountArray == null || mRequiredGemCountArray.Length == 0)
            {
                return 0;
            }
            int clamped = Mathf.Clamp(index, 0, mRequiredGemCountArray.Length - 1);
            return mRequiredGemCountArray[clamped];
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
