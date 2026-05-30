using NUnit.Framework;
using System;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.Tests.EditMode
{
	public class DailyPuzzleTests
	{
		[Test]
		public void CalculateIndex_OnStartDate_ReturnsZero()
		{
			int result = DailyPuzzleTable.CalculateIndex(
				new DateTime(2026, 1, 1),
				new DateTime(2026, 1, 1),
				3);
			Assert.AreEqual(0, result);
		}

		[Test]
		public void CalculateIndex_OnDay1_ReturnsOne()
		{
			int result = DailyPuzzleTable.CalculateIndex(
				new DateTime(2026, 1, 1),
				new DateTime(2026, 1, 2),
				3);
			Assert.AreEqual(1, result);
		}

		[Test]
		public void CalculateIndex_WrapsAround_AfterAllEntries()
		{
			// 3개 항목, 3일 후 → 인덱스 0으로 순환
			int result = DailyPuzzleTable.CalculateIndex(
				new DateTime(2026, 1, 1),
				new DateTime(2026, 1, 4),
				3);
			Assert.AreEqual(0, result);
		}

		[Test]
		public void CalculateIndex_BeforeStartDate_ReturnsZero()
		{
			int result = DailyPuzzleTable.CalculateIndex(
				new DateTime(2026, 1, 1),
				new DateTime(2025, 12, 31),
				3);
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
	}
}
