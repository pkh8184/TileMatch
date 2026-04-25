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

			int setsToRemove = Mathf.Min(3, groups.Count);
			List<Vector3> allPositions = new List<Vector3>();
			List<TileController> allTilesToRemove = new List<TileController>();

			for (int i = 0; i < setsToRemove; i++)
			{
				foreach (TileController tile in groups[i].Take(mMatchCount))
				{
					if (tile != null)
					{
						allPositions.Add(tile.transform.position);
						allTilesToRemove.Add(tile);
					}
				}
			}

			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			bool bEffectComplete = false;
			mEffectManager?.PlayBoomEffect(allPositions, () => { bEffectComplete = true; });

			foreach (TileController tile in allTilesToRemove)
			{
				if (tile != null)
				{
					mBoardManager.RemoveTile(tile);
					tile.Remove();
				}
			}

			float elapsed = 0f;
			const float TIMEOUT = 2f;
			while (!bEffectComplete && elapsed < TIMEOUT)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			mBoardManager.UpdateAllBlockedStates();

			yield return new WaitForSeconds(0.3f);

			onComplete?.Invoke();
		}
	}
}
