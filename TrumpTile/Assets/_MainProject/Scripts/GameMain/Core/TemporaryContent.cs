using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;

namespace TrumpTile.GameMain.Core
{
    [Serializable]
    public class TemporaryContent : ContentBase
    {
        [Header("컨텐츠 제한시간(분)")]
        [SerializeField] protected float mLimitTime;
        [Header("컨텐츠 종료 후 재시작까지 쿨타임(분)")]
        [SerializeField] private float mCoolTime;

        //컨텐츠 활성화 플래그
        protected bool mbIsActive;

        public bool IsActive => mbIsActive;

        //제한시간·쿨타임 모두 분 단위로 입력받아 초로 변환(*60)
        protected float LimitTimeSeconds => mLimitTime * 60f;
        protected float CoolTimeSeconds => mCoolTime * 60f;

        public override ContentInfo GetContentInfo()
        {
            //표시 UI(SetLimitTimeText, 언락 팝업)는 ActiveTime을 초로 취급하므로 분→초 변환해서 전달
            return new ContentInfo{ActiveTime = mLimitTime * 60f};
        }

        /// <summary>
        /// 활성 상태에서 남은 제한시간(초). 비활성이면 0.
        /// </summary>
        public float GetRemainTimeSeconds()
        {
            if(!mbIsActive)
            {
                return 0f;
            }
            DateTime activeDate = GetActiveDate();
            if(activeDate == default(DateTime))
            {
                //시작 시각이 없으면(기존 세이브 등) 전체 제한시간으로 간주
                return LimitTimeSeconds;
            }
            double remain = LimitTimeSeconds - (GameTime.UtcNow - activeDate).TotalSeconds;
            return remain > 0 ? (float)remain : 0f;
        }
        protected override void SetLock()
        {
            base.SetLock();
            mbIsActive = false;
        }

        //하위 컨텐츠가 PlayerData의 활성 플래그/시각과 연결하기 위한 훅
        protected virtual bool GetActiveFlag() => mbIsActive;
        protected virtual DateTime GetActiveDate() => default;
        protected virtual DateTime GetUnActiveDate() => default;
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }

        /// <summary>
        /// 시간 기준으로 활성/쿨타임 상태를 재평가한다. (Unlock 상태에서만 동작)
        /// - 활성 중  : 제한시간(mLimitTime*60초)이 지나면 비활성화 → 쿨타임 시작
        /// - 비활성 중: 첫 활성화이거나 쿨타임(mCoolTime*60초)이 지났으면 활성화(+진행도 초기화)
        /// 쿨타임 컨벤션대로 UTC(GameTime.UtcNow) 기준으로 비교한다.
        /// </summary>
        protected void EvaluateActiveState()
        {
            if(!mbIsUnlocked)
            {
                mbIsActive = false;
                return;
            }

            if(GetActiveFlag())
            {
                //ActiveDate가 없는(기존 세이브 등) 경우엔 제한시간 판정을 건너뛴다.
                //(now - MinValue 로 즉시 만료되어 진행 중 컨텐츠가 날아가는 것 방지)
                DateTime activeDate = GetActiveDate();
                bool bHasActiveDate = activeDate != default(DateTime);
                if(bHasActiveDate && LimitTimeSeconds > 0f && (GameTime.UtcNow - activeDate).TotalSeconds >= LimitTimeSeconds)
                {
                    //제한시간 만료 → 비활성화(쿨타임 시작)
                    OnDeactivate();
                    mbIsActive = false;
                }
                else
                {
                    mbIsActive = true;
                }
            }
            else
            {
                bool bNeverActivated = GetUnActiveDate() == default(DateTime);
                bool bCoolTimeOver = (GameTime.UtcNow - GetUnActiveDate()).TotalSeconds >= CoolTimeSeconds;
                if(bNeverActivated || bCoolTimeOver)
                {
                    //첫 활성화 또는 쿨타임 종료 → 활성화(+진행도 초기화)
                    OnActivate();
                    mbIsActive = true;
                }
                else
                {
                    mbIsActive = false;
                }
            }
        }
    }
}
