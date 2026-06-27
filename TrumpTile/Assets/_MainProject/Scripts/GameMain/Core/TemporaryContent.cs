using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [Serializable]
    public class TemporaryContent : ContentBase
    {
        [Header("컨텐츠 제한시간")]
        [SerializeField] protected float mLimitTime;
        [Header("컨텐츠 종료 후 재시작까지 쿨타임")]
        [SerializeField] private float mCoolTime;

        //컨텐츠 활성화 플래그
        protected bool mbIsActive;

        public bool IsActive => mbIsActive;

        public override ContentInfo GetContentInfo()
        {
            return new ContentInfo{ActiveTime = mLimitTime};
        }
        protected override void SetLock()
        {
            base.SetLock();
            mbIsActive = false;
        }
    }   
}
