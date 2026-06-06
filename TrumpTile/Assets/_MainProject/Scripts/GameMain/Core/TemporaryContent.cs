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

        public override void Initialize()
        {
            base.Initialize();

            //플레이어 데이터에서 컨텐츠에 해당하는 시간 정보 읽어와서 활성화 / 비활성화 처리
        }
    }   
}
