using NUnit.Framework;
using TrumpTile.GameMain.Data;
using System.Collections.Generic;

namespace TrumpTile.Tests.EditMode
{
	public class RemoveAdsSlotTests
	{
		[Test]
		public void IsAdsRemoved_ReturnsFalse_WhenRemoveAdsNotSet()
		{
			UserData userData = new UserData();
			userData.RemoveAds = false;

			bool bResult = userData.RemoveAds;

			Assert.IsFalse(bResult);
		}

		[Test]
		public void IsAdsRemoved_ReturnsTrue_WhenRemoveAdsSet()
		{
			UserData userData = new UserData();
			userData.RemoveAds = true;

			bool bResult = userData.RemoveAds;

			Assert.IsTrue(bResult);
		}
	}
}
