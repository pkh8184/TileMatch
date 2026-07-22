using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.FrameLibrary;
using System.Collections;
using System;

namespace TrumpTile.GameMain.UI
{
	public class LeaderboardView : ViewBase
	{
		private const int LEADERBOARD_COUNT = 100;

		[Header("스크롤 영역")]
		[SerializeField] private Transform mScrollContent;
		[SerializeField] private LeaderboardEntryView mEntryPrefab;

		[Header("내 순위 (하단 고정)")]
		[SerializeField] private LeaderboardEntryView mMyEntryView;

		[Header("해금 팝업 프리팹")]
		[SerializeField] private GameObject mUnlockPopupPrefab;

		[SerializeField] private TemporaryContentUIController mUIController;

		private List<LeaderboardEntryView> mEntryViews = new List<LeaderboardEntryView>();

		private int mMyRank = 0;
        public override void Initialize()
        {
            base.Initialize();

			if(PlayerDataManager.Inst.IsChampionsActive)
			{
				if(!PlayerDataManager.Inst.UserData.ChampionsUnlock)
				{
					MainManager.Instance.AddEvent(Co_MainSceneEnterEvent, EMainSceneEventType.UnlockContent);
					PlayerDataManager.Inst.UnlockChampions();
				}
				DateTime endOfMonth = new DateTime(GameTime.Now.Year, GameTime.Now.Month, DateTime.DaysInMonth(GameTime.Now.Year, GameTime.Now.Month), 23, 59, 59);
				float secondsLeft = (float)(endOfMonth - GameTime.Now).TotalSeconds;
				mUIController.SetLimitTimeText(secondsLeft);

				InitLeaderBoard();
			}
			else
			{
				mShowButton.gameObject.SetActive(false);
			}

        }
		private IEnumerator Co_MainSceneEnterEvent()
        {
            GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
            UIBase ui = obj.GetComponent<UIBase>();
            ui.Initialize();
            ui.Show();

            yield return new WaitWhile(() => obj.activeSelf);
        }
		public override void Show()
		{
			mMyEntryView.SetMyData(mMyRank);
			AdManager.Inst.HideBannerAd();
			base.Show();
		}
        public override void Hide()
        {
            base.Hide();
			AdManager.Inst.ShowBannerAd();
        }
		// private async Task LoadLeaderboardAsync()
		// {
		// 	ClearEntries();

		// 	LeaderboardResult result = await LeaderboardManager.Inst.GetLeaderboardAsync(LEADERBOARD_COUNT);

		// 	if (result == null)
		// 	{
		// 		return;
		// 	}

		// 	PopulateEntries(result);
		// }
		private void InitLeaderBoard()
		{
			List<TBLeaderNameData> list = LeaderboardManager.Inst.GetRankerList();

			for(int i = 0; i < 100; i++)
			{
				LeaderboardEntryView view = Instantiate(mEntryPrefab, mScrollContent);
				if(list[i] == null)
				{
					view.SetMyData(i + 1);
					mMyRank = i + 1;
				}
				else
				{
					view.SetData(list[i], i + 1);
				}
			}
			if(mMyRank == 0)
			{
				mMyRank = LeaderboardManager.Inst.FindMyRank();
				mMyEntryView.SetMyData(mMyRank);
			}

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
				//view.SetData(entry, bIsMyEntry);
				mEntryViews.Add(view);
			}

			if (mMyEntryView != null)
			{
				mMyEntryView.gameObject.SetActive(result.myEntry != null);
				if (result.myEntry != null)
				{
					//mMyEntryView.SetData(result.myEntry, true);
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
