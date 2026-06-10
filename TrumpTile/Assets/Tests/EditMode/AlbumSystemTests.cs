using System.Collections.Generic;
using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests
{
	public class AlbumSystemTests
	{
		[Test]
		public void GetPictureState_WhenCollected_ReturnsCollected()
		{
			List<int> collected = new List<int> { 101 };
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 5, currentStage: 3, collected);
			Assert.AreEqual(EAlbumPictureState.Collected, state);
		}

		[Test]
		public void GetPictureState_WhenStageMetAndNotCollected_ReturnsAvailable()
		{
			List<int> collected = new List<int>();
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 5, currentStage: 5, collected);
			Assert.AreEqual(EAlbumPictureState.Available, state);
		}

		[Test]
		public void GetPictureState_WhenStageNotMet_ReturnsLocked()
		{
			List<int> collected = new List<int>();
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 10, currentStage: 5, collected);
			Assert.AreEqual(EAlbumPictureState.Locked, state);
		}

		[Test]
		public void GetPictureState_WhenCollectedListNull_DoesNotThrow()
		{
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 3, currentStage: 5, null);
			Assert.AreEqual(EAlbumPictureState.Available, state);
		}
	}
}
