using System.Collections.Generic;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 스테이지 클리어 구간과 구글 플레이 업적 ID의 매핑을 보관한다.
	/// 이 클래스의 책임은 "어느 구간에 어떤 업적이 걸려 있는가" 조회뿐이다.
	/// 실제 업적 보고는 <see cref="StageAchievementReporter"/>가 담당한다.
	///
	/// 업적 ID는 Assets/GPGSIds.cs와 동일한 값이지만 그대로 옮겨 적었다.
	/// GPGSIds.cs는 Assets 루트(기본 어셈블리)에 있어 GameManin asmdef에서 참조할 수 없기 때문이다.
	/// 플레이 콘솔에서 업적을 다시 만들면 아래 ID도 함께 갱신해야 한다.
	/// </summary>
	public static class StageAchievementTable
	{
		//구글 플레이 콘솔 업적 ID (괄호는 GPGSIds.cs의 대응 상수명)
		private const string ACHIEVEMENT_STAGE_10 = "CgkImtHB9JsCEAIQFQ";   // 어? 맞췄네!        (achievement)
		private const string ACHIEVEMENT_STAGE_20 = "CgkImtHB9JsCEAIQFg";   // 워밍업 끝          (achievement_2)
		private const string ACHIEVEMENT_STAGE_30 = "CgkImtHB9JsCEAIQFw";   // 손맛 좀 아는데?     (achievement_3)
		private const string ACHIEVEMENT_STAGE_50 = "CgkImtHB9JsCEAIQGA";   // 슬슬 중독되는 중    (achievement_4)
		private const string ACHIEVEMENT_STAGE_100 = "CgkImtHB9JsCEAIQGQ";  // 100판 돌파!        (achievement_100)
		private const string ACHIEVEMENT_STAGE_200 = "CgkImtHB9JsCEAIQGg";  // 꾸준함이 실력       (achievement_5)
		private const string ACHIEVEMENT_STAGE_300 = "CgkImtHB9JsCEAIQHA";  // 매칭 장인          (achievement_7)
		private const string ACHIEVEMENT_STAGE_500 = "CgkImtHB9JsCEAIQIw";  // 타일 끝판왕         (achievement_13)

		/// <summary>스테이지 구간 하나와 거기에 연결된 업적 ID.</summary>
		public struct Milestone
		{
			public readonly int Stage;
			public readonly string AchievementId;

			public Milestone(int stage, string achievementId)
			{
				Stage = stage;
				AchievementId = achievementId;
			}
		}

		//구간은 반드시 오름차순으로 유지한다. (GetAchievedIds가 순서대로 순회하며 조기 종료한다)
		private static readonly Milestone[] MILESTONES =
		{
			new Milestone(10, ACHIEVEMENT_STAGE_10),
			new Milestone(20, ACHIEVEMENT_STAGE_20),
			new Milestone(30, ACHIEVEMENT_STAGE_30),
			new Milestone(50, ACHIEVEMENT_STAGE_50),
			new Milestone(100, ACHIEVEMENT_STAGE_100),
			new Milestone(200, ACHIEVEMENT_STAGE_200),
			new Milestone(300, ACHIEVEMENT_STAGE_300),
			new Milestone(500, ACHIEVEMENT_STAGE_500),
		};

		public static int MilestoneCount => MILESTONES.Length;

		public static IReadOnlyList<Milestone> Milestones => MILESTONES;

		/// <summary>
		/// 최고 클리어 스테이지 기준으로 달성한 모든 업적 ID를 반환한다.
		/// 달성분 전체를 돌려주므로, 뒤늦게 붙은 업적도 다음 클리어 때 한 번에 보고된다.
		/// </summary>
		public static List<string> GetAchievedIds(int maxClearedStage)
		{
			List<string> achievedIds = new List<string>();

			for(int i = 0; i < MILESTONES.Length; i++)
			{
				if(maxClearedStage < MILESTONES[i].Stage)
				{
					break;
				}

				achievedIds.Add(MILESTONES[i].AchievementId);
			}

			return achievedIds;
		}
	}
}
