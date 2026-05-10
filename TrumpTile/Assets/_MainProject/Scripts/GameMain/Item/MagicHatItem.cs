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

		private const float TIMEOUT = 5f;

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
			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			List<TileController> boardTiles = mBoardManager.GetBoardTiles();
			List<Transform> tileTransforms = boardTiles
				.Where(t => t != null)
				.Select(t => t.transform)
				.ToList();

			bool bEffectComplete = false;

			if (mEffectManager != null)
			{
				// BlackHoleEffect: 타일 흡수 → 셔플 → 분산 애니메이션
				// Spine 이펙트는 BlackHoleEffectCoroutine 내부에서 함께 재생됨
				mEffectManager.PlayBlackHoleEffect(
					tileTransforms,
					() =>
					{
						// 타일이 중앙에 모인 순간 TileData 교환 (위치는 그대로, 카드 face만 셔플)
						mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
					},
					() =>
					{
						bEffectComplete = true;
					}
				);
			}
			else
			{
				mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
				bEffectComplete = true;
			}

			float elapsed = 0f;
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
