using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 추가 슬롯 구매 팝업
    /// PopupBase 상속 - DOTween Show/Hide 애니메이션 자동 적용
    /// </summary>
    public class ExtraSlotPurchasePopup : PopupBase
    {
        [Header("슬롯 구매 설정")]
        [SerializeField] private int mExtraSlotGoldPrice = 500;
        [SerializeField] private LockedSlotView mLockedSlotView;
        [Header("Hide 애니메이션 완료 대기 시간 (PopupBase.mHideDuration과 동일하게 설정)")]
        [SerializeField] private float mUnlockDelay = 1F;

        [Header("UI 요소")]
        [SerializeField] private TMP_Text mPriceText;
        [SerializeField] private TMP_Text mCurrentGoldText;
        [SerializeField] private TMP_Text mNoticeText;
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

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (mPriceText != null)
            {
                mPriceText.text = $"{mExtraSlotGoldPrice:N0}G";
            }

            if (mCurrentGoldText != null && PlayerDataManager.Inst != null)
            {
                mCurrentGoldText.text = $"보유 골드: {PlayerDataManager.Inst.Gold:N0}G";
            }

            if (mNoticeText != null)
            {
                mNoticeText.gameObject.SetActive(false);
            }
        }

        private void OnConfirm()
        {
            bool bSuccess = PlayerDataManager.Inst != null &&
                            PlayerDataManager.Inst.PurchaseExtraSlot(mExtraSlotGoldPrice);

            if (bSuccess)
            {
                Hide();
                StartCoroutine(UnlockAfterHide());
            }
            else
            {
                if (mNoticeText != null)
                {
                    mNoticeText.gameObject.SetActive(true);
                    mNoticeText.text = "골드가 부족합니다.";
                }
            }
        }

        private IEnumerator UnlockAfterHide()
        {
            yield return new WaitForSeconds(mUnlockDelay);
            SlotManager.Instance?.SetSlotCount(7);
            mLockedSlotView?.OnUnlocked();
        }

        private void OnCancel()
        {
            Hide();
        }
    }
}
