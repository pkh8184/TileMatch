using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class SpaceTravelContentTests
	{
		private static readonly int[] DEFAULT_MIN_PER_STAGE = { 5, 7, 8, 8, 10, 12, 20 };

		[Test]
		public void Budget_SumEqualsEliminated_WhenRemaining1()
		{
			int[] budget = SpaceTravelContent.CalculateEliminationBudget(
				startCount: 100,
				minPerStage: DEFAULT_MIN_PER_STAGE,
				stageCount: 7,
				randomRange: (min, max) => min  // 항상 최솟값: remaining=1, add=0
			);

			int sum = 0;
			foreach (int v in budget)
			{
				sum += v;
			}

			Assert.AreEqual(99, sum); // 100 - 1 = 99명 탈락
		}

		[Test]
		public void Budget_EachStageAtLeastMin()
		{
			int[] budget = SpaceTravelContent.CalculateEliminationBudget(
				startCount: 100,
				minPerStage: DEFAULT_MIN_PER_STAGE,
				stageCount: 7,
				randomRange: (min, max) => min
			);

			for (int i = 0; i < 7; i++)
			{
				Assert.GreaterOrEqual(budget[i], DEFAULT_MIN_PER_STAGE[i],
					$"Stage {i + 1}: budget {budget[i]} < min {DEFAULT_MIN_PER_STAGE[i]}");
			}
		}

		[Test]
		public void Budget_StageCount_MatchesTargetStreakCount()
		{
			int[] budget = SpaceTravelContent.CalculateEliminationBudget(
				startCount: 100,
				minPerStage: DEFAULT_MIN_PER_STAGE,
				stageCount: 7,
				randomRange: (min, max) => min
			);

			Assert.AreEqual(7, budget.Length);
		}

		[Test]
		public void Budget_SumEqualsEliminated_WhenRemaining3()
		{
			int[] budget = SpaceTravelContent.CalculateEliminationBudget(
				startCount: 100,
				minPerStage: DEFAULT_MIN_PER_STAGE,
				stageCount: 7,
				randomRange: (min, max) => max - 1  // 항상 최댓값-1: remaining=3, add=maxAdd
			);

			int sum = 0;
			foreach (int v in budget)
			{
				sum += v;
			}

			Assert.AreEqual(97, sum); // 100 - 3 = 97명 탈락
		}
	}
}
