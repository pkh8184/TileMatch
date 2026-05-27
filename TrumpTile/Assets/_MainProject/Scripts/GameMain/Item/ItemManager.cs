using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Item
{
	public class ItemManager : Singleton_GameObject<ItemManager>
	{
		[Header("Data")]
		[SerializeField] private TBItemTable mItemTable;

		private Dictionary<int, IItem> mItems = new Dictionary<int, IItem>();
		private Dictionary<int, int> mItemCounts = new Dictionary<int, int>();
		private bool mIsItemInProgress = false;

		// (itemId, 새 개수)
		public event Action<int, int> OnItemCountChanged;

		public void Initialize(
			BoardManager boardManager,
			SlotManager slotManager,
			EffectManager effectManager,
			ITimerControllable timerControllable,
			int matchCount)
		{
			mItems.Clear();
			RegisterItem(new HammerItem(slotManager, effectManager));
			RegisterItem(new MagicWandItem(timerControllable, effectManager));
			RegisterItem(new MagicHatItem(boardManager, effectManager));
			RegisterItem(new BombItem(boardManager, effectManager, matchCount));

			LoadItemCounts();
		}

		private void RegisterItem(IItem item)
		{
			mItems[item.ItemId] = item;
		}

		private void LoadItemCounts()
		{
			mItemCounts = PlayerDataManager.Inst.GetAllItemCounts();
		}

		#region 조회

		public bool CanUseItem() => !mIsItemInProgress;

		public int GetItemCount(int itemId)
		{
			int count;
			return mItemCounts.TryGetValue(itemId, out count) ? count : 0;
		}

		public bool IsUnlocked(int itemId, int currentStage)
		{
			if (mItemTable == null)
			{
				return true;
			}
			TBItemData data = mItemTable.GetById(itemId);
			return data == null || currentStage >= data.UnlockLevel;
		}

		public List<int> GetRegisteredItemIds()
		{
			return new List<int>(mItems.Keys);
		}

		#endregion

		#region 사용

		public void UseItem(int itemId)
		{
			if (mIsItemInProgress)
			{
				return;
			}

			IItem item;
			if (!mItems.TryGetValue(itemId, out item))
			{
				return;
			}
			if (GetItemCount(itemId) <= 0)
			{
				// 임시
				EventManager.Inst.ActiveEvent("PurchaseItem", (object)itemId);
				return;
			}
			if (!item.CanExecute())
			{
				return;
			}
			
			mItemCounts[itemId]--;
			PlayerDataManager.Inst.SetItemCount(itemId, mItemCounts[itemId]);
			EventManager.Inst.ActiveEvent("ItemCountChanged");
			//OnItemCountChanged?.Invoke(itemId, mItemCounts[itemId]);

			StartCoroutine(ExecuteItemCoroutine(item));
		}

		private IEnumerator ExecuteItemCoroutine(IItem item)
		{
			mIsItemInProgress = true;
			yield return StartCoroutine(item.Execute(null));
			mIsItemInProgress = false;
		}

		#endregion

		#region 추가
		public void AddItem(int itemId, int count)
		{
			if (mIsItemInProgress)
			{
				return;
			}

			IItem item;
			if (!mItems.TryGetValue(itemId, out item))
			{
				return;
			}
			if(!mItemCounts.ContainsKey(itemId))
			{
				mItemCounts[itemId] = 0;
			}
			mItemCounts[itemId] += count;
			
			PlayerDataManager.Inst.SetItemCount(itemId, mItemCounts[itemId]);
			EventManager.Inst.ActiveEvent("ItemCountChanged");
			
			//OnItemCountChanged?.Invoke(itemId, mItemCounts[itemId]);
		}
		#endregion

		#region 서버 동기화

		public void SaveItemCountsToServer()
		{
			foreach (KeyValuePair<int, int> pair in mItemCounts)
			{
				PlayerDataManager.Inst.SetItemCount(pair.Key, pair.Value);
			}
		}

		#endregion

		#region 디버그

		[ContextMenu("Debug: Fill All Items (99)")]
		private void Debug_FillAllItems()
		{
			foreach (int itemId in mItems.Keys)
			{
				mItemCounts[itemId] = 99;
				OnItemCountChanged?.Invoke(itemId, 99);
			}
			Debug.Log("[ItemManager] Debug: 모든 아이템 99개로 채움");
		}

		#endregion
	}
}
