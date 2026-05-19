using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class ContentBase : ScriptableObject
    {
        [SerializeField] private string mContentName;
        protected bool mbIsUnlocked;
        public string ContentName => mContentName;
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

