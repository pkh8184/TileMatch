using System;
using System.Collections;
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
			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			bool bActionDone = false;

			if (mEffectManager != null)
			{
				mEffectManager.PlayMagicHatSpineEffect(() =>
				{
					mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
					bActionDone = true;
				});
			}
			else
			{
				mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
				bActionDone = true;
			}

			yield return new WaitUntil(() => bActionDone);
			yield return new WaitForSeconds(0.5f);

			onComplete?.Invoke();
		}
	}
}
