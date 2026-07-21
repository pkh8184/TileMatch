using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
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
            bool bIsDailyMode = DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive;
			if (bIsDailyMode)
			{
				mLevelText.text = "일일 퍼즐";
			}
			else
			{
                string header = GameManager.Instance.IsChampionsMode ? "챌린지 " : "레벨 ";
                string number = GameManager.Instance.IsChampionsMode ? PlayerDataManager.Inst.ChampionsLevel.ToString() : GameManager.Instance.CurrentLevel.ToString();
                mLevelText.text = header + number;
            }
            base.Show();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent(EventKeys.ON_EXIT_BUTTON, Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent(EventKeys.ON_EXIT_BUTTON, Show);
        }
        private void OnConfirmButton()
        {
            mOpenPopupCount = 0;
            CoreContainer.RewardContainer.Clear();
            GameManager.Instance.GoToMainMenu();
        }
    }    
}

