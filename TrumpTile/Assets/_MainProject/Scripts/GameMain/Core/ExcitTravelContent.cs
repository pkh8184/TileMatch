using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
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

            if(!PlayerDataManager.Inst.IsExcitTravelActive)
            {
                //비활성화 시간 - 현재 시간 > 쿨타임 이면 활성화
                //첫 활성화인 경우 비활성화 시간 == null 검사 후 활성화
            }

            mbIsActive = true;

            mCurrentIndex = PlayerDataManager.Inst.ExcitTravelIndex;

            if(mCurrentIndex >= MAX_REWARD_COUNT)
            {
                mbHasNewthing = false;
            }
            else
            {
                mbHasNewthing = true;
            }
        }

        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage > mLevelToUnlock)
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
                EventManager.Inst.ActiveEvent("GetReward", mFreeRewardArray[modifiedIndex].GetRewardDisplayInfo());

                mCurrentIndex++;
            }
        }
        public void OnPurchaseSuccess()
        {
            mCurrentIndex++;
            if(mCurrentIndex >= MAX_REWARD_COUNT)
            {
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
        public RewardDisplayInfo GetRewardDisplayInfos(int index)
        {
            return mFreeRewardArray[index].GetRewardDisplayInfo();
        }
        public EProductId GetProductId(int index)
        {
            return mPaidRewardIDArray[index];
        }
    }    
}

