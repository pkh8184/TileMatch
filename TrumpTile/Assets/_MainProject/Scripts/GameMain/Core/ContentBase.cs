using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public abstract class ContentBase
    {
        [Header("컨텐츠 이름")]
        [SerializeField] private string mContentName;
        [Header("컨텐츠 해금 레벨")]
        [SerializeField] private int mLevelToUnlock;

        //컨텐츠 잠금 플래그
        protected bool mbIsUnlocked;
        //레드닷 플래그
        protected bool mbHasNewthing;
        
        public string ContentName => mContentName;
        public bool HasNewThing => mbHasNewthing;
        public void SetUnlock()
        {
            mbIsUnlocked = true;

            InitOnUnlock();
        }
        public virtual void Initialize()
        {
            //플레이어 데이터의 컨텐츠 맵에서 컨텐츠 해금 데이터 읽어오기
            //mbIsUnlokced = PlayerDataManager.
        }
        protected virtual void InitOnUnlock()
        {
            
        }
        
    }    
}

