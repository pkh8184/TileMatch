using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameFailedPopup : PopupBase
    {
        [Header("다시 시도 버튼")]
        [SerializeField] private Button mRetryButton;
        [SerializeField] private Button mCancleButton;
        [Header("레벨 텍스트")]
        [SerializeField] private TMP_Text mLevelText;
        public override void Initialize()
        {
            base.Initialize();

            mRetryButton.onClick.AddListener(() => 
            {
                Hide();
                GameManager.Instance.RestartLevel();
            });
            mCancleButton.onClick.AddListener(GameManager.Instance.GoToMainMenu);
        }
        public override void Show()
        {
            mLevelText.text = "레벨 " + GameManager.Instance.CurrentLevel.ToString();
            base.Show();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("StageFailed", Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst.RemoveEvent("StageFailed", Show);
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
                GameManager.Instance.RestartLevel();
            });   
        }
    }   
}
