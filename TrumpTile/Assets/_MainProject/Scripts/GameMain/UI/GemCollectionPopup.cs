using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        [SerializeField] private Transform mMainViewGemTransform;
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
                mShowButton.transform.parent.gameObject.SetActive(false);
                return;
            }

            mShowButton.transform.parent.gameObject.SetActive(true);

            mContentController.SetLimitTimeText(mContentData.GetContentInfo().ActiveTime);

            CreateElement();
            InitCollectGage();
        }
        private void InitAfterRewardAnim()
        {
            if(mContentData.ShowUnlockPopup)
            {
                GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
                UIBase ui = obj.GetComponent<UIBase>();
                ui.Initialize();
                ui.Show();
            }
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent<int>("RefreshGemUI", RefreshGemUI);
            EventManager.Inst.AddEvent("GemRewardArrived", PulseGemRect);
            EventManager.Inst.AddEvent("RewardAnimDone", InitAfterRewardAnim);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent<int>("RefreshGemUI", RefreshGemUI);
            EventManager.Inst?.RemoveEvent("GemRewardArrived", PulseGemRect);
            EventManager.Inst?.RemoveEvent("RewardAnimDone", InitAfterRewardAnim);
        }
        private void PulseGemRect()
        {
            mMainViewGemTransform.DOKill(true);
            mMainViewGemTransform.localScale = Vector3.one;
            mMainViewGemTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
        }
        private void RefreshGemUI(int amount)
        {   
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            gameObject.SetActive(true);
            StartCoroutine(Co_RefreshGemUI(amount));
            EventManager.Inst.ActiveEvent("StartGemUIRefresh");
        }
        private IEnumerator Co_RefreshGemUI(int amount)
        {
            int gem = mContentData.GetCurrentGem();
            int goal = mContentData.GetCurrentRequiredGem();

            int count = 0;
            while(gem >= goal)
            {
                gem -= goal;
                count++;
                goal = mContentData.GetNextRequiredGem(count);
                if(goal == 0)
                {
                    break;
                }
            }
            int start = mContentData.GetCurrentGem() - amount;

            for(int i = 0; i < count; i++)
            {
                Sequence seq = DOTween.Sequence();

                AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp);

                seq.Append(mMainViewSlider.DOValue(1, 0.5f));
                seq.Join(DOTween.To(() => start, x =>
                {
                    mMainViewSliderProgressText.text = $"{x}/{mContentData.GetCurrentRequiredGem()}";
                }, mContentData.GetCurrentRequiredGem(), 0.5f));
                seq.AppendCallback(() => mContentData.RewardProgress());

                if(goal == 0 && i == count - 1)
                {
                    seq.Append(mShowButton.transform.parent.DOScale(0, 0.3f).SetEase(Ease.InBack));
                    seq.AppendCallback(() => EventManager.Inst.ActiveEvent("PlayRewardAnim"));
                    yield break;
                }

                seq.Append(mMainViewSliderRewardIcon.transform.DOScale(0, 0.3f).SetEase(Ease.InBack));
                seq.JoinCallback(() => AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp_Complete));
                seq.AppendCallback(() =>
                {
                    GemCollectionRewardConfig[] configs = mGemRewardUIList[mContentData.GetCurrentIndex()].GetConfigArray();
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
                    mMainViewSliderRewardIcon.sprite = sprite;
                    mMainViewSliderRewardCount.text = text;
                });
                seq.Append(mMainViewSliderRewardIcon.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack));
                seq.AppendCallback(() =>
                {
                    mMainViewSlider.value = 0;
                    start = 0;
                });

                seq.AppendInterval(0.5f);

                yield return seq.WaitForCompletion();
            }

            Sequence lastSeq = DOTween.Sequence();

            AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp);
            lastSeq.Append(mMainViewSlider.DOValue((float)((float)mContentData.GetCurrentGem()/(float)mContentData.GetCurrentRequiredGem()), 0.5f));
            lastSeq.Join(DOTween.To(() => start, x =>
            {
                mMainViewSliderProgressText.text = $"{x}/{mContentData.GetCurrentRequiredGem()}";
            }, mContentData.GetCurrentGem(), 0.5f));

            yield return lastSeq.WaitForCompletion();

            gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).gameObject.SetActive(true);

            InitCollectGage();
            SetElement();

            EventManager.Inst.ActiveEvent("EndGemUIRefresh");
            EventManager.Inst.ActiveEvent("PlayRewardAnim");
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
        private void SetElement()
        {
            if(mContentData.GetCurrentIndex() >= mGemRewardUIList.Count)
            {
                return;
            }
            for(int i = 0; i < mContentData.GetCurrentIndex(); i++)
            {
                mGemRewardUIList[i].SetCollect();
                mGemRewardUIList[i].SetUnlock();
            }
            mGemRewardUIList[mContentData.GetCurrentIndex()].SetUnlock();
        }
        private void InitCollectGage()
        {
            int index = mContentData.GetCurrentIndex();
            int count = mContentData.GetCurrentGem();
            int countMain = count - CoreContainer.RewardContainer.Gem;
            int goal = mContentData.GetCurrentRequiredGem();


            float value = (float)((float)count/(float)goal);
            float valueMain = (float)((float)countMain/(float)goal);
            mSlider.value = value;
            mMainViewSlider.value = valueMain;

            string progress = $"{count}/{goal}";
            string progressMain = $"{countMain}/{goal}";
            mSliderProgressText.text = progress;
            mMainViewSliderProgressText.text = progressMain;

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

