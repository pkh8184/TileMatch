using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 추가 슬롯 안내 팝업 (IAP RemoveAds 구매 유도)
    /// PopupBase 상속 - DOTween Show/Hide 애니메이션 자동 적용
    /// </summary>
    public class ExtraSlotPurchasePopup : PopupBase
    {
        [SerializeField] private LockedSlotView mLockedSlotView;
        [Header("Hide 애니메이션 완료 대기 시간 (PopupBase.mHideDuration과 동일하게 설정)")]
        [SerializeField] private float mUnlockDelay = 1F;

        [Header("UI 요소")]
        [SerializeField] private Button mConfirmButton;
        [SerializeField] private Button mCancelButton;

        private void Awake()
        {
            if (mConfirmButton != null)
            {
                mConfirmButton.onClick.AddListener(OnConfirm);
            }

            if (mCancelButton != null)
            {
                mCancelButton.onClick.AddListener(OnCancel);
            }
        }

        private void OnConfirm()
        {
            Hide();
            EventManager.Inst.ActiveEvent("AccessShopView");
        }

        /// <summary>
        /// IAP RemoveAds 구매 완료 후 호출
        /// </summary>
        public void OnRemoveAdsPurchaseCompleted()
        {
            Hide();
            StartCoroutine(UnlockAfterHide());
        }

        private IEnumerator UnlockAfterHide()
        {
            yield return new WaitForSeconds(mUnlockDelay);
            if(DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
            {
                SlotManager.Instance?.AddBonusSlot(5);
            }
            else
            {
                SlotManager.Instance?.AddBonusSlot(7);   
            }
            mLockedSlotView?.OnUnlocked();
        }

        private void OnCancel()
        {
            Hide();
        }
    }
}
