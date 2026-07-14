using System;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class PiggyBankContent : TemporaryContent
    {
        [Header("보상 목록")]
        [SerializeReference, SubclassSelector] private ProductReward mDefaultReward;
        [SerializeReference, SubclassSelector] private ProductReward[] mRewardArray;

        [Header("스테이지 클리어 요구 횟수")]
        [SerializeField] private int mRequiredStageClearCount;
        private int mCurrentStageClearCount;
        private bool mbCanConfirm;
        public bool CanConfirm => mbCanConfirm;  
        public bool CanGetRewardAfterEndActive => !mbIsActive && mbCanConfirm;
        private bool mbIsFull;
        public bool IsFull => mbIsFull;
        public override void Initialize()
        {
            base.Initialize();

            //쿨타임/제한시간 기준으로 활성 상태 재평가 (지났으면 재활성화 + 진행도 초기화)
            EvaluateActiveState();

            SetPiggyBankData();
        }
        public override void Refresh()
        {
            base.Refresh();

            EvaluateActiveState();

            SetPiggyBankData();
        }

        protected override bool GetActiveFlag() => PlayerDataManager.Inst.IsPiggyBankActive;
        protected override DateTime GetActiveDate() => PlayerDataManager.Inst.PiggyBankActiveDate;
        protected override DateTime GetUnActiveDate() => PlayerDataManager.Inst.PiggyBankUnActiveDate;
        protected override void OnActivate() => PlayerDataManager.Inst.StartPiggyBankContent();
        protected override void OnDeactivate() => PlayerDataManager.Inst.EndPiggyBankContent();

        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();
                if(!PlayerDataManager.Inst.UserData.PiggyBankUnlock)
                {
                    PlayerDataManager.Inst.UnlockPiggyBank();
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
        public int GetCurrentStageCount()
        {
            return mCurrentStageClearCount;
        }
        public int GetMaxCount()
        {
            return mRequiredStageClearCount;
        }
        public int GetCurrentStackedGold()
        {
            int result = 0;
            for(int i = 0; i < mCurrentStageClearCount; i++)
            {
                result += mRewardArray[i].GetRewardDisplayInfo().Amount;
            }
            return result;
        }
        public void PiggyBankDefaultRewardProgress()
        {
            mDefaultReward.GrantReward();
            CoreContainer.RewardContainer.AddReward(mDefaultReward.GetRewardDisplayInfo());
        }
        public void PiggyBankRewardProgress()
        {
            for(int i = 0; i < mCurrentStageClearCount; i++)
            {
                mRewardArray[i].GrantReward();
                CoreContainer.RewardContainer.AddReward(mRewardArray[i].GetRewardDisplayInfo());
            }
            PlayerDataManager.Inst.EndPiggyBankContent();
        }
        private void SetPiggyBankData()
        {
            mCurrentStageClearCount = PlayerDataManager.Inst.PiggyBankStageClearCount;

            mbIsFull = false;

            if(mCurrentStageClearCount >= mRequiredStageClearCount)
            {
                mCurrentStageClearCount = mRequiredStageClearCount;
                mbIsFull = true;
            }

            if(!PlayerDataManager.Inst.PiggyBankPurchase)
            {
                mbHasNewthing = true;
                mbCanConfirm = false;
            }
            else
            {
                mbHasNewthing = false;
                mbCanConfirm = true;
            }
        }
    }   
}
