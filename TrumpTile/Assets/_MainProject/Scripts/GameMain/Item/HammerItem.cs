using System;
using System.Collections;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.Item
{
	// 슬롯 맨 우측 타일 1개를 보드 최상위 레이어로 이동
	public class HammerItem : IItem
	{
		public int ItemId => 1005;

		private SlotManager mSlotManager;
		private BoardManager mBoardManager;
		private EffectManager mEffectManager;

		public HammerItem(SlotManager slotManager, BoardManager boardManager, EffectManager effectManager)
		{
			mSlotManager = slotManager;
			mBoardManager = boardManager;
			mEffectManager = effectManager;
		}

		public bool CanExecute()
		{
			return mSlotManager != null && mSlotManager.CurrentTileCount > 0;
		}

		public IEnumerator Execute(Action onComplete)
		{
			Vector3 popPosition = mSlotManager.GetLastTilePosition();
			if (mEffectManager != null)
			{
				mEffectManager.PlayStrikePopEffect(popPosition);
				mEffectManager.PlayHammerSpineEffect(popPosition);
			}
			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			yield return new WaitForSeconds(0.3f);

			Vector3 landPosition;
			bool bSuccess = mSlotManager.RemoveOneTileToBoard(out landPosition);
			if (bSuccess)
			{
				Vector3 actualLandPosition = mBoardManager != null
					? mBoardManager.GetLastPlacedTilePosition()
					: landPosition;
				if (mEffectManager != null)
				{
					mEffectManager.PlayStrikeLandEffect(actualLandPosition);
				}
			}

			yield return new WaitForSeconds(0.2f);

			onComplete?.Invoke();
		}
	}
}
