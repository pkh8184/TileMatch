using System;
using System.Threading.Tasks;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using TrumpTile.LevelEditor.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TrumpTile.GameMain.Core
{
	public class DailyPuzzleManager : Singleton_GameObject<DailyPuzzleManager>
	{
		private const string DAILY_PUZZLE_TABLE_ADDRESS = "DailyPuzzleTable";
		private const string DAILY_CLEARED_KEY_PREFIX = "DailyCleared_";

		[Header("Debug")]
		[SerializeField] private bool mbUseDateOverride = false;
		[SerializeField] private string mDateOverride = "20260625";
		private DailyPuzzleTable mDailyPuzzleTable;
		private AsyncOperationHandle<DailyPuzzleTable> mTableHandle;
		private bool mbIsActive = false;
		private bool mbIsDailyPuzzlePlayEarly = false;
		private bool mbIsInitialized = false;
		public bool IsActive => mbIsActive;
		public bool IsDailyPuzzlePlayEarly => mbIsDailyPuzzlePlayEarly;
		public void SetDailyPuzzlePlayEarlyFalse() => mbIsDailyPuzzlePlayEarly = false;
		public bool IsInitialized => mbIsInitialized;
		public bool IsTodayCleared => CheckIsTodayCleared();

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		public static bool CheckIsTodayCleared()
		{
			string key = DAILY_CLEARED_KEY_PREFIX + GameTime.Today.ToString("yyyy-MM-dd");
			return PlayerPrefs.GetInt(key, 0) == 1;
		}

		private void OnDestroy()
		{
			if (mTableHandle.IsValid())
			{
				Addressables.Release(mTableHandle);
			}
		}

		public async Task InitializeAsync()
		{
			if (mbIsInitialized)
			{
				return;
			}

			mTableHandle = Addressables.LoadAssetAsync<DailyPuzzleTable>(DAILY_PUZZLE_TABLE_ADDRESS);
			await mTableHandle.Task;

			if (mTableHandle.Status == AsyncOperationStatus.Succeeded)
			{
				mDailyPuzzleTable = mTableHandle.Result;
				mbIsInitialized = true;
				Debug.Log("[DailyPuzzleManager] Initialized");
			}
			else
			{
				Debug.LogError("[DailyPuzzleManager] DailyPuzzleTable load failed!");
			}
		}

		public AssetReferenceT<LevelData> GetTodayAssetRef()
		{
			return mDailyPuzzleTable?.GetTodayEntry()?.levelDataRef;
		}

		public void EnterDailyPuzzle()
		{
			if (!mbIsInitialized)
			{
				Debug.LogWarning("[DailyPuzzleManager] Not initialized yet");
				return;
			}

			if (IsTodayCleared)
			{
				Debug.LogWarning("[DailyPuzzleManager] Today already cleared");
				return;
			}

			mbIsActive = true;
			mbIsDailyPuzzlePlayEarly = true;
			SceneTransister.Inst.TransistScene("GameScene");
		}

		public void OnDailyClear()
		{
			string key = DAILY_CLEARED_KEY_PREFIX + GameTime.Today.ToString("yyyy-MM-dd");
			PlayerPrefs.SetInt(key, 1);
			PlayerPrefs.Save();

			if (mDailyPuzzleTable != null)
			{
				PlayerDataManager.Inst.AddGold(mDailyPuzzleTable.dailyRewardGold);
				CoreContainer.RewardContainer.AddGold(mDailyPuzzleTable.dailyRewardGold);
			}

			mbIsActive = false;
			Debug.Log("[DailyPuzzleManager] Daily puzzle cleared!");
		}

		public void ExitDailyMode()
		{
			mbIsActive = false;
		}

		public static int CalculateBackgroundIndex(DateTime date, int arrayLength)
		{
			int seed = int.Parse(date.ToString("yyyyMMdd"));
			return new System.Random(seed).Next(0, arrayLength);
		}

		public int GetTodayRandomIndex(int arrayLength)
		{
			if (mbUseDateOverride && DateTime.TryParseExact(mDateOverride, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime overrideDate))
			{
				return CalculateBackgroundIndex(overrideDate, arrayLength);
			}
			return CalculateBackgroundIndex(GameTime.Today, arrayLength);
		}
		public DayOfWeek GetToday()
		{
			return GameTime.DayOfWeek;
		}
	}
}
