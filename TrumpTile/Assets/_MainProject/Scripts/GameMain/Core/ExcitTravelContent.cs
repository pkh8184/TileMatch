using System;
using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class ExcitTravelContent : TemporaryContent
    {
        [Header("유료 보상 아이디")]
        [SerializeField] private EProductId[] mPaidRewardIDArray;
        [Header("무료 보상 목록")]
        [SerializeReference, SubclassSelector] private ProductReward[] mFreeRewardArray;
        private const int PAID_INTERVAL = 3;
        private const int MAX_REWARD_COUNT = 12;
        private int mCurrentIndex;
        public int CurrentIndex => mCurrentIndex;
        public override void Initialize()
        {
            base.Initialize();

            //쿨타임/제한시간 기준으로 활성 상태 재평가 (지났으면 재활성화 + 진행도 초기화)
            EvaluateActiveState();

            RefreshExcitTravelData();
        }

        public override void Refresh()
        {
            base.Refresh();

            EvaluateActiveState();

            RefreshExcitTravelData();
        }

        private void RefreshExcitTravelData()
        {
            mCurrentIndex = PlayerDataManager.Inst.ExcitTravelIndex;
            mbHasNewthing = mCurrentIndex < MAX_REWARD_COUNT;
        }

        protected override bool GetActiveFlag() => PlayerDataManager.Inst.IsExcitTravelActive;
        protected override DateTime GetActiveDate() => PlayerDataManager.Inst.ExcitTravelActiveDate;
        protected override DateTime GetUnActiveDate() => PlayerDataManager.Inst.ExcitTravelUnActiveDate;
        protected override void OnActivate() => PlayerDataManager.Inst.ActiveExcitTravel();
        protected override void OnDeactivate() => PlayerDataManager.Inst.UnActiveExcitTravel();

        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();

                if(!PlayerDataManager.Inst.UserData.ExcitTravelUnlock)
                {
                    PlayerDataManager.Inst.UnlockExcitTravel();
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
        public void ConfirmCurrentReward()
        {
            int modifiedIndex = mCurrentIndex + 1;

            if(modifiedIndex % PAID_INTERVAL == 0)
            {
                modifiedIndex = modifiedIndex / PAID_INTERVAL - 1;
                IAPManager.Instance.PurchaseProduct(mPaidRewardIDArray[modifiedIndex]);  
            }
            else
            {
                modifiedIndex = mCurrentIndex - modifiedIndex / PAID_INTERVAL;
                mFreeRewardArray[modifiedIndex].GrantReward();

                List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
                infos.Add(mFreeRewardArray[modifiedIndex].GetRewardDisplayInfo());

                EventManager.Inst.ActiveEvent(EventKeys.PLAY_MINI_REWARD_ANIM, new MiniRewardPayload{Infos = infos, Type = EMiniRewardAnimType.ViewContent});
                CoreContainer.RewardContainer.AddReward(mFreeRewardArray[modifiedIndex].GetRewardDisplayInfo());
                mCurrentIndex++;
                PlayerDataManager.Inst.IncreaseExcitTravelIndex();
            }
        }
        public void OnPurchaseSuccess()
        {
            int modifiedIndex = mCurrentIndex + 1;
            modifiedIndex = modifiedIndex / PAID_INTERVAL - 1;

            List<RewardDisplayInfo> infos = IAPManager.Instance.GetRewardDisplayInfos(mPaidRewardIDArray[modifiedIndex]);
            EventManager.Inst.ActiveEvent(EventKeys.PLAY_MINI_REWARD_ANIM, new MiniRewardPayload{Infos = infos, Type = EMiniRewardAnimType.ViewContent});

            foreach(var item in infos)
            {
                CoreContainer.RewardContainer.AddReward(item);   
            }

            mCurrentIndex++;
            PlayerDataManager.Inst.IncreaseExcitTravelIndex();
            if(mCurrentIndex >= MAX_REWARD_COUNT)
            {
                PlayerDataManager.Inst.UnActiveExcitTravel();
                mbHasNewthing = false;
            }
        }
        public int GetRewardCount()
        {
            return MAX_REWARD_COUNT;
        }
        public int GetPaidInterval()
        {
            return PAID_INTERVAL;
        }
        public RewardDisplayInfo GetRewardDisplayInfo(int index)
        {
            return mFreeRewardArray[index].GetRewardDisplayInfo();
        }
        public EProductId GetProductId(int index)
        {
            return mPaidRewardIDArray[index];
        }
    }    
}

