using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class TreasureBoxContent : ContentBase
    {
        [Header("구매 후 재활성화까지 쿨타임(분)")]
        [SerializeField] private float mReactivateCoolTime;

        //쿨타임 분→초 변환
        private double ReactivateCoolTimeSeconds => mReactivateCoolTime * 60f;

        /// <summary>
        /// 상시 활성 컨텐츠. 해금됐고, 마지막 구매의 재활성화 시각이 지났으면(또는 미구매면) 활성.
        /// 쿨타임 컨벤션대로 UTC(GameTime.UtcNow) 기준으로 비교한다.
        /// </summary>
        public bool IsActive => mbIsUnlocked && GameTime.UtcNow >= PlayerDataManager.Inst.TreasureBoxActiveDate;

        public override void CheckUnlock()
        {
            if(PlayerDataManager.Inst.CurrentStage >= mLevelToUnlock)
            {
                SetUnlock();
                if(!PlayerDataManager.Inst.UserData.TreasureBoxUnlock)
                {
                    PlayerDataManager.Inst.UnlockTreasureBox();
                    mbShowUnlockPopup = true;
                }
                else
                {
                    mbShowUnlockPopup = false;
                }
            }
            else
            {
                SetLock();
            }
        }

        /// <summary>
        /// 구매 성공 시 호출. 실제 보상 지급은 IAP(OnPurchaseConfirmed → GrantReward)가 담당하므로
        /// 여기선 획득 연출용으로만 RewardContainer에 담고, 재활성화 쿨타임을 시작한다(그동안 비활성).
        /// </summary>
        public void OnPurchaseSuccess()
        {
            foreach(RewardDisplayInfo info in IAPManager.Instance.GetRewardDisplayInfos(EProductId.TreasureBox))
            {
                CoreContainer.RewardContainer.AddReward(info);
            }
            PlayerDataManager.Inst.StartTreasureBoxCoolTime(ReactivateCoolTimeSeconds);
        }

        /// <summary>
        /// 재활성화까지 남은 쿨타임(초). 활성 상태면 0.
        /// </summary>
        public float GetRemainCoolTimeSeconds()
        {
            double remain = (PlayerDataManager.Inst.TreasureBoxActiveDate - GameTime.UtcNow).TotalSeconds;
            return remain > 0f ? (float)remain : 0f;
        }
    }
}
