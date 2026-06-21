using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class PiggyBankContent : TemporaryContent
    {
        [Header("보상 목록")]
        [SerializeReference, SubclassSelector] private ProductReward[] mRewardArray;

        [Header("스테이지 클리어 요구 횟수")]
        [SerializeField] private int mFirstRequiredStageClearCount;
        [SerializeField] private int mSecondRequiredStageClearCount;
        private int mCurrentStageClearCount;
        private bool mbCanConfirm;
        public bool CanConfirm => mbCanConfirm;
        
        public bool CanGetRewardAfterEndActive => !mbIsActive && mbCanConfirm;
        private bool mbIsFull;
        public bool IsFull => mbIsFull;
        private int mCurrentCheckPoint;
        public override void Initialize()
        {
            base.Initialize();
            
            if(!PlayerDataManager.Inst.IsPiggyBankActive)
            {
                //비활성화 시간 - 현재 시간 > 쿨타임 이면 활성화
                //첫 활성화인 경우 비활성화 시간 == null 검사 후 활성화
            }

            mbIsActive = true;

            SetPiggyBankData();
        }
        public override void Refresh()
        {
            base.Refresh();

            SetPiggyBankData();
        }
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
        }
        public int GetCurrentStageCount()
        {
            return mCurrentStageClearCount;
        }
        public int GetCurrentCheckPoint()
        {
            return mCurrentCheckPoint;
        }
        public int GetMaxCount()
        {
            return mSecondRequiredStageClearCount;
        }
        public int GetCurrentStackedGold()
        {
            int result = 0;
            for(int i = 0; i <= mCurrentCheckPoint; i++)
            {
                result += mRewardArray[i].GetRewardDisplayInfo().Amount;
            }

            return result;
        }
        public void PiggyBankRewardProgress()
        {
            for(int i = 0; i <= mCurrentCheckPoint; i++)
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

            if(mCurrentStageClearCount >= mSecondRequiredStageClearCount)
            {
                mCurrentStageClearCount = mSecondRequiredStageClearCount;
                mbIsFull = true;
            }

            mCurrentCheckPoint = 0;
            if(mCurrentStageClearCount >= mFirstRequiredStageClearCount)
            {
                mCurrentCheckPoint++;
                if(mCurrentStageClearCount >= mSecondRequiredStageClearCount)
                {
                    mCurrentCheckPoint++;
                }
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
