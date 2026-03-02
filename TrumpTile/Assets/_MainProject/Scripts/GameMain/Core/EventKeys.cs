namespace TrumpTile.GameMain.Core
{
	public static class EventKeys
	{
		// SlotManager → 타일 매치 발생
		public const string MATCH_OCCURRED = "Match_Occurred";

		// ComboSystem → 콤보 레이블 발동 (페이로드: ComboTriggeredPayload)
		public const string COMBO_TRIGGERED = "Combo_Triggered";

		// ComboSystem → 콤보 타이머 만료로 연속 카운트 초기화
		public const string COMBO_RESET = "Combo_Reset";
	}
}
