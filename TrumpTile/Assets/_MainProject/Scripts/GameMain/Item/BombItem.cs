using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.Item
{
	// 보드 내 짝이 되는 타일 3개씩 최대 3세트 파괴
	public class BombItem : IItem
	{
		public int ItemId => 1008;

		private const float BOMB_SET_DELAY = 0.15F;
		private const float TIMEOUT = 3F;

		private BoardManager mBoardManager;
		private SlotManager mSlotManager;
		private EffectManager mEffectManager;
		private int mMatchCount;

		public BombItem(SlotManager slotManager, BoardManager boardManager, EffectManager effectManager, int matchCount)
		{
			mSlotManager = slotManager;
			mBoardManager = boardManager;
			mEffectManager = effectManager;
			mMatchCount = matchCount;
		}

		public bool CanExecute()
		{
			if (mBoardManager == null)
			{
				return false;
			}
			return mBoardManager.GetBoardTilesContainSlot()
				.Where(t => t != null && t.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Any(g => g.Count() >= mMatchCount);
			// return mBoardManager.GetBoardTiles()
			// 	.Where(t => t != null && t.Data != null)
			// 	.GroupBy(t => t.Data.TileID)
			// 	.Any(g => g.Count() >= mMatchCount);
		}

		public IEnumerator Execute(Action onComplete)
		{
			//List<IGrouping<string, TileController>> groups = mBoardManager.GetBoardTiles()
			// List<IGrouping<string, TileController>> groups = mBoardManager.GetBoardTilesContainSlot()
			// 	.Where(t => t != null && t.Data != null)
			// 	.GroupBy(t => t.Data.TileID)
			// 	.Where(g => g.Count() >= mMatchCount)
			// 	.ToList();

			List<int> random = new List<int>();
			for(int i = 0; i < mBoardManager.GetBoardTilesContainSlot().Count; i++)
			{
				random.Add(i);
			}
			List<TileController> setsTiles = new List<TileController>();
			int setsToRemove = Mathf.Min(3, mBoardManager.GetBoardTilesContainSlot().Count / 3);

			for(int i = 0; i < setsToRemove; i++)
			{
				int index = random[UnityEngine.Random.Range(0, random.Count)];
				string id = mBoardManager.GetBoardTilesContainSlot()[index].TileTypeId;

				List<TileController> targets = mBoardManager.GetBoardTilesContainSlot().Where(t => t != null && t.TileTypeId == id).Take(3).ToList();
				foreach(var item in targets)
				{
					setsTiles.Add(item);
					int j = mBoardManager.GetBoardTilesContainSlot().IndexOf(item);
					Debug.Log("선택된 타일 인덱스 : " + j.ToString());
					random.Remove(j);
				}
			}
			if (setsTiles.Count == 0)
			{
				onComplete?.Invoke();
				yield break;
			}
			Vector3 center = Vector3.zero;

			List<TileController> capturedTiles = setsTiles;
			int pendingCount = capturedTiles.Count;
			if (mEffectManager != null)
			{
				mEffectManager.PlayBombSpineEffect(center, () =>
				{
					SettingsManager.Inst?.Vibrate(EVibrationStyle.Heavy);
					foreach (TileController tile in capturedTiles)
					{
						if (tile != null)
						{
							if(tile.IsInSlot)
							{
								mSlotManager.RemoveTileDirectly(tile);
							}
							else
							{
								mBoardManager.RemoveTileFromBoard(tile, bProcessBonus: false);
								tile.Remove();
							};
						}
						pendingCount--;
					}
				});
			}
			else
			{
				SettingsManager.Inst?.Vibrate(EVibrationStyle.Heavy);
				foreach (TileController tile in capturedTiles)
				{
					if (tile != null)
					{
						if(tile.IsInSlot)
						{
							mSlotManager.RemoveTileDirectly(tile);
						}
						else
						{
							mBoardManager.RemoveTileFromBoard(tile, bProcessBonus: false);
							tile.Remove();
						};
					}
					pendingCount--;
				}
			}
			AudioEvent.Play(EAudioKey.SFX_Bomb);

			float elapsed = 0F;
			float hapticElapsed = 0F;
			const float HAPTIC_INTERVAL = 0.3F;
			while (pendingCount > 0 && elapsed < TIMEOUT)
			{
				elapsed += Time.deltaTime;
				hapticElapsed += Time.deltaTime;
				if (hapticElapsed >= HAPTIC_INTERVAL)
				{
					hapticElapsed = 0F;
					SettingsManager.Inst?.Vibrate(EVibrationStyle.Light);
				}
				yield return null;
			}

			yield return new WaitForSeconds(0.3F);

			mBoardManager.UpdateAllBlockedStates();

			onComplete?.Invoke();

			if(BoardManager.Instance.GetAllTiles().Count == 0 && SlotManager.Instance.GetAllSlotTiles().Count == 0)
			{
				GameManager.Instance.LevelClear();
			}
		}

		private Vector3 ComputeCenter(List<TileController> tiles)
		{
			Vector3 sum = Vector3.zero;
			int count = 0;
			foreach (TileController tile in tiles)
			{
				if (tile != null)
				{
					sum += tile.transform.position;
					count++;
				}
			}
			return count > 0 ? sum / count : Vector3.zero;
		}
	}
}
