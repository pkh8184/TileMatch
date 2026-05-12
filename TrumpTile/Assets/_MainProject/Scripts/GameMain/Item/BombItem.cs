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

		private const float BOMB_SET_DELAY = 0.15f;
		private const float TIMEOUT = 3f;

		private BoardManager mBoardManager;
		private EffectManager mEffectManager;
		private int mMatchCount;

		public BombItem(BoardManager boardManager, EffectManager effectManager, int matchCount)
		{
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
			return mBoardManager.GetBoardTiles()
				.Where(t => t != null && t.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Any(g => g.Count() >= mMatchCount);
		}

		public IEnumerator Execute(Action onComplete)
		{
			List<IGrouping<string, TileController>> groups = mBoardManager.GetBoardTiles()
				.Where(t => t != null && t.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= mMatchCount)
				.ToList();

			if (groups.Count == 0)
			{
				onComplete?.Invoke();
				yield break;
			}

			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			int setsToRemove = Mathf.Min(3, groups.Count);
			int pendingCount = setsToRemove;

			for (int i = 0; i < setsToRemove; i++)
			{
				List<TileController> setTiles = groups[i].Take(mMatchCount).ToList();
				Vector3 center = ComputeCenter(setTiles);

				List<TileController> capturedTiles = setTiles;

				if (mEffectManager != null)
				{
					mEffectManager.PlayBombSpineEffect(center, () =>
					{
						foreach (TileController tile in capturedTiles)
						{
							if (tile != null)
							{
								mBoardManager.RemoveTileFromBoard(tile, bProcessBonus: false);
								tile.Remove();
							}
						}
						pendingCount--;
					});
				}
				else
				{
					foreach (TileController tile in capturedTiles)
					{
						if (tile != null)
						{
							mBoardManager.RemoveTileFromBoard(tile, bProcessBonus: false);
							tile.Remove();
						}
					}
					pendingCount--;
				}

				yield return new WaitForSeconds(BOMB_SET_DELAY);
			}

			float elapsed = 0f;
			while (pendingCount > 0 && elapsed < TIMEOUT)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			mBoardManager.UpdateAllBlockedStates();
			yield return new WaitForSeconds(0.3f);

			onComplete?.Invoke();
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
