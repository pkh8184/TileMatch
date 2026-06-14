using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests
{
	public class AlbumSystemTests
	{
		[Test]
		public void GetPictureState_WhenStageRewarded_ReturnsCollected()
		{
			EAlbumPictureState state = AlbumContent.GetPictureState(stageValue: 5, currentStage: 10, lastAlbumRewardedStage: 5);
			Assert.AreEqual(EAlbumPictureState.Collected, state);
		}

		[Test]
		public void GetPictureState_WhenStageClearedButNotRewarded_ReturnsAvailable()
		{
			EAlbumPictureState state = AlbumContent.GetPictureState(stageValue: 5, currentStage: 6, lastAlbumRewardedStage: 0);
			Assert.AreEqual(EAlbumPictureState.Available, state);
		}

		[Test]
		public void GetPictureState_WhenStageNotCleared_ReturnsLocked()
		{
			EAlbumPictureState state = AlbumContent.GetPictureState(stageValue: 10, currentStage: 5, lastAlbumRewardedStage: 0);
			Assert.AreEqual(EAlbumPictureState.Locked, state);
		}

		[Test]
		public void GetPictureState_WhenStageValueEqualsCurrentStage_ReturnsLocked()
		{
			// currentStage = lastClearedStage + 1 이므로, stageValue == currentStage 는 아직 미클리어
			EAlbumPictureState state = AlbumContent.GetPictureState(stageValue: 5, currentStage: 5, lastAlbumRewardedStage: 0);
			Assert.AreEqual(EAlbumPictureState.Locked, state);
		}
	}
}
