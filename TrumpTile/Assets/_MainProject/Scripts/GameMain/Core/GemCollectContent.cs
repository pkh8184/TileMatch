using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
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

        public override void Initialize()
        {
            base.Initialize();

            if(!PlayerDataManager.Inst.IsGemCollectionActive)
            {
                //비활성화 시간 - 현재 시간 > 쿨타임 이면 활성화
                //첫 활성화인 경우 비활성화 시간 == null 검사 후 활성화
            }

            mbIsActive = true;

            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
        }
        public override void Refresh()
        {
            base.Refresh();

            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
        }
        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage > mLevelToUnlock)
            {
                SetUnlock();

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
        }
        public void RewardProgress()
        {
            if(mCurrentIndex >= MAX_INDEX)
            {
                return;
            }
            if(mCurrentGemCount < mRequiredGemCountArray[mCurrentIndex])
            {
                return;
            }
            List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
            foreach(var item in mRewardArray[mCurrentIndex].RewardArray)
            {
                item.GrantReward();

                RewardDisplayInfo info = item.GetRewardDisplayInfo();
                infos.Add(info);
                CoreContainer.RewardContainer.AddReward(info);
            }
            EventManager.Inst.ActiveEvent("PlayMiniRewardAnim", new MiniRewardPayload{Infos = infos, Type = EMiniRewardAnimType.ViewContent});
            
            mCurrentGemCount -= mRequiredGemCountArray[mCurrentIndex];
            PlayerDataManager.Inst.SetGemCount(mCurrentGemCount);

            mCurrentIndex++;
            PlayerDataManager.Inst.SetGemIndex(mCurrentIndex);
        }
        public int GetCurrentGem()
        {
            return mCurrentGemCount;
        }
        public int GetCurrentRequiredGem()
        {
            return mRequiredGemCountArray[mCurrentIndex];
        }
        public int GetCurrentIndex()
        {
            return mCurrentIndex;
        }
        public ProductReward[] GetCurrentRewards()
        {
            return mRewardArray[mCurrentIndex].RewardArray;
        }
        public GemCollectionReward[] GetRewards()
        {
            return mRewardArray;
        }
    }   
}
