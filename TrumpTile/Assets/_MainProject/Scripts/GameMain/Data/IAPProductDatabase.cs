using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TrumpTile.GameMain.Data
{
    [System.Serializable]
    public abstract class ProductReward
    {
        public virtual void GrantReward() {}
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
    }
    [System.Serializable]
    public class ItemReward : ConsumableReward
    {
        [SerializeField] private int itemId;
        public override void GrantReward()
        {
            PlayerDataManager.Inst.AddItemCount(itemId, mCount);
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

