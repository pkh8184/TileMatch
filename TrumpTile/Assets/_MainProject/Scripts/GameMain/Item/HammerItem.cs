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
			AudioEvent.Play(EAudioKey.SFX_ItemUse);

			Vector3 popPosition = mSlotManager.GetLastTilePosition();

			bool bActionDone = false;

			if (mEffectManager != null)
			{
				mEffectManager.PlayHammerSpineEffect(popPosition, () =>
				{
					Vector3 landPosition;
					bool bSuccess = mSlotManager.RemoveOneTileToBoard(out landPosition, bWithSpin: true);
					if (bSuccess)
					{
						Vector3 actualLandPos = mBoardManager != null
							? mBoardManager.GetLastPlacedTilePosition()
							: landPosition;
						mEffectManager.PlayStrikeLandEffect(actualLandPos);
					}
					bActionDone = true;
				});
			}
			else
			{
				Vector3 landPosition;
				mSlotManager.RemoveOneTileToBoard(out landPosition, bWithSpin: true);
				bActionDone = true;
			}

			yield return new WaitUntil(() => bActionDone);
			yield return new WaitForSeconds(0.3f);

			onComplete?.Invoke();
		}
	}
}
