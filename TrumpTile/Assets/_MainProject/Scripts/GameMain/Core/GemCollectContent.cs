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
            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
        }
        public override void Refresh()
        {
            base.Refresh();

            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
            Debug.Log($"[GemCollectContent] 현재 젬 개수 : {mCurrentGemCount}");
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
            EventManager.Inst.ActiveEvent("PlayMiniRewardAnim", new MiniRewardPayload{Infos = infos, Type = EMiniRewardAnimType.Custom, target = new Vector2(235, 79), parentName = "Contents_GemCollect"});
            
            mCurrentGemCount -= mRequiredGemCountArray[mCurrentIndex];
            PlayerDataManager.Inst.SetGemCount(mCurrentGemCount);

            mCurrentIndex++;
            PlayerDataManager.Inst.SetGemIndex(mCurrentIndex);
            if(mCurrentIndex >= MAX_INDEX)
            {
                PlayerDataManager.Inst.UnActiveGemCollection();
            }
        }
        public int GetCurrentGem()
        {
            return mCurrentGemCount;
        }
        public int GetCurrentRequiredGem()
        {
            return mRequiredGemCountArray[mCurrentIndex];
        }
        public int GetNextRequiredGem(int step)
        {
            if(mCurrentIndex + step >= MAX_INDEX)
            {
                return 0;
            }
            return mRequiredGemCountArray[mCurrentIndex + step];
        }
        public int GetCurrentIndex()
        {
            return mCurrentIndex;
        }
        public GemCollectionReward[] GetRewards()
        {
            return mRewardArray;
        }
        public bool IsProgressEnd()
        {
            return mCurrentIndex >= MAX_INDEX;
        }
    }   
}
