using System;
using System.Collections.Generic;
using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class AdManagerRewardedCloseOrderTests
	{
		[Test]
		public void InvokeRevivedThenReloadAd_CallsOnClosedBeforeReloadAd()
		{
			List<string> callOrder = new List<string>();

			AdManager.InvokeRevivedThenReloadAd(
				bRewardEarned: true,
				onClosed: (bDone) => callOrder.Add($"onClosed:{bDone}"),
				reloadAd: () => callOrder.Add("reloadAd"));

			Assert.AreEqual(new List<string> { "onClosed:True", "reloadAd" }, callOrder);
		}

		[Test]
		public void InvokeRevivedThenReloadAd_PassesRewardEarnedValueToOnClosed()
		{
			bool? bReceived = null;

			AdManager.InvokeRevivedThenReloadAd(
				bRewardEarned: false,
				onClosed: (bDone) => bReceived = bDone,
				reloadAd: () => { });

			Assert.IsFalse(bReceived.Value);
		}

		[Test]
		public void InvokeRevivedThenReloadAd_StillCallsReloadAd_WhenOnClosedThrows()
		{
			bool bReloadCalled = false;

			Assert.Throws<InvalidOperationException>(() =>
			{
				AdManager.InvokeRevivedThenReloadAd(
					bRewardEarned: true,
					onClosed: (bDone) => throw new InvalidOperationException("게임 재개 중 예외"),
					reloadAd: () => bReloadCalled = true);
			});

			Assert.IsTrue(bReloadCalled);
		}

		[Test]
		public void InvokeRevivedThenReloadAd_DoesNotThrow_WhenOnClosedIsNull()
		{
			bool bReloadCalled = false;

			Assert.DoesNotThrow(() =>
			{
				AdManager.InvokeRevivedThenReloadAd(
					bRewardEarned: true,
					onClosed: null,
					reloadAd: () => bReloadCalled = true);
			});

			Assert.IsTrue(bReloadCalled);
		}
	}
}
