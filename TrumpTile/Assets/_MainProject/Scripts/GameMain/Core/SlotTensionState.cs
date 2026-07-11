namespace TrumpTile.GameMain.Core
{
	public enum ETensionEvent
	{
		None,
		EnterDanger,
		ExitDanger,
		ClutchSave
	}

	/// <summary>
	/// 슬롯 위험/클러치 판정 상태머신 (Unity 의존 없음).
	/// 위험 = 1칸 남음(count == max - 1). 클러치 = 위험 도달 후 매치로 0이 될 때, 스테이지당 1회.
	/// </summary>
	public class SlotTensionState
	{
		private bool mbInDanger;
		private bool mbReachedDanger;
		private bool mbClutchFired;

		public void Reset()
		{
			mbInDanger = false;
			mbReachedDanger = false;
			mbClutchFired = false;
		}

		public ETensionEvent Evaluate(int count, int max, ESlotDecreaseReason reason)
		{
			bool bDangerNow = count == max - 1;

			if (bDangerNow && !mbInDanger)
			{
				mbInDanger = true;
				mbReachedDanger = true;
				return ETensionEvent.EnterDanger;
			}

			if (!bDangerNow && mbInDanger)
			{
				mbInDanger = false;

				if (count == 0 && reason == ESlotDecreaseReason.Match && mbReachedDanger && !mbClutchFired)
				{
					mbClutchFired = true;
					return ETensionEvent.ClutchSave;
				}
				return ETensionEvent.ExitDanger;
			}

			if (count == 0 && reason == ESlotDecreaseReason.Match && mbReachedDanger && !mbClutchFired)
			{
				mbClutchFired = true;
				return ETensionEvent.ClutchSave;
			}

			return ETensionEvent.None;
		}
	}
}
