using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using System;

namespace TrumpTile.GameMain.Core
{
	public class EventManager : Singleton_GameObject<EventManager>
	{
		//Event
		private Dictionary<string, Action<object>> mEvents = new Dictionary<string, Action<object>>();

		#region 초기화 & OnDestroy

		private void Awake()
		{
		}

		public void OnDestroy()
		{
			mEvents.Clear();
		}

		#endregion

		#region Event 처리

		//이벤트 추가
		public void AddEvent(string eventKey, Action<object> action)
		{
			//이벤트 있으면 제거 후 추가
			if (mEvents.ContainsKey(eventKey))
			{
				mEvents.Remove(eventKey);
			}

			mEvents.Add(eventKey, action);
		}

		//이벤트 있으면 제거
		public void RemoveEvent(string eventKey)
		{
			if (mEvents.ContainsKey(eventKey) == false)
			{
				return;
			}

			mEvents.Remove(eventKey);
		}

		//이벤트 있으면 실행
		public void ActiveEvent<T>(string eventKey, T parameter)
		{
			if (mEvents.ContainsKey(eventKey) == false)
			{
				return;
			}

			mEvents[eventKey].Invoke(parameter);
		}

		#endregion
	}
}
