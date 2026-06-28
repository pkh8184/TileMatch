using NUnit.Framework;
using System;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.Tests.EditMode
{
	public class DailyPuzzleTests
	{
		[Test]
		public void CalculateIndex_SameDate_ReturnsSameIndex()
		{
			DateTime date = new DateTime(2026, 6, 24);
			int result1 = DailyPuzzleTable.CalculateIndex(date, 5);
			int result2 = DailyPuzzleTable.CalculateIndex(date, 5);
			Assert.AreEqual(result1, result2);
		}

		[Test]
		public void CalculateIndex_AlwaysInRange()
		{
			DateTime date = new DateTime(2026, 1, 1);
			for (int count = 2; count <= 8; count++)
			{
				for (int i = 0; i < 200; i++)
				{
					int result = DailyPuzzleTable.CalculateIndex(date.AddDays(i), count);
					Assert.IsTrue(result >= 0 && result < count,
						"count " + count + " day " + i + " out of range: " + result);
				}
			}
		}

		[Test]
		public void CalculateIndex_NoConsecutiveDuplicate()
		{
			DateTime date = new DateTime(2026, 1, 1);
			for (int count = 2; count <= 8; count++)
			{
				int prev = DailyPuzzleTable.CalculateIndex(date, count);
				for (int i = 1; i < 200; i++)
				{
					int current = DailyPuzzleTable.CalculateIndex(date.AddDays(i), count);
					Assert.AreNotEqual(prev, current,
						"count " + count + " consecutive duplicate at day " + i);
					prev = current;
				}
			}
		}

		[Test]
		public void CalculateIndex_CoversAllEntriesPerCycle()
		{
			// count>=3: 한 사이클(count일) 동안 모든 엔트리가 정확히 한 번씩
			DateTime date = new DateTime(2026, 1, 1);
			for (int count = 3; count <= 8; count++)
			{
				// 사이클 시작(absoluteDay % count == 0)에 맞춰 윈도우 정렬
				long absoluteDay = date.Date.Ticks / TimeSpan.TicksPerDay;
				int alignOffset = (int)((count - absoluteDay % count) % count);
				DateTime cycleStart = date.AddDays(alignOffset);

				bool[] seen = new bool[count];
				for (int i = 0; i < count; i++)
				{
					int result = DailyPuzzleTable.CalculateIndex(cycleStart.AddDays(i), count);
					Assert.IsFalse(seen[result],
						"count " + count + " duplicate within cycle: " + result);
					seen[result] = true;
				}
			}
		}

		[Test]
		public void CalculateIndex_SingleEntry_ReturnsZero()
		{
			int result = DailyPuzzleTable.CalculateIndex(new DateTime(2026, 1, 1), 1);
			Assert.AreEqual(0, result);
		}

		[Test]
		public void CheckIsTodayCleared_ReturnsFalse_WhenKeyAbsent()
		{
			string key = "DailyCleared_" + DateTime.Today.ToString("yyyy-MM-dd");
			PlayerPrefs.DeleteKey(key);

			Assert.IsFalse(DailyPuzzleManager.CheckIsTodayCleared());
		}

		[Test]
		public void CheckIsTodayCleared_ReturnsTrue_WhenKeyPresent()
		{
			string key = "DailyCleared_" + DateTime.Today.ToString("yyyy-MM-dd");
			PlayerPrefs.SetInt(key, 1);

			bool bResult = DailyPuzzleManager.CheckIsTodayCleared();

			PlayerPrefs.DeleteKey(key);
			Assert.IsTrue(bResult);
		}

		[Test]
		public void CalculateBackgroundIndex_SameDate_ReturnsSameIndex()
		{
			DateTime date = new DateTime(2026, 6, 24);
			int result1 = DailyPuzzleManager.CalculateBackgroundIndex(date, 5);
			int result2 = DailyPuzzleManager.CalculateBackgroundIndex(date, 5);
			Assert.AreEqual(result1, result2);
		}

		[Test]
		public void CalculateBackgroundIndex_ReturnsInRange()
		{
			DateTime date = new DateTime(2026, 6, 24);
			int result = DailyPuzzleManager.CalculateBackgroundIndex(date, 5);
			Assert.IsTrue(result >= 0 && result < 5);
		}

		[Test]
		public void CalculateBackgroundIndex_DifferentDates_EachInRange()
		{
			int result1 = DailyPuzzleManager.CalculateBackgroundIndex(new DateTime(2026, 6, 24), 5);
			int result2 = DailyPuzzleManager.CalculateBackgroundIndex(new DateTime(2026, 6, 25), 5);
			Assert.IsTrue(result1 >= 0 && result1 < 5);
			Assert.IsTrue(result2 >= 0 && result2 < 5);
		}
	}
}
