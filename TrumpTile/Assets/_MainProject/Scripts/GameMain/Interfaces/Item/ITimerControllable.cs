namespace TrumpTile.GameMain.Item
{
	// GameManager가 구현. MagicWandItem이 타이머 정지를 요청할 때 사용
	public interface ITimerControllable
	{
		void FreezeTimer(float seconds);
	}
}
