namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// EventManager 오디오 이벤트 호출 헬퍼
	/// 호출부에서 EventManager 직접 참조 없이 간결하게 사용
	/// 예) AudioEvent.Play(EAudioKey.SFX_BtnClick);
	/// </summary>
	public static class AudioEvent
	{
		public static void Play(EAudioKey key)
		{
			EventManager.Inst?.ActiveEvent<AudioEventPayload>(
				EventKeys.AUDIO_PLAY,
				new AudioEventPayload(EAudioCommand.Play, key)
			);
		}

		public static void Pause(EAudioKey key = default)
		{
			EventManager.Inst?.ActiveEvent<AudioEventPayload>(
				EventKeys.AUDIO_PAUSE,
				new AudioEventPayload(EAudioCommand.Pause, key)
			);
		}

		public static void Stop(EAudioKey key = default)
		{
			EventManager.Inst?.ActiveEvent<AudioEventPayload>(
				EventKeys.AUDIO_STOP,
				new AudioEventPayload(EAudioCommand.Stop, key)
			);
		}

		public static void Resume(EAudioKey key = default)
		{
			EventManager.Inst?.ActiveEvent<AudioEventPayload>(
				EventKeys.AUDIO_RESUME,
				new AudioEventPayload(EAudioCommand.Resume, key)
			);
		}
	}
}
