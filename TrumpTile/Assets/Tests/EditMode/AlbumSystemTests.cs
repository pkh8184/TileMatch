using System.Collections.Generic;
using NUnit.Framework;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.Tests
{
	public class AlbumSystemTests
	{
		// --- GetPictureState 테스트 ---

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

		// --- IsChapterComplete 테스트 ---

		[Test]
		public void IsChapterComplete_WhenAllCollected_ReturnsTrue()
		{
			TBPictureData[] pictures = new TBPictureData[]
			{
				new TBPictureData { PictureId = 1 },
				new TBPictureData { PictureId = 2 },
			};
			List<int> collected = new List<int> { 1, 2 };
			Assert.IsTrue(AlbumContent.IsChapterComplete(pictures, collected));
		}

		[Test]
		public void IsChapterComplete_WhenPartiallyCollected_ReturnsFalse()
		{
			TBPictureData[] pictures = new TBPictureData[]
			{
				new TBPictureData { PictureId = 1 },
				new TBPictureData { PictureId = 2 },
			};
			List<int> collected = new List<int> { 1 };
			Assert.IsFalse(AlbumContent.IsChapterComplete(pictures, collected));
		}

		[Test]
		public void IsChapterComplete_WhenEmptyGroup_ReturnsFalse()
		{
			Assert.IsFalse(AlbumContent.IsChapterComplete(new TBPictureData[0], new List<int>()));
		}
	}
}
