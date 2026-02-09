using UnityEngine;
using System;

namespace TrumpTile.Data
{
    /// <summary>
    /// 아이템 타입
    /// </summary>
    public enum ItemType
    {
        None = 0,
        Strike = 1,         // 타일 섞기
        BlackHole = 2,      // 슬롯 1칸 비우기
        Boom = 3,           // 랜덤 타일 3세트 제거
        Coin = 100,         // 코인 (재화)
        Gem = 101,          // 보석 (프리미엄 재화)
    }

    /// <summary>
    /// 아이템 등급
    /// </summary>
    public enum ItemGrade
    {
        Common = 0,     // 일반
        Rare = 1,       // 희귀
        Epic = 2,       // 에픽
        Legendary = 3,  // 전설
    }

    /// <summary>
    /// 아이템 테이블 데이터
    /// </summary>
    [Serializable]
    public class ItemData
    {
        [Header("기본 정보")]
        public int itemId;               // 아이템 고유 ID
        public ItemType itemType;        // 아이템 타입
        public string itemName;          // 아이템 이름
        public string description;       // 아이템 설명
        
        [Header("가치")]
        public ItemGrade grade;          // 아이템 등급
        public int value;                // 가치 (정렬, 비교용)
        
        [Header("가격")]
        public int coinPrice;            // 코인 가격
        public int gemPrice;             // 보석 가격
        
        [Header("리소스")]
        public string iconSrc;           // 아이콘 경로
        public string effectSrc;         // 이펙트 경로
        public string soundSrc;          // 사운드 경로
        
        [Header("제한")]
        public int maxStack;             // 최대 보유 개수 (0이면 무제한)
        public bool isConsumable;        // 소모성 여부
        public bool isPurchasable;       // 구매 가능 여부
    }

    /// <summary>
    /// 아이템 테이블 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "TrumpTile/Data/Item Table")]
    public class ItemTable : ScriptableObject
    {
        public ItemData[] items;

        /// <summary>
        /// 아이템 ID로 데이터 찾기
        /// </summary>
        public ItemData GetItemById(int itemId)
        {
            if (items == null) return null;
            
            foreach (var item in items)
            {
                if (item.itemId == itemId)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 아이템 타입으로 데이터 찾기
        /// </summary>
        public ItemData GetItemByType(ItemType type)
        {
            if (items == null) return null;
            
            foreach (var item in items)
            {
                if (item.itemType == type)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 구매 가능한 아이템 목록
        /// </summary>
        public ItemData[] GetPurchasableItems()
        {
            if (items == null) return new ItemData[0];
            
            return System.Array.FindAll(items, item => item.isPurchasable);
        }
    }
}
