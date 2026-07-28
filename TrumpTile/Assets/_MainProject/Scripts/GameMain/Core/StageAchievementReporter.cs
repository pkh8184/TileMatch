using System.Collections.Generic;
using GooglePlayGames;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 스테이지 업적을 구글 플레이에 보고한다.
	/// 이 클래스의 책임은 GPGS 호출뿐이다. 어떤 구간에 어떤 업적이 걸렸는지는
	/// <see cref="StageAchievementTable"/>이 결정한다.
	/// </summary>
	public static class StageAchievementReporter
	{
		//GPGS 표준 업적은 진행도 100%를 보고하면 해금된다.
		private const double UNLOCK_PROGRESS = 100.0;

		/// <summary>
		/// 최고 클리어 스테이지 기준으로 달성한 업적을 모두 해금 보고한다.
		/// 이미 해금된 업적을 다시 보고해도 GPGS가 무시하므로, 매번 전체를 보내도 안전하다.
		/// (덕분에 업적 도입 이전부터 진행한 유저도 다음 클리어 때 소급 해금된다)
		/// </summary>
		public static void ReportAchievedStages(int maxClearedStage)
		{
			List<string> achievedIds = StageAchievementTable.GetAchievedIds(maxClearedStage);
			if(achievedIds.Count == 0)
			{
				return;
			}

			if(!IsAvailable())
			{
				Debug.Log("[StageAchievementReporter] 구글 플레이 미인증 상태. 업적 보고를 건너뜁니다.");
				return;
			}

			for(int i = 0; i < achievedIds.Count; i++)
			{
				Unlock(achievedIds[i]);
			}
		}

		private static bool IsAvailable()
		{
			//에디터/미지원 플랫폼에서는 GPGS 인스턴스가 더미라 인증이 잡히지 않는다.
			return PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();
		}

		private static void Unlock(string achievementId)
		{
			if(string.IsNullOrEmpty(achievementId))
			{
				return;
			}

			PlayGamesPlatform.Instance.ReportProgress(achievementId, UNLOCK_PROGRESS, bSuccess =>
			{
				if(!bSuccess)
				{
					Debug.LogWarning($"[StageAchievementReporter] 업적 보고 실패: {achievementId}");
				}
			});
		}
	}
}
