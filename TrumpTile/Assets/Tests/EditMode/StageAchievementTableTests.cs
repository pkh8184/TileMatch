using System.Collections.Generic;
using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class StageAchievementTableTests
	{
		[Test]
		public void Milestones_AreSortedAscending()
		{
			//GetAchievedIds가 오름차순을 전제로 조기 종료하므로 정렬이 깨지면 조용히 누락된다.
			IReadOnlyList<StageAchievementTable.Milestone> milestones = StageAchievementTable.Milestones;

			for(int i = 1; i < milestones.Count; i++)
			{
				Assert.Less(milestones[i - 1].Stage, milestones[i].Stage);
			}
		}

		[Test]
		public void Milestones_HaveNoDuplicateAchievementId()
		{
			HashSet<string> seen = new HashSet<string>();

			foreach(StageAchievementTable.Milestone milestone in StageAchievementTable.Milestones)
			{
				Assert.IsFalse(string.IsNullOrEmpty(milestone.AchievementId), $"스테이지 {milestone.Stage}의 업적 ID가 비어있다.");
				Assert.IsTrue(seen.Add(milestone.AchievementId), $"업적 ID 중복: {milestone.AchievementId}");
			}
		}

		[Test]
		public void GetAchievedIds_WhenBelowFirstMilestone_ReturnsEmpty()
		{
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 9);

			Assert.IsEmpty(ids);
		}

		[Test]
		public void GetAchievedIds_WhenExactlyOnMilestone_IncludesThatMilestone()
		{
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 10);

			Assert.AreEqual(1, ids.Count);
			Assert.AreEqual(StageAchievementTable.Milestones[0].AchievementId, ids[0]);
		}

		[Test]
		public void GetAchievedIds_WhenBetweenMilestones_ReturnsOnlyPassedOnes()
		{
			//99는 50 구간까지만 달성. 100 구간은 아직이다.
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 99);

			Assert.AreEqual(4, ids.Count);
			Assert.IsFalse(ids.Contains(StageAchievementTable.Milestones[4].AchievementId));
		}

		[Test]
		public void GetAchievedIds_WhenStage100_IncludesHundredMilestone()
		{
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 100);

			Assert.AreEqual(5, ids.Count);
			Assert.Contains(StageAchievementTable.Milestones[4].AchievementId, ids);
		}

		[Test]
		public void GetAchievedIds_WhenAllCleared_ReturnsEveryMilestone()
		{
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 500);

			Assert.AreEqual(StageAchievementTable.MilestoneCount, ids.Count);
		}

		[Test]
		public void GetAchievedIds_WhenBeyondLastMilestone_DoesNotExceedMilestoneCount()
		{
			List<string> ids = StageAchievementTable.GetAchievedIds(maxClearedStage: 9999);

			Assert.AreEqual(StageAchievementTable.MilestoneCount, ids.Count);
		}

		[Test]
		public void GetAchievedIds_WhenStageIsZeroOrNegative_ReturnsEmpty()
		{
			Assert.IsEmpty(StageAchievementTable.GetAchievedIds(maxClearedStage: 0));
			Assert.IsEmpty(StageAchievementTable.GetAchievedIds(maxClearedStage: -1));
		}
	}
}
