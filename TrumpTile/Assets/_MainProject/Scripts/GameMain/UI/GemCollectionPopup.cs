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
        [Header("젬 2배 버프 활성 시 재생할 파티클\n(Contents_GemCollect/CollectGage/Icon_Gem 하위)")]
        [SerializeField] private ParticleSystem mGemDoubleParticle;
        
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

            mContentController.SetLimitTimeText(mContentData.GetRemainTimeSeconds());

            CreateElement();
            InitCollectGauge();
            RefreshGemDoubleEffect();

            MainManager.Instance.AddEvent(Co_MainSceneEnterEvent, EMainSceneEventType.UnlockContent);
        }
        protected override void Refresh()
        {
            base.Refresh();
            //로케일 변경/데이터 갱신 시 시간제한 텍스트 재적용 (SetLimitTimeText가 refresh 흐름 밖 일회성이라 초기 로케일 미반영되던 문제 방지)
            if(mContentData == null || !mContentData.Unlock || !mContentData.IsActive)
            {
                return;
            }
            mContentController.SetLimitTimeText(mContentData.GetRemainTimeSeconds());
            //버프 획득/만료가 CONTENT_DATA_REFRESH를 타고 들어오므로 여기서 연출을 맞춘다.
            RefreshGemDoubleEffect();
        }
        private IEnumerator Co_MainSceneEnterEvent()
        {
            if(mContentData.ShowUnlockPopup)
            {
                GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
                UIBase ui = obj.GetComponent<UIBase>();
                ui.Initialize();
                ui.Show();

                yield return new WaitWhile(() => obj.activeSelf);
            }
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent<int>(EventKeys.REFRESH_GEM_UI, RefreshGemUI);
            EventManager.Inst.AddEvent(EventKeys.GEM_REWARD_ARRIVED, PulseGemRect);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent<int>(EventKeys.REFRESH_GEM_UI, RefreshGemUI);
            EventManager.Inst?.RemoveEvent(EventKeys.GEM_REWARD_ARRIVED, PulseGemRect);
        }
        /// <summary>
        /// 젬 2배 버프 상태에 맞춰 젬 아이콘 파티클을 켜고 끈다.
        /// 버프 만료 판정은 ContentManager가 컨텐츠 쿨타임과 같은 시점에 처리하므로,
        /// 여기서는 그 결과(IsGemDoubleActive)만 보고 연출을 맞춘다.
        /// </summary>
        private void RefreshGemDoubleEffect()
        {
            if(mGemDoubleParticle == null)
            {
                return;
            }

            bool bActive = PlayerDataManager.Inst != null && PlayerDataManager.Inst.IsGemDoubleActive;
            if(bActive)
            {
                //이미 재생 중이면 다시 Play해서 연출이 끊기지 않도록 둔다.
                if(!mGemDoubleParticle.gameObject.activeSelf)
                {
                    mGemDoubleParticle.gameObject.SetActive(true);
                }
                if(!mGemDoubleParticle.isPlaying)
                {
                    mGemDoubleParticle.Play(true);
                }
                return;
            }

            if(mGemDoubleParticle.isPlaying)
            {
                mGemDoubleParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            mGemDoubleParticle.gameObject.SetActive(false);
        }
        private void PulseGemRect()
        {
            mMainViewGemTransform.DOKill(true);
            mMainViewGemTransform.localScale = Vector3.one;
            mMainViewGemTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
            AudioEvent.Play(EAudioKey.SFX_GetReward_Gold);
        }
        private void RefreshGemUI(int amount)
        {   
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            
            gameObject.SetActive(true);
            MainManager.Instance.AddEvent(() => Co_RefreshGemUI(amount), EMainSceneEventType.GetGemProgress);
        }
        private IEnumerator Co_RefreshGemUI(int amount)
        {
            int startValue = mContentData.CapturedGemCount - amount;

            List<GemCollectAnimPayload> animPayloadList = mContentData.GetAnimPayload();
            int count = 0;
            if(animPayloadList != null)
            {
                count = animPayloadList.Count;
            }
            for(int i = 0; i < count; i++)
            {
                Sequence seq = DOTween.Sequence();

                AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp);

                seq.Append(mMainViewSlider.DOValue(1, 0.5f));

                seq.Join(DOTween.To(() => startValue, x =>
                {
                    mMainViewSliderProgressText.text = $"{x}/{animPayloadList[i].CapturedRequiredGemCount}";
                }, animPayloadList[i].CapturedRequiredGemCount, 0.5f));

                seq.AppendCallback(() => EventManager.Inst.ActiveEvent(EventKeys.PLAY_MINI_REWARD_ANIM, new MiniRewardPayload{Infos = animPayloadList[i].RewardDisplayInfoList, Type = EMiniRewardAnimType.Custom, target = new Vector2(235, 79), parentName = "Contents_GemCollect"}));
                seq.Append(mMainViewSliderRewardIcon.transform.DOScale(0, 0.3f).SetEase(Ease.InBack));
                seq.JoinCallback(() => AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp_Complete));

                seq.AppendCallback(() =>
                {
                    GemCollectionRewardConfig[] configs = mGemRewardUIList[animPayloadList[i].CapturedCurrentIndex + 1].GetConfigArray();
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
                    startValue = 0;
                });

                seq.AppendInterval(0.5f);

                yield return seq.WaitForCompletion();

                foreach(var item in animPayloadList[i].RewardDisplayInfoList)
                {
                    CoreContainer.RewardContainer.AddReward(item);

                    //대기 골드를 실제 애니 대상(mGold)으로 이동: AddReward가 mGold에 더했으니 대기분에서 차감.
                    //(차감 총량 = mGold + PendingAnimGold 는 불변 → 게이지 진행 중에도 표기 일관)
                    if(item != null && item.Type == ERewardType.Gold)
                    {
                        CoreContainer.RewardContainer.ConsumePendingAnimGold(item.Amount);
                    }
                }
            }
            Sequence lastSeq = DOTween.Sequence();

            if(mContentData.IsMaxIndex)
            {
                lastSeq.Append(mShowButton.transform.parent.DOScale(0, 0.3f).SetEase(Ease.InBack));
                lastSeq.AppendCallback(() => mShowButton.transform.parent.gameObject.SetActive(false));
                
                yield return lastSeq.WaitForCompletion();
                EventManager.Inst.ActiveEvent(EventKeys.PLAY_REWARD_ANIM);
                yield break;
            }

            AudioEvent.Play(EAudioKey.SFX_Main_GemCollection_GaugeUp);
            lastSeq.Append(mMainViewSlider.DOValue((float)((float)mContentData.GetCurrentGem()/(float)mContentData.GetCurrentRequiredGem()), 0.5f));
            lastSeq.Join(DOTween.To(() => startValue, x =>
            {
                mMainViewSliderProgressText.text = $"{x}/{mContentData.GetCurrentRequiredGem()}";
            }, mContentData.GetCurrentGem(), 0.5f));

            yield return lastSeq.WaitForCompletion();

            gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).gameObject.SetActive(true);

            SetElement();

            EventManager.Inst.ActiveEvent(EventKeys.PLAY_REWARD_ANIM);
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
        private void InitCollectGauge()
        {
            int index = mContentData.CapturedIndex;
            int count = mContentData.GetCurrentGem();
            int goal = mContentData.GetCurrentRequiredGem();

            int countMain = mContentData.PreviousGemCount;
            int goalMain = mContentData.GetCapturedRequiredGemCount();


            float value = (float)((float)count/(float)goal);
            float valueMain = (float)((float)countMain/(float)goalMain);
            mSlider.value = value;
            mMainViewSlider.value = valueMain;

            string progress = $"{count}/{goal}";
            string progressMain = $"{countMain}/{goalMain}";
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

