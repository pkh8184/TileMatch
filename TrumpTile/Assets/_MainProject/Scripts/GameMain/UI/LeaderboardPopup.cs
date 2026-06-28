using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
	public class LeaderboardPopup : PopupBase
	{
		private const int LEADERBOARD_COUNT = 100;

		[Header("스크롤 영역")]
		[SerializeField] private Transform mScrollContent;
		[SerializeField] private LeaderboardEntryView mEntryPrefab;

		[Header("내 순위 (하단 고정)")]
		[SerializeField] private LeaderboardEntryView mMyEntryView;

		private List<LeaderboardEntryView> mEntryViews = new List<LeaderboardEntryView>();

		public override void Show()
		{
			base.Show();
			_ = LoadLeaderboardAsync();
		}

		private async Task LoadLeaderboardAsync()
		{
			ClearEntries();

			LeaderboardResult result = await LeaderboardManager.Inst.GetLeaderboardAsync(LEADERBOARD_COUNT);

			if (result == null)
			{
				return;
			}

			PopulateEntries(result);
		}

		private void PopulateEntries(LeaderboardResult result)
		{
			HashSet<int> myRankSet = new HashSet<int>();
			if (result.myEntry != null)
			{
				myRankSet.Add(result.myEntry.rank);
			}

			foreach (LeaderboardEntryData entry in result.topN)
			{
				bool bIsMyEntry = myRankSet.Contains(entry.rank);
				LeaderboardEntryView view = Instantiate(mEntryPrefab, mScrollContent);
				view.SetData(entry, bIsMyEntry);
				mEntryViews.Add(view);
			}

			if (mMyEntryView != null)
			{
				mMyEntryView.gameObject.SetActive(result.myEntry != null);
				if (result.myEntry != null)
				{
					mMyEntryView.SetData(result.myEntry, true);
				}
			}
		}

		private void ClearEntries()
		{
			foreach (LeaderboardEntryView view in mEntryViews)
			{
				Destroy(view.gameObject);
			}
			mEntryViews.Clear();
		}

	}
}
