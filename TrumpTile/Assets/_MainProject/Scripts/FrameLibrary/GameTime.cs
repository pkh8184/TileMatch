using System;

namespace TrumpTile.FrameLibrary
{
	/// <summary>
	/// 게임 내 "현재 시간"의 단일 진입점.
	/// 쿨타임 등은 UtcNow, 하루경계(출석 등)는 Now/Today 를 사용한다.
	/// 에디터/개발빌드에서는 디버그 오프셋을 주입해 시간 흐름을 앞뒤로 옮길 수 있다.
	/// </summary>
	public static class GameTime
	{
		// 디버그용 시간 오프셋. 릴리즈 빌드에서는 항상 Zero.
		private static TimeSpan mDebugOffset = TimeSpan.Zero;

		public static DateTime UtcNow
		{
			get { return DateTime.UtcNow + mDebugOffset; }
		}

		public static DateTime Now
		{
			get { return DateTime.Now + mDebugOffset; }
		}

		public static DateTime Today
		{
			get { return Now.Date; }
		}

		public static DayOfWeek DayOfWeek
		{
			get { return Now.DayOfWeek; }
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		public static TimeSpan DebugOffset
		{
			get { return mDebugOffset; }
		}

		/// <summary>현재 오프셋에 더한다. (예: 하루 뒤 상황 재현)</summary>
		public static void AddOffset(TimeSpan span)
		{
			mDebugOffset += span;
		}

		/// <summary>오프셋을 특정 값으로 설정한다.</summary>
		public static void SetOffset(TimeSpan span)
		{
			mDebugOffset = span;
		}

		/// <summary>오프셋을 실제 시간으로 되돌린다.</summary>
		public static void Reset()
		{
			mDebugOffset = TimeSpan.Zero;
		}
#endif
	}
}
