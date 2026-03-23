using UnityEngine;
using UnityEngine.EventSystems;
using TrumpTile.GameMain.Core;

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
            bool bUnlocked = UserDataManager.Instance != null && UserDataManager.Instance.IsExtraSlotUnlocked;
            if (bUnlocked)
            {
                gameObject.SetActive(false);
            }
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
    }
}
