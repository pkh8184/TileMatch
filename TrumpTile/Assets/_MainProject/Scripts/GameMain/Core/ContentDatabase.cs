using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "TrumpTile/ContentDatabase")]
    public class ContentDatabase : ScriptableObject
    {
       [SerializeReference, SubclassSelector] private List<ContentBase> mContentBaseArray;

        private Dictionary<string, ContentBase> mContentMap;

        public void Initialize()
        {
            mContentMap = new Dictionary<string, ContentBase>();

            foreach(var item in mContentBaseArray)
            {
                mContentMap[item.ContentName] = item;
                Debug.Log($"컨텐츠 {item.ContentName} 맵에 추가");
                item.Initialize();
            }
        }
        public void Refresh()
        {
            if(mContentMap == null)
            {
                return;
            }
            foreach(var item in mContentMap)
            {
                item.Value.Refresh();
            }
        }
        public T GetContentData<T>(string contentName) where T : ContentBase
        {
            return mContentMap[contentName] as T;
        }
    }   
}

