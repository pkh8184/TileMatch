using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum EAlbumPictureState
	{
		Locked,    // StageValue 미달 또는 이전 챕터 미완성
		Available, // StageValue 달성, 미수집
		Collected, // 수집 완료
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

		public static EAlbumPictureState GetPictureState(int pictureId, int stageValue, int currentStage, System.Collections.Generic.List<int> collectedIds)
		{
			if (collectedIds != null && collectedIds.Contains(pictureId))
			{
				return EAlbumPictureState.Collected;
			}
			if (currentStage >= stageValue)
			{
				return EAlbumPictureState.Available;
			}
			return EAlbumPictureState.Locked;
		}

		public static bool IsChapterComplete(TBPictureData[] groupPictures, System.Collections.Generic.List<int> collectedIds)
		{
			if (groupPictures == null || groupPictures.Length == 0)
			{
				return false;
			}
			foreach (TBPictureData picture in groupPictures)
			{
				if (collectedIds == null || !collectedIds.Contains(picture.PictureId))
				{
					return false;
				}
			}
			return true;
		}
	}
}
