using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class RouletteContent : ContentBase
    {
        [System.Serializable]
        public class RouletteRewardConfig
        {
            [SerializeReference, SubclassSelector] public ProductReward Reward;
            public float Weight;
        }
        [Header("룰렛 지급 보상")]
        [SerializeField] private RouletteRewardConfig[] mRewardConfigArray;

        //룰렛 최대 사용 횟수 (일일)
        private const int MAX_COUNT = 4;
        //룰렛 무료 사용 횟수 (일일)
        private const int FREE_COUNT = 1;
        private const int REWARD_TYPE = 6;
        private int mCurrentCount = 0;
        private int mCurrentRewardIndex = 0;
        private bool mbIsFree = true;
        public bool IsFree => mbIsFree;
        public int Count => MAX_COUNT - mCurrentCount;
        public int MaxCount => MAX_COUNT;
        public int FreeCount => FREE_COUNT;
        public override void Initialize()
        {
            base.Initialize();

            mbIsFree = true;
            
            mCurrentCount = PlayerDataManager.Inst.RouletteCount;

            SetIsFree();

            if(mCurrentCount < MAX_COUNT)
            {
                mbHasNewthing = true;
            }
        }   
        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage > mLevelToUnlock)
            {
                SetUnlock();

                if(!PlayerDataManager.Inst.UserData.RouletteUnlock)
                {
                    PlayerDataManager.Inst.UnlockRoulette();
                    mbShowUnlockPopup = true;
                }
                else
                {
                    mbShowUnlockPopup = false;
                }
            }
        }
        public override void Refresh()
        {
            base.Refresh();

            SetIsFree();
            EventManager.Inst.ActiveEvent("RefreshRouletteData");
        }
        public int GetRewardIndex()
        {
            float totalWeight = 0f;
            foreach (RouletteRewardConfig config in mRewardConfigArray)
            {
                totalWeight += config.Weight;
            }

            float random = Random.Range(0f, totalWeight);
            float sum = 0f;

            for (int i = 0; i < mRewardConfigArray.Length; i++)
            {
                sum += mRewardConfigArray[i].Weight;
                if (random < sum)
                {
                    mCurrentRewardIndex = i;
                    return i;
                }
            }
            mCurrentRewardIndex = mRewardConfigArray.Length - 1;
            return mRewardConfigArray.Length - 1;
        }
        public ProductReward GetProductReward()
        {
            return mRewardConfigArray[mCurrentRewardIndex].Reward;
        }
        public bool CanProgress()
        {
            return mCurrentCount < MAX_COUNT;
        }
        public void RouletteRewardProgress()
        {
            if(mCurrentCount >= MAX_COUNT)
            {
                Debug.LogError($"[RouletteContent] 룰렛 이용 횟수가 최대치에 도달했습니다.");
                return;
            }
            if(!mbIsFree)
            {
                Debug.Log($"[RouletteContent] 보상형 광고 시청. 광고 종료 콜백 대기");
                //광고 재생
            }

            mRewardConfigArray[mCurrentRewardIndex].Reward.GrantReward();
            CoreContainer.RewardContainer.AddElement(mRewardConfigArray[mCurrentRewardIndex].Reward);
            mCurrentCount++;

            SetIsFree();

            if(mCurrentCount >= MAX_COUNT)
            {
                mbHasNewthing = false;
            }
        }
        private void SetIsFree()
        {
            if(mCurrentCount >= FREE_COUNT)
            {
                if(!PlayerDataManager.Inst.IsAdsRemoved)
                {
                    mbIsFree = false;
                }
                else
                {
                    mbIsFree = true;
                }
            }
        }
    }   
}
