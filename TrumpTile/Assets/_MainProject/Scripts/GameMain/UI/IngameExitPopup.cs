using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameExitPopup : PopupBase
    {
        [Header("레벨 텍스트")]
        [SerializeField] private TMP_Text mLevelText;
        [Header("버튼들")]
        [SerializeField] private Button mCancleButton;
        [SerializeField] private Button mConfirmButton;
        public override void Initialize()
        {
            base.Initialize();

            mCancleButton.onClick.AddListener(Hide);

            mConfirmButton.onClick.AddListener(OnConfirmButton);
        }
        public override void Show()
        {
            if(gameObject.activeSelf)
            {
                return;
            }
            mLevelText.text = "레벨 " + GameManager.Instance.CurrentLevel.ToString();
            base.Show();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("OnExitButton", _ => Show());
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("OnExitButton");
        }
        private void OnConfirmButton()
        {
            mOpenPopupCount = 0;
            GameManager.Instance.GoToMainMenu();
        }
    }    
}

