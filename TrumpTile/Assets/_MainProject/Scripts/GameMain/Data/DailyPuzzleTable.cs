using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using TrumpTile.LevelEditor.Editor;

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
		/// 날짜 기반 인덱스 계산 — static으로 분리하여 테스트 가능
		/// </summary>
		public static int CalculateIndex(DateTime startDate, DateTime targetDate, int entryCount)
		{
			if (entryCount <= 0)
			{
				return 0;
			}

			int days = (targetDate.Date - startDate.Date).Days;
			if (days < 0)
			{
				days = 0;
			}

			return days % entryCount;
		}

		public DailyPuzzleEntry GetTodayEntry()
		{
			if (entries == null || entries.Count == 0)
			{
				return null;
			}

			if (!DateTime.TryParse(startDate, out DateTime parsedDate))
			{
				Debug.LogError("[DailyPuzzleTable] Invalid startDate format: " + startDate);
				return null;
			}

			int index = CalculateIndex(parsedDate, DateTime.Today, entries.Count);
			return entries[index];
		}
	}
}
