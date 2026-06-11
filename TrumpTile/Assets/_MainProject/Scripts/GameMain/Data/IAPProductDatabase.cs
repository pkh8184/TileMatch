using System.Collections;
using System.Collections.Generic;
using System.Security;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TrumpTile.GameMain.Data
{
    public enum ERewardType
    {
        Gold,
        Item,
        ETC,
        Length
    }
    public class RewardDisplayInfo
    {
        public ERewardType Type;
        public int Amount;
        public int ItemId;
    }
    [System.Serializable]
    public abstract class ProductReward
    {
        public abstract void GrantReward();
        public virtual RewardDisplayInfo GetRewardDisplayInfo()
        {
            return null;
        }
    }
    public abstract class ConsumableReward : ProductReward
    {
        [SerializeField] protected int mCount;
    }
    [System.Serializable]
    public class GoldReward : ConsumableReward
    {
        public override void GrantReward()
        {
            PlayerDataManager.Inst.AddGold(mCount);
                        
        }
        public override RewardDisplayInfo GetRewardDisplayInfo()
        {
            return new RewardDisplayInfo{Type = ERewardType.Gold, Amount = mCount};
        }
    }
    [System.Serializable]
    public class ItemReward : ConsumableReward
    {
        [SerializeField] private int mItemId;
        public override void GrantReward()
        {
            PlayerDataManager.Inst.AddItemCount(mItemId, mCount);
        }
         public override RewardDisplayInfo GetRewardDisplayInfo()
        {
            return new RewardDisplayInfo{Type = ERewardType.Item, Amount = mCount, ItemId = mItemId};
        }
    }
    [System.Serializable]
    public class RemoveAds : ProductReward
    {
        public override void GrantReward()
        {
            PlayerDataManager.Inst.RemoveAds();
        }
    }
    [System.Serializable]
    public class SeasonPass : ProductReward
    {
        public override void GrantReward()
        {
            PlayerDataManager.Inst.UnlockSeasonPass();
        }
    }
    [System.Serializable]
    public class PiggyBank : ProductReward
    {
        public override void GrantReward()
        {
            PlayerDataManager.Inst.UnlockPiggyBank();
        }
    }

    public enum EProductId
    {
        GoldPackage_1,
        GoldPackage_2,
        GoldPackage_3,
        GoldPackage_4,
        GoldPackage_5,
        GoldPackage_6,
        ItemPackage_1,
        ItemPackage_2,
        ItemPackage_3,
        ItemPackage_4,
        NewbiePackage,
        BigginerPackage,
        MasterPackage,
        RemoveAds,
        PiggyBank,
        None
    }
    [System.Serializable]
    public class ProductEntry
    {
        [SerializeField] private EProductId mEProductId;
        [SerializeField] private ProductType mEProductType;
        [SerializeField] private string mProductId;
        [SerializeReference, SubclassSelector] private List<ProductReward> mRewardList = new List<ProductReward>();

        public EProductId EProductId => mEProductId;
        public ProductType EProductType => mEProductType;
        public string ProductId => mProductId;

        public void GrantReward()
        {
            foreach(var item in mRewardList)
            {
                item.GrantReward();
            }
            EventManager.Inst.ActiveEvent("GetPackageReward", mRewardList);
        }
    }
    [System.Serializable]
    [CreateAssetMenu(fileName = "IAPProductDatabase", menuName = "TrumpTile/IAPProductDatabase")]
    public class IAPProductDatabase : ScriptableObject
    {
        [SerializeField] private ProductEntry[] mProductEntries;
        private Dictionary<EProductId, ProductEntry> mProductMap;

        public ProductEntry[] ProductEntries => mProductEntries;
        public Dictionary<EProductId, ProductEntry> ProductMap => mProductMap;
        public void Initialize()
        {
            mProductMap = new Dictionary<EProductId, ProductEntry>();
            foreach (var entry in mProductEntries)
            {
                mProductMap[entry.EProductId] = entry;
            }
        }
        public string GetProductId(EProductId eProductId)
        {
            return mProductMap[eProductId].ProductId;
        }
        public void GrantReward(string id)
        {
            foreach(var item in mProductMap)
            {
                if(item.Value.ProductId == id)
                {
                    item.Value.GrantReward();
                }
            }
        }

    }
}

