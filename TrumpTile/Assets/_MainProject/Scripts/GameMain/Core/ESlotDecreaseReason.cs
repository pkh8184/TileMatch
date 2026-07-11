namespace TrumpTile.GameMain.Core
{
	/// <summary>슬롯 개수가 변한 사유. 클러치는 Match로 0이 될 때만 발동한다.</summary>
	public enum ESlotDecreaseReason
	{
		None,   // 증가 또는 사유 없음
		Match,  // 매치로 제거
		Item,   // 아이템으로 보드 복귀
		Revive  // 부활로 보드 복귀
	}
}
