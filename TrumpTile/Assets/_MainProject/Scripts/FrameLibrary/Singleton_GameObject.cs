using UnityEngine;

namespace TrumpTile.FrameLibrary
{
	public abstract class Singleton_GameObject<T> : MonoBehaviour where T : Component
	{
		private static T mInst;
		private static bool mbIsQuitting = false;

		public static T Inst
		{
			get
			{
				if (mbIsQuitting)
				{
					return null;
				}

				if (mInst == null)
				{
					string objName = typeof(T).Name;
					T singletonComp = GameObject.FindObjectOfType<T>();

					if (singletonComp != null)
					{
						mInst = singletonComp;
					}
					else
					{
						GameObject singletonObj = new GameObject(objName);
						mInst = singletonObj.AddComponent<T>();
					}
				}

				return mInst;
			}
		}

		private void OnApplicationQuit()
		{
			mbIsQuitting = true;
		}
	}
}
