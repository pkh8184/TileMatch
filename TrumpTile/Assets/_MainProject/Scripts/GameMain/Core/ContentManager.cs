using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrumpTile.FrameLibrary;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace TrumpTile.GameMain.Core
{
    public class ContentManager : Singleton_GameObject<ContentManager>
    {
        private bool mbWasInit;
        [SerializeField] private ContentDatabase mContentDatabase;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy()
        {
            EventManager.Inst?.RemoveEvent(EventKeys.CONTENT_DATA_REFRESH, Refresh);
        }
        protected override void InitOnCreated()
        {
            base.InitOnCreated();

            Addressables.LoadAssetAsync<ContentDatabase>("ContentDatabase").Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    mContentDatabase = handle.Result;
                    Debug.Log("컨텐츠데이터베이스 읽어옴");
                }
            };

            EventManager.Inst.AddEvent(EventKeys.CONTENT_DATA_REFRESH, Refresh);
        }
        public async Task Initialize()
        {
            if(mbWasInit)
            {
                mContentDatabase.Refresh();
                return;
            }
            while(mContentDatabase == null)
            {
                await Task.Yield();
            }

            mbWasInit = true;
            mContentDatabase.Initialize();

            return;  
        }
        private void Refresh()
        {
            mContentDatabase.Refresh();
        }
        public T GetContentData<T>(string contentName) where T : ContentBase
        {
            if(mContentDatabase == null)
            {
                return null;
            }
            return mContentDatabase.GetContentData<T>(contentName);
        }
    }    
}

