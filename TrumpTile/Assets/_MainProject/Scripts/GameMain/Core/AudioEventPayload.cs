namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 오디오 이벤트 페이로드
	/// </summary>
	public struct AudioEventPayload
	{
		public EAudioCommand Command;
		public EAudioKey Key;

		public AudioEventPayload(EAudioCommand command, EAudioKey key)
		{
			Command = command;
			Key = key;
		}
	}
}
