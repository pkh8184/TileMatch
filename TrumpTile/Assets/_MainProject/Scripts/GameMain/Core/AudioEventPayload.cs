namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 오디오 이벤트 페이로드
	/// </summary>
	public struct AudioEventPayload
	{
		public EAudioCommand Command;
		public EAudioKey Key;

		/// <summary>
		/// 콤보 사운드처럼 인덱스가 필요한 경우 사용 (기본값 0)
		/// </summary>
		public int ComboIndex;

		public AudioEventPayload(EAudioCommand command, EAudioKey key, int comboIndex = 0)
		{
			Command = command;
			Key = key;
			ComboIndex = comboIndex;
		}
	}
}
