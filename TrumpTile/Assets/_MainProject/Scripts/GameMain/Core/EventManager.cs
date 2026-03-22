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

		private void OnDestroy()
		{
			mEvents.Clear();
		}

		#endregion

		#region Event 처리

		//이벤트 추가
		public void AddEvent(string eventKey, Action<object> action)
		{
			if (mEvents.ContainsKey(eventKey))
			{
				mEvents[eventKey] -= action;
				mEvents[eventKey] += action;
			}
			else
				mEvents.Add(eventKey, action);
		}

		//특정 콜백만 제거
		public void RemoveEvent(string eventKey, Action<object> action)
		{
			if (mEvents.ContainsKey(eventKey) == false)
				return;

			mEvents[eventKey] -= action;

			if (mEvents[eventKey] == null)
				mEvents.Remove(eventKey);
		}

		//키에 등록된 모든 콜백 제거
		public void RemoveEvent(string eventKey)
		{
			if (mEvents.ContainsKey(eventKey) == false)
				return;

			mEvents.Remove(eventKey);
		}

		//이벤트 있으면 실행
		public void ActiveEvent<T>(string eventKey, T parameter)
		{
			if (mEvents.ContainsKey(eventKey) == false)
				return;

			mEvents[eventKey].Invoke(parameter);
		}

		//파라미터 없는 이벤트 실행
		public void ActiveEvent(string eventKey)
		{
			ActiveEvent<object>(eventKey, null);
		}

		#endregion
	}
}
