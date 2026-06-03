using DG.Tweening;
using JetBrains.Annotations;
using System.Collections;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using TrumpTile.LevelEditor.Editor;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [System.Serializable]
    public class IngameItemConfig
    {
        public string itemName;
        public string itemDescription;
        public int itemId;
        public int amount = 3;
        public int cost = 100;
        public int unlockLevel;
        public Sprite itemIcon;
    }
    public class IngameView : ViewBase
    {
        [Header("스테이지 시작 시 표시될 레벨네임 오브젝트들")]
        [SerializeField] private Image mLevelNameBackground;
        [SerializeField] private CanvasGroup mLevelNameCanvasGroup;
        [SerializeField] private RectTransform mTopLevelNameRect;
        [SerializeField] private Image mLevelNameImage;        
        [SerializeField] private GameObject[] mEffectObjectArray;

        [Header("난이도별 레벨네임 오브젝트 배경")]
        [SerializeField] private Sprite[] mLevelTextBackgroundArray;

        [Header("레벨 배경 관련")]
        [SerializeField] private Image mBackgroundImage;
        [SerializeField] private Sprite mDefaultBackgroundSprite;
        [SerializeField] private Sprite[] mDifficultyLevelBackgroundArray = new Sprite[3];

        [Header("타이머 관련")]
        [SerializeField] private TMP_Text mTimerText;
        [SerializeField] private Slider mTimerSlider;
        [SerializeField] private RectTransform mTimerIconRect;
        [SerializeField] private RectTransform mTimePickerRect;

        [Header("슬롯 관련")]
        [SerializeField] private Button mBonusSlotButton;
        [SerializeField] private TMP_Text mBonusSlotText;

        [System.Serializable]
		private class ItemButtonConfig
		{
			public IngameItemConfig ingameItemConfig;
			public Button button;
			public TMP_Text countText;
            public GameObject lockObject;
            public GameObject unlockObject;
		}

        [Header("아이템 버튼")]
        [SerializeField] private ItemButtonConfig[] mItemButtonConfigArray = new ItemButtonConfig[4];
        [Header("구매 팝업들")]
        [SerializeField] private IngameItemPurchasePopup mItemPurchasePopup;
        [SerializeField] private IngameSlotPurchasePopup mSlotPurchasePopup;
        [Header("튜토리얼 팝업들")]
        [SerializeField] private PopupBase mMatchTutorial;
        [SerializeField] private PopupBase mSlotTutorial;
        [SerializeField] private ItemTutorialPopup mItemTutorial;
        [Header("아이템 해금 말풍선 관련")]
        [SerializeField] private RectTransform mBallonRect;
        [SerializeField] private TMP_Text mBallonText;
        private Sequence mBallonSequence;
        private float mBallonBaseY;
        private bool mbBallonBaseYCached;
        public override void Initialize()
        {
            base.Initialize();

            mLevelNameCanvasGroup.alpha = 0;
            mSlotPurchasePopup.SetSlotButtonObject(mBonusSlotButton.gameObject);
            mBonusSlotButton.onClick.AddListener(() =>
            {
                if (SlotManager.Instance != null && SlotManager.Instance.IsProcessing)
                {
                    return;
                }
                if(PlayerDataManager.Inst.Gold < SlotManager.Instance.BonusSlotCost)
                {
                    GameManager.Instance.PauseGame();
                    EventManager.Inst.ActiveEvent("AccessShopView");
                }
                else
                {
                    GameManager.Instance.PauseGame();
                    mSlotPurchasePopup.Show();
                }
            });
            
            RefreshButtons();

            mTopLevelNameRect.localScale = Vector3.zero;
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();
            //임시
            EventManager.Inst.AddEvent("IngameLoadingComplete", OnLoadLevelComplete);
            EventManager.Inst.AddEvent("TimerSettingComplete", OnTimerSettingComplete);
            //다른 UI들에서 상점 접근이 가능해지기 위한 이벤트 등록
            EventManager.Inst.AddEvent("ItemCountChanged", RefreshButtons);
            EventManager.Inst.AddEvent("PurchaseItem", PurchaseItem);
            
            PlayerDataManager.Inst.OnGoldChanged += RefreshButtons;
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            EventManager.Inst?.RemoveEvent("IngameLoadingComplete", OnLoadLevelComplete);
            EventManager.Inst?.RemoveEvent("TimerSettingComplete", OnTimerSettingComplete);
            EventManager.Inst?.RemoveEvent("ItemCountChanged", RefreshButtons);
            EventManager.Inst?.RemoveEvent("PurchaseItem", PurchaseItem);

            if(PlayerDataManager.Inst != null)
            {
                PlayerDataManager.Inst.OnGoldChanged -= RefreshButtons;   
            }
        }
        private void PurchaseItem(object id)
        {
            int itemId = (int)id;
            foreach(var item in mItemButtonConfigArray)
            {
                if(item.ingameItemConfig.itemId == itemId)
                {
                    GameManager.Instance.PauseGame();
                    mItemPurchasePopup.SetValid(item.ingameItemConfig);
                    mItemPurchasePopup.Show();
                    return;
                }
            }
        }
        private void RefreshButtons()
        {
            foreach (var item in mItemButtonConfigArray)
            {
                int id = item.ingameItemConfig.itemId;
                int count = PlayerDataManager.Inst.GetItemCount(id);
                item.countText.text = count.ToString();
                //item.button.image.color = count > 0? Color.white : new Color(200f / 255f,200f / 255f,200f / 255f,128f / 255f);
            }

            mBonusSlotText.color = PlayerDataManager.Inst.Gold >= SlotManager.Instance.BonusSlotCost ? Color.white : Color.red;
        }
        private void OnItemButtonClick(int id)
        {
            if (GameManager.Instance == null || !GameManager.Instance.CanUseItem())
			{
				return;
			}
            ItemManager.Inst.UseItem(id);
        }
        private void OnLoadLevelComplete(object obj)
        {
            var (levelData, isRetry) = ((LevelData, bool))obj;   

            if(isRetry)
            {
                mBonusSlotButton.gameObject.SetActive(true);
                mBonusSlotButton.transform.localScale = Vector2.one;
                GameManager.Instance.LoadingAnimComplete = true;
                GameManager.Instance.tutorialComplete = true;
                return;
            }

            int index = GetLevelDifficultyIndex(levelData.difficulty);

            mLevelNameImage.sprite = mLevelTextBackgroundArray[index];
            Sprite background = mDifficultyLevelBackgroundArray[index];
            mBackgroundImage.sprite = background? background : mDefaultBackgroundSprite; 

            if(index == 0)
            {
                AudioEvent.Play(EAudioKey.BGM_Ingame);
            }
            else if(index == 1)
            {
                AudioEvent.Play(EAudioKey.BGM_Ingame_Hard);
            }
            else
            {
                AudioEvent.Play(EAudioKey.BGM_Ingame_VeryHard);
            }

            bool bIsDailyMode = DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive;

            if (!bIsDailyMode)
            {
                mLevelNameBackground.gameObject.SetActive(true);
                mEffectObjectArray[index].SetActive(true);
                mLevelNameBackground.color = new Color(0, 0, 0, 245f / 255f);
                mTopLevelNameRect.localScale = Vector3.zero;
                mLevelNameCanvasGroup.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
                mTopLevelNameRect.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
            }

            foreach (var item in mItemButtonConfigArray)
            {
                if(levelData.levelNumber >= item.ingameItemConfig.unlockLevel)
                {
                    item.unlockObject.SetActive(true);
                    item.lockObject.SetActive(false);
                    int id = item.ingameItemConfig.itemId;
                    item.button.onClick.AddListener(() =>
                    {
                        OnItemButtonClick(id);
                    });
                }
                else
                {
                    item.lockObject.SetActive(true);
                    item.unlockObject.SetActive(false);
                    item.button.onClick.AddListener(() => ShowBallon(item));
                }
            }

            if (bIsDailyMode)
            {
                StartCoroutine(Co_PlayFadeInOnly());
            }
            else
            {
                StartCoroutine(Co_PlayLevelNameAnim(levelData.levelNumber, index));
            }
        }
        private void ShowBallon(ItemButtonConfig item)
        {
            // 인스펙터 직렬화된 Y를 최초 1회 캐싱. 이후 매번 같은 기준으로 사용해 누적 누락.
            if (!mbBallonBaseYCached)
            {
                mBallonBaseY = mBallonRect.anchoredPosition.y;
                mbBallonBaseYCached = true;
            }

            if(mBallonSequence != null && mBallonSequence.IsActive())
            {
                mBallonSequence.Kill();
            }
            if(mBallonRect.gameObject.activeSelf)
            {
                mBallonRect.gameObject.SetActive(false);
            }
            mBallonRect.SetParent(item.button.transform);
            mBallonRect.anchoredPosition = new Vector2(0f, mBallonBaseY);
            mBallonRect.localScale = Vector3.zero;
            mBallonRect.localRotation = Quaternion.Euler(0f, 0f, -8f);

            mBallonText.text = item.ingameItemConfig.unlockLevel.ToString() + "레벨 오픈";

            mBallonRect.gameObject.SetActive(true);

            mBallonSequence = DOTween.Sequence();
            // 등장: 회전 풀기 + 스케일 튀어오름 + 위로 살짝 떠오름
            mBallonSequence.Append(mBallonRect.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2.5f));
            mBallonSequence.Join(mBallonRect.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutCubic));
            mBallonSequence.Join(mBallonRect.DOAnchorPosY(mBallonBaseY + 4f, 0.35f).SetEase(Ease.OutCubic));
            // 흔들림 1회 (살짝 내려갔다 다시 위로)
            mBallonSequence.Append(mBallonRect.DOAnchorPosY(mBallonBaseY - 2f, 0.25f).SetEase(Ease.InOutSine));
            mBallonSequence.Append(mBallonRect.DOAnchorPosY(mBallonBaseY + 4f, 0.25f).SetEase(Ease.InOutSine));
            // 잠시 유지
            mBallonSequence.AppendInterval(0.3f);
            // 사라짐: 안으로 빨려들어가듯
            mBallonSequence.Append(mBallonRect.DOScale(0f, 0.22f).SetEase(Ease.InBack));
            mBallonSequence.OnComplete(() => mBallonRect.gameObject.SetActive(false));
        }
        private int GetLevelDifficultyIndex(EDifficultyType eDifficultyType)
        {
            string difficultString = eDifficultyType.ToString();

            if(difficultString.Contains("Very"))
            {
                return 2;
            }
            else if(difficultString.Contains("Hard"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        private void OnTimerSettingComplete()
        {
            StartCoroutine(Co_TimerTextProgress());
            StartCoroutine(Co_TimePickerProgress());
            StartCoroutine(Co_TimerSliderProgress());
        }
        private IEnumerator Co_PlayFadeInOnly()
        {
            yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
            RefreshButtons();
            GameManager.Instance.LoadingAnimComplete = true;
        }

        private IEnumerator Co_PlayLevelNameAnim(int level, int difficultyIndex)
        {
            yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());   

            RefreshButtons();
            Sequence sq = DOTween.Sequence();

            if(difficultyIndex == 0)
            {
                AudioEvent.Play(EAudioKey.SFX_LevelName_01);
            }
            else if(difficultyIndex == 1)
            {
                AudioEvent.Play(EAudioKey.SFX_LevelName_Hard);
            }
            else
            {
                AudioEvent.Play(EAudioKey.SFX_LevelName_VeryHard);
            }
            sq.Append(mLevelNameCanvasGroup.DOFade(1, 0.5f));
            RectTransform rt = mLevelNameCanvasGroup.transform.GetComponent<RectTransform>();

            rt.anchoredPosition = Vector2.zero;
            sq.Append(rt.DOPunchScale(Vector3.one * 0.1f, 0.4f, 5, 0.5f));
            sq.AppendInterval(0.5f);

            float targetX = -(Screen.width / 2f + rt.rect.width);
            sq.AppendCallback(() => AudioEvent.Play(EAudioKey.SFX_LevelName_02));
            sq.Append(rt.DOAnchorPosX(targetX, 0.5f).SetEase(Ease.InQuad));
            sq.Append(mLevelNameBackground.DOFade(0, 0.3f));

            sq.AppendInterval(0.3f);
            sq.Append(mTopLevelNameRect.DOScale(1, 0.3f).SetEase(Ease.OutBounce));
            sq.OnComplete(() => GameManager.Instance.LoadingAnimComplete = true);

            yield return sq.WaitForCompletion();
            
            int itemId = 0;
            int index = 0;
            foreach(var item in mItemButtonConfigArray)
            {
                if(level == item.ingameItemConfig.unlockLevel)
                {
                    itemId = item.ingameItemConfig.itemId;
                    break;
                }
                index++;
            }
            if(level == 1)
            {
                mMatchTutorial.Show();
            }
            else if(level == 2)
            {
                mSlotTutorial.Show();
            }
            else if(itemId != 0)
            {
                mItemTutorial.SetValid(mItemButtonConfigArray[index].ingameItemConfig);
                mItemTutorial.Show();
            }
            else
            {
                GameManager.Instance.tutorialComplete = true;
            }

            mLevelNameBackground.gameObject.SetActive(false);
        }
        private IEnumerator Co_TimerTextProgress()
        {
            while(true)
            {
                mTimerText.text = GameManager.Instance.GetCurrentTimeString();
                yield return null;
            }
        }
        private IEnumerator Co_TimePickerProgress()
        {
             while(true)
            {
                float angle = 360f * GameManager.Instance.GetCurrentTimeClamped();

                mTimePickerRect.localRotation = Quaternion.Euler(0,0,angle);
                yield return null;
            }
        }
        private IEnumerator Co_TimerSliderProgress()
        {
            Image sliderImage = mTimerSlider.fillRect.GetComponent<Image>();
            bool timerShakeStart = false;
            while (true)
            {
                float t = GameManager.Instance.GetCurrentTimeClamped();
                if(t <= 0.2f)
                {
                    if(timerShakeStart == false)
                    {
                        timerShakeStart = true;
                        mTimerIconRect.DOShakeAnchorPos(0.1f, 2.5f, 20).SetLoops(-1, LoopType.Restart);
                    }
                }
                else
                {
                    if(timerShakeStart == true)
                    {
                        timerShakeStart = false;
                        mTimerIconRect.DOKill();
                        mTimerIconRect.anchoredPosition = Vector2.zero;
                    }
                }

                mTimerSlider.value = t;

                Color green = new Color(0, 1, 0);
                Color yellow = new Color(1, 1, 0);
                Color red = new Color(1, 0, 0);

                Color result = t > 0.5f
                    ? Color.Lerp(yellow, green, (t - 0.5f) * 2f)
                    : Color.Lerp(red, yellow, t * 2f);

                sliderImage.color = GameManager.Instance.IsTimerFrozen? Color.white : result;

                yield return null;
            }
        }
    }
    
}

