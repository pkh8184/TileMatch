using System.Diagnostics;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Core
{
    //획득한 보상을 임시로 저장할 컨테이너
    public class RewardContainer
    {
        private int mGold;
        private int mHammer;
        private int mClock;
        private int mHat;
        private int mBomb;
        private int mGem;

        public int Gold => mGold;
        public int Gem => mGem;

        public void AddReward(RewardDisplayInfo info)
        {
            if(info == null) return;
            
            if(info.Type == ERewardType.Gold)
            {
                mGold += info.Amount;
            }
            else if(info.Type == ERewardType.Item)
            {
                 switch(info.ItemId)
                {
                    case 1005:
                        mHammer += info.Amount;
                        break;
                    case 1006:
                        mClock += info.Amount;
                        break;
                    case 1007:
                        mHat += info.Amount;
                        break;
                    case 1008:
                        mBomb += info.Amount;
                        break;
                    default:
                        UnityEngine.Debug.Log("[RewardContainer] 아이템 ID가 유효하지 않습니다.");
                        break;
                }
            }
        }
        public void Clear()
        {
            mGold = 0;
            mGem = 0;
        }
        public void AddGold(int amount)
        {
            mGold += amount;
        }
        public void AddItem(int itemId, int amount)
        {
            switch(itemId)
            {
                case 1005:
                    mHammer += amount;
                    break;
                case 1006:
                    mClock += amount;
                    break;
                case 1007:
                    mHat += amount;
                    break;
                case 1008:
                    mBomb += amount;
                    break;
                default:
                    UnityEngine.Debug.Log("[RewardContainer] 아이템 ID가 유효하지 않습니다.");
                    break;
            }
        }
        public void AddGem(int amount)
        {
            mGem += amount;
        }
        public int UseGold()
        {
            int result = mGold;
            mGold = 0;
            return result;
        }
        public int[] UseItem()
        {
            int[] result = {mHammer, mClock, mHat, mBomb};

            mHammer = 0;
            mClock = 0;
            mHat = 0;
            mBomb = 0;

            return result;
        }
        public int UseGem()
        {
            int result = mGem;
            mGem = 0;
            return result;
        }
        public bool HasRewards()
        {
            bool hasRewards = mGold > 0 || mHammer > 0 || mClock > 0 || mHat > 0 || mBomb > 0 || mGem > 0;

            return hasRewards;
        }
        public bool HasGoldRewads()
        {
            return mGold > 0;
        }
    }    
}

