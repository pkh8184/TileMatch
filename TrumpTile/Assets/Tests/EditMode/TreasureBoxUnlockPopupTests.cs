using NUnit.Framework;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class TreasureBoxUnlockPopupTests
	{
		[Test]
		public void ShouldAutoShowOnMainSceneEnter_WhenFirstUnlock_ReturnsTrue()
		{
			bool bResult = TreasureBoxContent.ShouldAutoShowOnMainSceneEnter(bUnlocked: true, bActive: true, bFirstUnlock: true);

			Assert.IsTrue(bResult);
		}

		[Test]
		public void ShouldAutoShowOnMainSceneEnter_WhenAlreadyUnlockedBefore_ReturnsFalse()
		{
			// 최초 해금 이후 메인씬에 다시 진입한 경우. 팝업이 반복 표시되던 버그의 재현 케이스.
			bool bResult = TreasureBoxContent.ShouldAutoShowOnMainSceneEnter(bUnlocked: true, bActive: true, bFirstUnlock: false);

			Assert.IsFalse(bResult);
		}

		[Test]
		public void ShouldAutoShowOnMainSceneEnter_WhenNotUnlocked_ReturnsFalse()
		{
			bool bResult = TreasureBoxContent.ShouldAutoShowOnMainSceneEnter(bUnlocked: false, bActive: true, bFirstUnlock: true);

			Assert.IsFalse(bResult);
		}

		[Test]
		public void ShouldAutoShowOnMainSceneEnter_WhenInCoolTime_ReturnsFalse()
		{
			// 구매 후 재활성화 쿨타임 중이면 해금 직후라도 표시하지 않는다.
			bool bResult = TreasureBoxContent.ShouldAutoShowOnMainSceneEnter(bUnlocked: true, bActive: false, bFirstUnlock: true);

			Assert.IsFalse(bResult);
		}
	}
}
