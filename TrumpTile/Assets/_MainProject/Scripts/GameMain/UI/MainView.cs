using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.LevelEditor.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class MainView : ViewBase
    {
        [Header("MainView 버튼")]
        [SerializeField] private Button mStageStartButton;
        [SerializeField] private Button mShopButton;

        [Header("일일 퍼즐")]
        [SerializeField] private Button mDailyPuzzleButton;
        [SerializeField] private CanvasGroup mDailyPuzzleButtonCanvasGroup;
        [SerializeField] private TMP_Text mDailyPuzzleButtonText;

        [Header("MainView 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [SerializeField] private TMP_Text mCurrentStageText;

        [Header("하위 팝업 참조")]
        [SerializeField] private ProfilePopup mProfilePopup;

        [Header("프로필 이미지")]
        [SerializeField] private Image mProfileFrame;
        [SerializeField] private Image mProfileImage;

        [Header("프로필 아바타 및 프레임 스프라이트 리스트")]
        [SerializeField] private List<Sprite> mAvataSpriteList;
        [SerializeField] private List<Sprite> mFrameSpriteList;
        [Header("스테이지 버튼 스프라이트")]
        [SerializeField] private Sprite mDefault;
        [SerializeField] private Sprite mBonus;

        [Header("메인씬 로딩 시 연출 효과를 줄 렉트들")]
        [SerializeField] private RectTransform mLeftElementsRect;
        [SerializeField] private RectTransform mRightElementsRect;
        [SerializeField] private RectTransform[] mSizeAdjustElementRectArray;
        [Header("보상 획득 연출 컴포넌트")]
        [SerializeField] private RewardAnimator mRewardAnimator;
        [Header("챔피언스 리그 활성화 / 비활성화 시 적용할 스프라이트")]
        [SerializeField] private Sprite mChampionsBackgroundSprite;
        [SerializeField] private Sprite mChampionsStageButtonSprite;
        [SerializeField] private Sprite mDefaultBackgroundSprite;
        [Header("배경")]
        [SerializeField] private Image mBackgroundImage;
        private CanvasGroup mLeftElementsRectCanvasGroup;
        private CanvasGroup mRightElementsRectCanvasGroup;
        private List<CanvasGroup> mSizeAdjustElementRectCanvasGroupList = new List<CanvasGroup>();
        public override void Initialize()
        {
            base.Initialize();

            PlayerDataManager.Inst?.Initialize();
            RefreshLocalData();          
            mShopButton.onClick.AddListener(() => EventManager.Inst.ActiveEvent("AccessShopView"));
            mStageStartButton.onClick.AddListener(OnStageButtonClick);
            mDailyPuzzleButton.onClick.AddListener(OnDailyPuzzleButtonClick);
            mDailyPuzzleButton.interactable = false;
            mDailyPuzzleButton.transform.localScale = Vector3.zero;
            mDailyPuzzleButtonCanvasGroup.alpha = 0;

            InitializeDailyPuzzleAsync();
            mProfilePopup.SetProfilePopupValid(mAvataSpriteList, mFrameSpriteList);

            mLeftElementsRect.anchoredPosition = Vector2.left * 250;
            mRightElementsRect.anchoredPosition = Vector2.right * 250;
            foreach(var item in mSizeAdjustElementRectArray)
            {
                item.localScale = Vector2.zero;
                CanvasGroup group = item.GetComponent<CanvasGroup>();
                mSizeAdjustElementRectCanvasGroupList.Add(group);
                group.alpha = 0;
            }

            mLeftElementsRectCanvasGroup = mLeftElementsRect.GetComponent<CanvasGroup>();
            mLeftElementsRectCanvasGroup.alpha = 0;

            mRightElementsRectCanvasGroup = mRightElementsRect.GetComponent<CanvasGroup>();
            mRightElementsRectCanvasGroup.alpha = 0;

            mRewardAnimator.Initialize();

            if(PlayerDataManager.Inst.CurrentStage >= CoreData.MAX_STAGE)
            {
                mStageStartButton.image.sprite = mChampionsStageButtonSprite;
                mStageStartButton.image.pixelsPerUnitMultiplier = 1;
                mCurrentStageText.text = "챌린지 " + PlayerDataManager.Inst.ChampionsLevel.ToString();
                mBackgroundImage.sprite = mChampionsBackgroundSprite;
                return;
            }
            else
            {
                mStageStartButton.image.sprite = mDefault;
                mStageStartButton.image.pixelsPerUnitMultiplier = 3;
                mCurrentStageText.text = "LEVEL " + PlayerDataManager.Inst.CurrentStage.ToString();
                mBackgroundImage.sprite = mDefaultBackgroundSprite;
            }

            // 임시 (테이블로 교체해야함)
            LevelData level;
            Addressables.LoadAssetAsync<LevelData>(string.Format("Level_{0:D3}",PlayerDataManager.Inst?.CurrentStage)).Completed += handle => {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    level = handle.Result;
                    if(level.difficulty.ToString().Contains("Bonus"))
                    {
                        mStageStartButton.image.sprite = mBonus;
                        mCurrentStageText.text = "BONUS";  
                    }
                    else
                    {
                        mStageStartButton.image.sprite = mDefault;
                        mCurrentStageText.text = PlayerDataManager.Inst?.GetDataToString(EPlayerDataType.CurrentStageForStageStart);
                    }
                }
            };

            AudioEvent.Play(EAudioKey.BGM_Main);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                CoreContainer.RewardContainer.AddGem(500);
                PlayerDataManager.Inst.AddGemCount(500);
                EventManager.Inst.ActiveEvent("ContentDataRefresh");

                PlayRewardAnim();
            }
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();
            EventManager.Inst.AddEvent("MainSceneLoadComplete", OnMainSceneLoadComplete);
            EventManager.Inst.AddEvent("PlayRewardAnim", PlayRewardAnim);
            EventManager.Inst.AddEvent("GoldRewardArrived", PulseShopButton);
            EventManager.Inst.AddEvent<(float,int)>("RefreshGoldText", GoldTextProgress);
            EventManager.Inst.AddEvent("ItemRewardArrived", PulseStageButton);
            EventManager.Inst.AddEvent("StartGemUIRefresh", SetInteractableFalse);
            EventManager.Inst.AddEvent("EndGemUIRefresh", SetInteractableTrue);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            EventManager.Inst?.RemoveEvent("MainSceneLoadComplete", OnMainSceneLoadComplete);
            EventManager.Inst?.RemoveEvent("PlayRewardAnim", PlayRewardAnim);
            EventManager.Inst?.RemoveEvent("GoldRewardArrived", PulseShopButton);
            EventManager.Inst?.RemoveEvent<(float,int)>("RefreshGoldText", GoldTextProgress);
            EventManager.Inst?.RemoveEvent("ItemRewardArrived", PulseStageButton);
            EventManager.Inst?.RemoveEvent("StartGemUIRefresh", SetInteractableFalse);
            EventManager.Inst?.RemoveEvent("EndGemUIRefresh", SetInteractableTrue);
        }
        protected override void Refresh()
        {
            int gold = PlayerDataManager.Inst.Gold;
            if(CoreContainer.RewardContainer.HasGoldRewads())
            {
                gold -= CoreContainer.RewardContainer.Gold;
            }
            mGoldText.text = gold.ToString();
        }
        
        protected override void RefreshLocalData()
        {
            int index = PlayerDataManager.Inst.GetProfileImageIndex();
            mProfileImage.sprite = mAvataSpriteList[index];

            index = PlayerDataManager.Inst.GetProfileFrameIndex();
            mProfileFrame.sprite = mFrameSpriteList[index];

            Debug.Log($"[{name}] Refresh Local Data");
        }
        private void SetInteractableTrue()
        {
            SetInteractable(true);
        }
        private void SetInteractableFalse()
        {
            SetInteractable(false);
        }
        private void PlayRewardAnim()
        {
            mRewardAnimator.PlayRewardAnim(() => SetInteractable(false), () => 
            {
                SetInteractable(true);
                EventManager.Inst.ActiveEvent("RewardAnimDone");

            });
        }
        private void PulseShopButton()
        {
            Transform shopTransform = mShopButton.transform;
            shopTransform.DOKill(true);
            shopTransform.localScale = Vector3.one;
            shopTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
            AudioEvent.Play(EAudioKey.SFX_GetReward_Gold);
        }
        private void PulseStageButton()
        {
            Transform stageTransform = mStageStartButton.transform;
            stageTransform.DOKill(true);
            stageTransform.localScale = Vector3.one;
            stageTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);
            AudioEvent.Play(EAudioKey.SFX_GetReward_Gold);
        }
        private void GoldTextProgress((float, int) data)
        {
            float duration = data.Item1;
            int amount = data.Item2;
            
            int value = PlayerDataManager.Inst.Gold - amount;

            DOTween.To(() => value, x =>
            {
                mGoldText.text = x.ToString();
            },PlayerDataManager.Inst.Gold, duration);
        }
        private void OnStageButtonClick()
        {
            SceneTransister.Inst.TransistScene("GameScene");
        }
        
        private async void InitializeDailyPuzzleAsync()
        {
            if (DailyPuzzleManager.Inst == null)
            {
                return;
            }
            await DailyPuzzleManager.Inst.InitializeAsync();
            RefreshDailyPuzzleButton();
        }

        private void RefreshDailyPuzzleButton()
        {
            bool bCleared = DailyPuzzleManager.Inst.IsTodayCleared;
            mDailyPuzzleButton.interactable = !bCleared;
            mDailyPuzzleButtonCanvasGroup.alpha = mDailyPuzzleButton.interactable ? 1F : 0.5F;
            if (mDailyPuzzleButtonText != null)
            {
                mDailyPuzzleButtonText.text = bCleared ? "완료" : "일일 퍼즐";
            }
        }

        private void OnDailyPuzzleButtonClick()
        {
            if (!DailyPuzzleManager.Inst.IsInitialized)
            {
                Debug.LogWarning("[MainView] DailyPuzzleManager not initialized yet");
                return;
            }

            DailyPuzzleManager.Inst.EnterDailyPuzzle();
         }

        private void OnMainSceneLoadComplete()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mLeftElementsRect.DOAnchorPosX(0, 0.25f));
            seq.Join(mRightElementsRect.DOAnchorPosX(0, 0.25f));
            seq.Join(mLeftElementsRectCanvasGroup.DOFade(1, 0.3f));
            seq.Join(mRightElementsRectCanvasGroup.DOFade(1, 0.3f));

            int i = 0;
            foreach(var item in mSizeAdjustElementRectArray)
            {
                seq.Insert(0.25f / 1.5f + (0.2f / 1.5f * i), item.DOScale(Vector2.one, 0.5f / 1.5f));
                seq.Join(mSizeAdjustElementRectCanvasGroupList[i].DOFade(1, 0.7f / 1.5f));
                i++;
            }

            float targetAlpha = mDailyPuzzleButton.interactable ? 1F : 0.5F;
            seq.Join(mDailyPuzzleButton.transform.DOScale(Vector3.one, 0.5f / 1.5f));
            seq.Join(mDailyPuzzleButtonCanvasGroup.DOFade(targetAlpha, 0.7f / 1.5f));

            seq.OnComplete(() => PlayRewardAnim());
        }
    }
}

