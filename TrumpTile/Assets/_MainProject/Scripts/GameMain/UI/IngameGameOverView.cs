using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class IngameGameOverView : ViewBase
    {
        [Header("UI 참조")]
        [SerializeField] private Image mBackground;
        [SerializeField] private Image mTimeGauge;
        [SerializeField] private TMP_Text mTimeText;
        [SerializeField] private TMP_Text mMessageText;
        [SerializeField] private Button mResurrectionButton;
        [SerializeField] private Button mRetryButton;
        [SerializeField] private Button mMainButton;

        [Header("부활 카운트")]
        [SerializeField] private float mCount;
        [Header("표시할 메세지")]
        [SerializeField] private string mMessage;
        [Header("타이머 게이지 색깔 지정")]
        [SerializeField] private Color mStartColor;
        [SerializeField] private Color mEndColor;
        [Header("첫 사망 시 표시할 버튼 오브젝트")]
        [SerializeField] private GameObject mBeforeResurrectionObj;
        [Header("부활 후 사망 시 표시할 버튼 오브젝트")]
        [SerializeField] private GameObject mAfterResurrectionObj;

        private float mBackgroundOriginAlpha;
        public override void Initialize()
        {
            base.Initialize();

            mResurrectionButton.onClick.AddListener(OnResurrectionButtonClick);

            mRetryButton.onClick.AddListener(OnRetryButtonClick);

            mMainButton.onClick.AddListener(GameManager.Instance.GoToMainMenu);
            
            mBackgroundOriginAlpha = mBackground.color.a;

            mMessageText.text = mMessage;
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("GameOver", Show);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("GameOver", Show);
        }
        public override void Show()
        {
            base.Show();

            if(GameManager.Instance.IsResurrection)
            {
                mAfterResurrectionObj.SetActive(true);
                mBeforeResurrectionObj.SetActive(false);
            }
            else
            {
                mBeforeResurrectionObj.SetActive(true);
                mAfterResurrectionObj.SetActive(false);
            }
            mBackground.color = new Color(mBackground.color.r, mBackground.color.g, mBackground.color.b, mBackgroundOriginAlpha);
            mTimeGauge.fillAmount = 1;

            Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            seq.Append(mTimeGauge.DOFillAmount(0, mCount));

            Color.RGBToHSV(mStartColor, out float startH, out float startS, out float startV);
            Color.RGBToHSV(mEndColor, out float endH, out float endS, out float endV);

            seq.Join(DOTween.To(() => startH, x =>
            {
                mTimeGauge.color = Color.HSVToRGB(x, 1f, 1f);
            }, endH, mCount));

            float value = mCount;
            seq.Join(DOTween.To(() => value, x =>
            {
                value = x;
                int seconds = (int)x;
                mTimeText.text = seconds.ToString();
            }, 0, mCount));

            seq.Join(mBackground.DOFade(1, mCount));

            seq.OnComplete(() =>
            {
                GameManager.Instance.RestartLevel();
                Hide();
            });
        }
        private void OnResurrectionButtonClick()
        {
            if(PlayerDataManager.Inst.UserData.RemoveAds)
            {
                
            }
            else
            {
                Debug.Log("[IngameGameOverView] 광고 재생 후 부활");
                Resurrection();
            }
        }
        private void OnRetryButtonClick()
        {
            GameManager.Instance.RestartLevel();
            Hide();
        }
        private void Resurrection()
        {
            GameManager.Instance.ContinueGame();
            Hide();
        }
    }    
}

