using UnityEngine;

namespace TrumpTile.FrameLibrary
{
    public abstract class Singleton_GameObject<T> : MonoBehaviour where T : Component
    {
        private static T _inst;
        public static T Inst
        {
            get
            {
                if (_inst == null)
                {
                    string objName = typeof(T).Name;
                    T singletonComp = GameObject.FindObjectOfType<T>();

                    if (singletonComp != null)
                    {
                        _inst = singletonComp;
                    }
                    else
                    {
                        GameObject singletonObj = null;
                        if (singletonObj == null)
                            singletonObj = new GameObject(objName);

                        _inst = singletonObj.AddComponent<T>();
                    }
                }

                return _inst;
            }
        }
    }
}