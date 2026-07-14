using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using TrumpTile.LevelEditor.Editor;
using TrumpTile.FrameLibrary;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class DailyPuzzleEntry
	{
		public AssetReferenceT<LevelData> levelDataRef;
	}

	[CreateAssetMenu(fileName = "DailyPuzzleTable", menuName = "TrumpTile/Data/Daily Puzzle Table")]
	public class DailyPuzzleTable : ScriptableObject
	{
		public string startDate = "2026-01-01";
		public int dailyRewardGold = 500;
		public List<DailyPuzzleEntry> entries = new List<DailyPuzzleEntry>();

		/// <summary>
		/// 날짜 기반 셔플백 인덱스 — startDate 무관, 날짜를 시드로 사용.
		/// 한 사이클(entryCount일) 동안 모든 엔트리가 한 번씩 등장하고,
		/// 사이클 경계에서 직전 날과 같은 레벨이 나오지 않게 보정한다.
		/// static으로 분리하여 테스트 가능.
		/// </summary>
		public static int CalculateIndex(DateTime targetDate, int entryCount)
		{
			if (entryCount <= 1)
			{
				return 0;
			}

			// 2개짜리는 교대(0,1,0,1)만이 연속 중복 없는 유일한 수열
			if (entryCount == 2)
			{
				return (int)(targetDate.Date.Ticks / TimeSpan.TicksPerDay % 2);
			}

			long absoluteDay = targetDate.Date.Ticks / TimeSpan.TicksPerDay;
			long cycle = absoluteDay / entryCount;
			int pos = (int)(absoluteDay % entryCount);

			int[] thisCycle = ShuffledBag(entryCount, cycle);

			// 사이클 경계: 직전 사이클 마지막과 이번 사이클 첫 칸이 같으면
			// 첫 두 칸을 맞바꿔 연속 중복을 막는다. pos와 무관하게 동일 보정이라
			// 사이클 내 "모든 엔트리 한 번씩" 성질은 유지된다.
			int prevLast = ShuffledBag(entryCount, cycle - 1)[entryCount - 1];
			if (thisCycle[0] == prevLast)
			{
				int temp = thisCycle[0];
				thisCycle[0] = thisCycle[1];
				thisCycle[1] = temp;
			}

			return thisCycle[pos];
		}

		/// <summary>
		/// 사이클 번호를 시드로 [0..count-1]을 결정론적으로 셔플(Fisher-Yates)
		/// </summary>
		private static int[] ShuffledBag(int count, long cycleSeed)
		{
			int[] bag = new int[count];
			for (int i = 0; i < count; i++)
			{
				bag[i] = i;
			}

			System.Random rng = new System.Random((int)(cycleSeed * 31 + 7));
			for (int i = count - 1; i > 0; i--)
			{
				int j = rng.Next(0, i + 1);
				int temp = bag[i];
				bag[i] = bag[j];
				bag[j] = temp;
			}

			return bag;
		}

		public DailyPuzzleEntry GetTodayEntry()
		{
			if (entries == null || entries.Count == 0)
			{
				return null;
			}

			int index = CalculateIndex(GameTime.Today, entries.Count);
			return entries[index];
		}
		public int GetTodayIndex()
		{
			return CalculateIndex(GameTime.Today, entries.Count);
		}
	}
}
