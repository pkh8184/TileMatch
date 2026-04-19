using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    /// <summary>
    /// 시간 기반 별 판정 전역 설정
    /// Inspector에서 수정 가능
    /// </summary>
    [CreateAssetMenu(fileName = "StarConfig", menuName = "TrumpTile/Data/Star Config")]
    public class StarConfig : ScriptableObject
    {
        [Header("자동 계산 계수")]
        [Tooltip("타일 1개당 적정 시간(초). 타일 48개 * 2.0 = 96초")]
        [SerializeField] private float mTileTimeCoefficient = 2.0F;

        [Header("별 차감 비율")]
        [Tooltip("적정 시간의 이 배수 초과 시 2성. 예: 1.25 → 적정*1.25초 초과")]
        [SerializeField] private float mStar2TimeRatio = 1.25F;

        [Tooltip("적정 시간의 이 배수 초과 시 1성. 예: 1.5 → 적정*1.5초 초과")]
        [SerializeField] private float mStar1TimeRatio = 1.5F;

        public float TileTimeCoefficient => mTileTimeCoefficient;
        public float Star2TimeRatio => mStar2TimeRatio;
        public float Star1TimeRatio => mStar1TimeRatio;
    }
}
