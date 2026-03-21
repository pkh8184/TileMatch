using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Data;
using System.Collections;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
{
    public class MainView : ViewBase
    {
        [Header("MainView 버튼")]
        [SerializeField] private Button mSettingViewButton;
        [SerializeField] private Button mShopViewButton;
        [SerializeField] private Button mProfileViewButton;
        [SerializeField] private Button mStageStartButton;

        [Header("MainView 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [SerializeField] private TMP_Text mCurrentStageText;

        [Header("MainView 팝업")]
        [SerializeField] private PopupBase mSettingPopup;
        [SerializeField] private PopupBase mProfilePopup;

        [Header("씬 전환 시 Fade 이미지")]
        [SerializeField] private Image mFadeImage;
        [Header("씬 전환 시 Fade 애니메이션 시간")]
        [SerializeField] private float mFadeDuration;

        //플레이어의 로컬 데이터 -> 따로 로컬데이터매니저에서 관리하는 게 좋을듯 (03/18)
        private Image mProfileFrame;
        private Image mProfileMask;


        public override void Initialize()
        {
            base.Initialize();

            StartCoroutine(Co_PlayFadeInAnim());
        }

        protected override void Refresh()
        {
            mGoldText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.Gold);
            mCurrentStageText.text = PlayerDataManager.Inst.GetDataToString(EPlayerDataType.CurrentStage);
        }
        
        private IEnumerator Co_PlayFadeInAnim()
        {
            mFadeImage.gameObject.SetActive(true);

            mFadeImage.DOFade(0, mFadeDuration);

            yield return new WaitForSeconds(mFadeDuration);

            mFadeImage.gameObject.SetActive(false);
        }
    }
}

