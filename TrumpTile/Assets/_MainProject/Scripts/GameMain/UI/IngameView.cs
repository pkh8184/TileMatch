using DG.Tweening;
using System.Collections;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using TrumpTile.LevelEditor;
using TrumpTile.LevelEditor.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
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
			public int itemId;
			public Button button;
			public TMP_Text countText;
		}

        [Header("아이템 버튼")]
        [SerializeField] private ItemButtonConfig[] mItemButtonConfigArray = new ItemButtonConfig[4];
        [Header("인게임 샵 뷰")]
        [SerializeField] private ShopView mShopView;
        public override void Initialize()
        {
            base.Initialize();

            mLevelNameCanvasGroup.alpha = 0;

            mBonusSlotButton.onClick.AddListener(() =>
            {
                if (SlotManager.Instance != null && SlotManager.Instance.IsProcessing)
                {
                    return;
                }
                if(PlayerDataManager.Inst.Gold >= SlotManager.Instance.BonusSlotCost)
                {
                    PlayerDataManager.Inst.UseGold(SlotManager.Instance.BonusSlotCost);
                    Sequence seq = DOTween.Sequence();
                    seq.Append(mBonusSlotButton.transform.DOScale(0, 0.3f));
                    seq.OnComplete(() => 
                    {
                        mBonusSlotButton.gameObject.SetActive(false);
                        SlotManager.Instance.SetSlotCount(7);
                    });
                }
                else
                {
                    mShopView.Show();
                }
            });

            foreach (var item in mItemButtonConfigArray)
            {
                int id = item.itemId;
                item.button.onClick.AddListener(() =>
                {
                    OnItemButtonClick(id);
                });

                int count = PlayerDataManager.Inst.GetItemCount(id);
                item.countText.text = count.ToString();
                item.button.image.color = count > 0? Color.white : new Color(200f / 255f,200f / 255f,200f / 255f,128f / 255f);
            }

            mBonusSlotText.color = PlayerDataManager.Inst.Gold >= SlotManager.Instance.BonusSlotCost ? Color.white : Color.red;

            mTopLevelNameRect.localScale = Vector3.zero;
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();
            //임시
            EventManager.Inst.AddEvent("IngameLoadingComplete", OnLoadLevelComplete);
            EventManager.Inst.AddEvent("TimerSettingComplete", OnTimerSettingComplete);
            //다른 UI들에서 상점 접근이 가능해지기 위한 이벤트 등록
            EventManager.Inst.AddEvent("AccessShopView", _ => mShopView.Show());
            EventManager.Inst.AddEvent("ItemCountChanged", _ => OnItemCountChanged());
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            EventManager.Inst?.RemoveEvent("IngameLoadingComplete");
            EventManager.Inst?.RemoveEvent("TimerSettingComplete");
            EventManager.Inst?.RemoveEvent("AccessShopView");
            EventManager.Inst?.RemoveEvent("ItemCountChanged");
        }
        private void OnItemCountChanged()
        {
             foreach (var item in mItemButtonConfigArray)
            {
                int id = item.itemId;
                int count = PlayerDataManager.Inst.GetItemCount(id);
                item.countText.text = count.ToString();
                item.button.image.color = count > 0? Color.white : new Color(200f / 255f,200f / 255f,200f / 255f,128f / 255f);
            }
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
            LevelData levelData = (LevelData)obj;   

            int index = GetLevelDifficultyIndex(levelData.difficulty);

            mLevelNameImage.sprite = mLevelTextBackgroundArray[index];
            Sprite background = mDifficultyLevelBackgroundArray[index];
            mBackgroundImage.sprite = background? background : mDefaultBackgroundSprite; 

            mLevelNameBackground.gameObject.SetActive(true);
            mEffectObjectArray[index].SetActive(true);

            mLevelNameBackground.color = new Color(0, 0, 0, 245f / 255f);

            mTopLevelNameRect.localScale = Vector3.zero;  

            mLevelNameCanvasGroup.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
            mTopLevelNameRect.transform.GetChild(0).GetComponent<TMP_Text>().text = $"LEVEL {GameManager.Instance.CurrentLevel}";
            StartCoroutine(Co_PlayLevelNameAnim());
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
        private void OnTimerSettingComplete(object obj)
        {
            StartCoroutine(Co_TimerTextProgress());
            StartCoroutine(Co_TimePickerProgress());
            StartCoroutine(Co_TimerSliderProgress());
        }
        private IEnumerator Co_PlayLevelNameAnim()
        {
            yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());

            Sequence sq = DOTween.Sequence();

            AudioEvent.Play(EAudioKey.SFX_LevelName_01);
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

