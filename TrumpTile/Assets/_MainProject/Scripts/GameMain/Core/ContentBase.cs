using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrumpTile.GameMain.Core
{
    public class ContentInfo
    {
        public float ActiveTime;
    }
    [System.Serializable]
    public abstract class ContentBase
    {
        [Header("컨텐츠 이름")]
        [SerializeField] private string mContentName;
        [Header("컨텐츠 해금 레벨")]
        [SerializeField] protected int mLevelToUnlock;
        //컨텐츠 잠금 플래그
        protected bool mbIsUnlocked = false;
        //레드닷 플래그
        protected bool mbHasNewthing = false;
        //해금 팝업 플래그
        protected bool mbShowUnlockPopup = false;

        public string ContentName => mContentName;
        public bool HasNewThing => mbHasNewthing;
        public bool Unlock => mbIsUnlocked;
        public bool ShowUnlockPopup => mbShowUnlockPopup;
        public virtual void Initialize()
        {
            //플레이어 데이터의 컨텐츠 맵에서 컨텐츠 해금 데이터 읽어오기
            CheckUnlock();
        }
        public virtual void Refresh()
        {
            //모든 컨텐츠는 메인씬에서 접근하기 때문에
            //메인씬이 호출될 때마다 플레이어의 컨텐츠 관련 정보를 읽어와 갱신해줘야함
            CheckUnlock();
        }
        public virtual void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();
            }
        }
        public virtual ContentInfo GetContentInfo()
        {
            return new ContentInfo();
        }
        protected void SetUnlock()
        {
            mbIsUnlocked = true;

            OnUnlock();
        }
        protected virtual void OnUnlock()
        {
            
        }
    }    
}

