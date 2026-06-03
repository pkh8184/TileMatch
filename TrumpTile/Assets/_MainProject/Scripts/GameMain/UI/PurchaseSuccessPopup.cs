using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    public class PurchaseSuccessPopup : PopupBase
    {
        [SerializeField] private Button mButton;

        public override void Initialize()
        {
            base.Initialize();

            mButton.onClick.AddListener(Hide);
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("PurchaseConfirmed", Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            
            EventManager.Inst.RemoveEvent("PurchaseConfirmed", Show);
        }
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);
                EventManager.Inst.ActiveEvent("PurchaseSuccess");
            }); 
        }

    }    
}
