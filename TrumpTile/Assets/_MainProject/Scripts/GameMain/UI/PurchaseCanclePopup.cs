using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;


namespace TrumpTile.GameMain.UI
{
    public class PurchaseCanclePopup : PopupBase
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

            EventManager.Inst.AddEvent(EventKeys.PURCHASE_FAILED, Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            
            EventManager.Inst.RemoveEvent(EventKeys.PURCHASE_FAILED, Show);
        }
    }    
}

