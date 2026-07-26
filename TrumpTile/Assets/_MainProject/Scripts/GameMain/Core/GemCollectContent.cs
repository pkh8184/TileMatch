using System;
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

            //쿨타임/제한시간 기준으로 활성 상태 재평가 (지났으면 재활성화 + 진행도 초기화)
            EvaluateActiveState();

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

            EvaluateActiveState();

            mCurrentIndex = PlayerDataManager.Inst.GemCollectionIndex;
            mCurrentGemCount = PlayerDataManager.Inst.GemCollectionCount;
            mPreviousGemCount = mCurrentGemCount - CoreContainer.RewardContainer.Gem;
            mCapturedGemCount = mCurrentGemCount;
            mCapturedIndex = mCurrentIndex;

            RewardProgressForRefresh();
        }

        protected override bool GetActiveFlag() => PlayerDataManager.Inst.IsGemCollectionActive;
        protected override DateTime GetActiveDate() => PlayerDataManager.Inst.GemCollectionActiveDate;
        protected override DateTime GetUnActiveDate() => PlayerDataManager.Inst.GemCollectionUnActiveDate;
        protected override void OnActivate() => PlayerDataManager.Inst.ActiveGemCollection();
        protected override void OnDeactivate() => PlayerDataManager.Inst.UnActiveGemCollection();

        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
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
            //단계 완료 여부와 무관하게 항상 새 리스트로 초기화한다.
            //(단계 미완료로 early-return될 때 이전 수집의 payload가 남아, 이전 단계 프로그레스가 잘못 재생되는 것 방지)
            mPlayloadList = new List<GemCollectAnimPayload>();

            if(mCurrentIndex >= MAX_INDEX)
            {
                return;
            }
            if(mCurrentGemCount < mRequiredGemCountArray[mCurrentIndex])
            {
                return;
            }

            while(mCurrentGemCount >= mRequiredGemCountArray[mCurrentIndex])
            {
                List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
                foreach(var item in mRewardArray[mCurrentIndex].RewardArray)
                {
                    item.GrantReward();

                    RewardDisplayInfo info = item.GetRewardDisplayInfo();
                    infos.Add(info);

                    //골드는 PlayerData에 즉시 반영되지만 실제 애니(젬 게이지 후 골드 카운트업)는 나중이라,
                    //그 사이 메인 골드 표기가 미리 최종값으로 보이지 않도록 대기 골드로 등록한다.
                    if(info != null && info.Type == ERewardType.Gold)
                    {
                        CoreContainer.RewardContainer.AddPendingAnimGold(info.Amount);
                    }
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
