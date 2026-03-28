using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    /// <summary>
    /// 스테이지 클리어 여부와 관계 없이 스테이지가 종료 될 때
    /// 갱신되어야 하는 유저 데이터의 집합입니다.
    /// 스테이지 진행 시에 변경되는 데이터를 누적합니다.
    /// </summary>
    public class StageEndData
    {
        public int CurrentStage;
        public bool bIsClear;
        public int EndTimeToSecond;

        public int Gold;
        public int Star; // 하우징 재화
        public int Blackhole;
        public int Timer;
        public int Bomb;
        public int Item4;
    }

}
