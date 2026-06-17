using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class GemCollectionPopup : PopupBase
    {
        [SerializeField] private TemporaryContentUIController mContentController;
        
        [Header("컨텐츠 해금 팝업 프리팹")]
        [SerializeField] private GameObject mUnlockPopupPrefab;

        [Header("보상 프리팹")]
        [SerializeField] private GameObject mRewardPrefab;
        [Header("보상 프리팹 부모 스크롤뷰 트랜스폼")]
        [SerializeField] private Transform mRewardParent;
        [Header("메인뷰에 있는 수집 게이지 / 보상")]
        [SerializeField] private Slider mMainViewSlider;
        [SerializeField] private Image mMainViewSliderRewardIcon;
        [SerializeField] private TMP_Text mMainViewSliderRewardCount;
        [SerializeField] private TMP_Text mMainViewSliderProgressText;
        [Header("팝업 내부 수집 게이지 / 보상")]
        [SerializeField] private Slider mSlider;
        [SerializeField] private Image mSliderRewardIcon;
        [SerializeField] private TMP_Text mSliderRewardCount;
        [SerializeField] private TMP_Text mSliderProgressText;
        [Header("다수 보상의 경우 표시할 선물상자 아이콘")]
        [SerializeField] private Sprite mGiftBoxSprite;
        
        private List<GemCollectionRewardUI> mGemRewardUIList = new List<GemCollectionRewardUI>();
        private GemCollectContent mContentData;
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<GemCollectContent>("GemCollection");
            if(mContentData == null)
            {
                Debug.Log($"[ExcitTravelView] 컨텐츠 데이터 읽어오기에 실패했습니다.");
                return;
            }
            if(!mContentData.Unlock || !mContentData.IsActive)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }

            mShowButton.gameObject.SetActive(true);

            mContentController.SetLimitTimeText(mContentData.GetContentInfo().ActiveTime);

            if(mContentData.ShowUnlockPopup)
            {
                GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
                UIBase ui = obj.GetComponent<UIBase>();
                ui.Initialize();
                ui.Show();
            }

            CreateElement();
            InitCollectGage();
        }
        private void CreateElement()
        {
            GemCollectionReward[] rewards = mContentData.GetRewards();

            int index = mContentData.GetCurrentIndex();

            for(int i = 0; i < rewards.Length; i++)
            {
                GemCollectionRewardUI ui = Instantiate(mRewardPrefab, mRewardParent).GetComponent<GemCollectionRewardUI>();

                ui.Ininitialize(i + 1, rewards[i].RewardArray, index < i, index > i);
                mGemRewardUIList.Add(ui);
            }
        }
        private void InitCollectGage()
        {
            int index = mContentData.GetCurrentIndex();
            int count = mContentData.GetCurrentGem();
            int goal = mContentData.GetCurrentRequiredGem();

            float value = (float)((float)count/(float)goal);
            mSlider.value = value;
            mMainViewSlider.value = value;

            string progress = $"{count}/{goal}";
            mSliderProgressText.text = progress;
            mMainViewSliderProgressText.text = progress;

            GemCollectionRewardConfig[] configs = mGemRewardUIList[index].GetConfigArray();
            Sprite sprite;
            string text = "";
            if(!configs[1].Image.gameObject.activeSelf)
            {
                sprite = configs[0].Image.sprite;
                text = configs[0].Text.text;
            }
            else
            {
                sprite = mGiftBoxSprite;
            }
            mSliderRewardIcon.sprite = sprite;
            mMainViewSliderRewardIcon.sprite = sprite;

            mSliderRewardCount.text = text;
            mMainViewSliderRewardCount.text = text;
        }
    }    
}

