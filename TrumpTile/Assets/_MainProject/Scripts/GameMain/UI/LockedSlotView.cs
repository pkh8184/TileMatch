using UnityEngine;
using UnityEngine.EventSystems;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 인게임 7번째 슬롯 잠금 UI
    /// 터치 시 ExtraSlotPurchasePopup 오픈
    /// </summary>
    public class LockedSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ExtraSlotPurchasePopup mPurchasePopup;

        private void Start()
        {
            bool bUnlocked = PlayerDataManager.Inst != null && PlayerDataManager.Inst.IsAdsRemoved;
            if (bUnlocked)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventManager.Inst?.AddEvent("RemoveAdsPurchased", OnRemoveAdsPurchased);
        }

        private void OnDisable()
        {
            EventManager.Inst?.RemoveEvent("RemoveAdsPurchased", OnRemoveAdsPurchased);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (SlotManager.Instance != null && SlotManager.Instance.IsProcessing)
            {
                return;
            }

            mPurchasePopup?.Show();
        }

        /// <summary>
        /// 구매 완료 후 ExtraSlotPurchasePopup에서 호출
        /// </summary>
        public void OnUnlocked()
        {
            gameObject.SetActive(false);
        }

        private void OnRemoveAdsPurchased()
        {
            SlotManager.Instance?.AddBonusSlot(7);
            OnUnlocked();
        }
    }
}
