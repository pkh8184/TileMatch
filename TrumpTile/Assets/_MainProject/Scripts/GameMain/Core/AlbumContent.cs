using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum EAlbumPictureState
	{
		Locked,    // stageValue >= currentStage (스테이지 미달)
		Available, // lastAlbumRewardedStage < stageValue < currentStage (클리어했으나 보상 미수령)
		Collected, // stageValue <= lastAlbumRewardedStage (보상 수령 완료)
	}

	[System.Serializable]
	public class AlbumContent : ContentBase
	{
		private const int UNLOCK_STAGE = 3; // CurrentStage >= 3 = 스테이지 2 클리어 완료

		public override void Initialize()
		{
			base.Initialize();

			if (PlayerDataManager.Inst != null && PlayerDataManager.Inst.CurrentStage >= UNLOCK_STAGE)
			{
				SetUnlock();
			}
		}

		public static EAlbumPictureState GetPictureState(int stageValue, int currentStage, int lastAlbumRewardedStage)
		{
			if (stageValue <= lastAlbumRewardedStage)
			{
				return EAlbumPictureState.Collected;
			}
			if (stageValue < currentStage)
			{
				return EAlbumPictureState.Available;
			}
			return EAlbumPictureState.Locked;
		}

	}
}
