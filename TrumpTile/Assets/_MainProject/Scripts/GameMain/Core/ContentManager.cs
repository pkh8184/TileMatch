using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
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
                EvaluateTimedBuffs();
                mContentDatabase.Refresh();
                return;
            }
            while(mContentDatabase == null)
            {
                await Task.Yield();
            }

            mbWasInit = true;
            EvaluateTimedBuffs();
            mContentDatabase.Initialize();

            return;
        }
        private void Refresh()
        {
            EvaluateTimedBuffs();
            mContentDatabase.Refresh();
        }

        /// <summary>
        /// 컨텐츠 쿨타임과 같은 시점에, 컨텐츠에 속하지 않는 시간제 버프의 만료도 함께 재평가한다.
        /// (ContentDatabase를 거치지 않는 PlayerData 직속 버프들이 여기 모인다)
        /// </summary>
        private void EvaluateTimedBuffs()
        {
            PlayerDataManager.Inst?.EvaluateGemDoubleState();
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

