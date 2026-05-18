using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using System;
using System.Diagnostics;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class EventManager : Singleton_GameObject<EventManager>
	{
		//Event
		private Dictionary<string, Action<object>> mEvents = new Dictionary<string, Action<object>>();

		// 타입 안전 제네릭 이벤트
		private Dictionary<string, Delegate> mTypedEvents = new Dictionary<string, Delegate>();

		#region 초기화 & OnDestroy

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		private void OnDestroy()
		{
			mEvents.Clear();
			mTypedEvents.Clear();
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

		//이벤트 있으면 실행 (기존 object 기반 - 하위 호환)
		public void ActiveEvent(string eventKey, object parameter)
		{
			if (mEvents.ContainsKey(eventKey) == false)
			{
				return;
			}

			mEvents[eventKey].Invoke(parameter);
		}

		//파라미터 없는 이벤트 실행
		public void ActiveEvent(string eventKey)
		{
			ActiveEvent(eventKey, null);
		}

		#endregion

		#region 제네릭 타입 안전 Event 처리

		public void AddEvent<T>(string eventKey, Action<T> action)
		{
			if (mTypedEvents.ContainsKey(eventKey))
			{
				mTypedEvents[eventKey] = Delegate.Remove(mTypedEvents[eventKey], action);
				mTypedEvents[eventKey] = Delegate.Combine(mTypedEvents[eventKey], action);
			}
			else
			{
				mTypedEvents.Add(eventKey, action);
			}
		}

		public void RemoveEvent<T>(string eventKey, Action<T> action)
		{
			if (mTypedEvents.ContainsKey(eventKey) == false)
			{
				return;
			}

			mTypedEvents[eventKey] = Delegate.Remove(mTypedEvents[eventKey], action);

			if (mTypedEvents[eventKey] == null)
			{
				mTypedEvents.Remove(eventKey);
			}
		}

		public void ActiveEvent<T>(string eventKey, T payload)
		{
			if (mTypedEvents.ContainsKey(eventKey) == false)
			{
				return;
			}

			if (mTypedEvents[eventKey] is Action<T> action)
			{
				action.Invoke(payload);
			}
		}

		#endregion
	}
}
