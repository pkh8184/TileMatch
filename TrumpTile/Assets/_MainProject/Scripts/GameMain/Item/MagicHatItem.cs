using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.Item
{
	// 보드 내 모든 타일을 무작위로 셔플
	public class MagicHatItem : IItem
	{
		public int ItemId => 1007;

		private BoardManager mBoardManager;
		private EffectManager mEffectManager;

		public MagicHatItem(BoardManager boardManager, EffectManager effectManager)
		{
			mBoardManager = boardManager;
			mEffectManager = effectManager;
		}

		public bool CanExecute()
		{
			return mBoardManager != null && mBoardManager.HasRemainingTiles();
		}

		public IEnumerator Execute(Action onComplete)
		{
			List<TileController> boardTiles = mBoardManager.GetBoardTiles();
			List<Transform> tileTransforms = boardTiles
				.Where(t => t != null)
				.Select(t => t.transform)
				.ToList();

			bool bEffectComplete = false;
			mEffectManager?.PlayBlackHoleEffect(
				tileTransforms,
				() => { },
				() =>
				{
					// BoardManager 자신이 코루틴 실행자로 동작
					mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
					bEffectComplete = true;
				}
			);

			float elapsed = 0f;
			const float TIMEOUT = 5f;
			while (!bEffectComplete && elapsed < TIMEOUT)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			yield return new WaitForSeconds(0.2f);

			onComplete?.Invoke();
		}
	}
}
