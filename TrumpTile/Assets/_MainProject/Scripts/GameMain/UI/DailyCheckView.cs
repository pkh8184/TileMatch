using System;
using System.Collections;
using System.Collections.Generic;
using DG.DemiEditor;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class DailyCheckView : ViewBase
    {
        [Serializable]
        public class StickerButtonConfig
        {
            public Button StickerButton;
            public Image ItemImage;
            public TMP_Text ItemCount;
        }

        [Header("컨텐츠 UI 전용 컴포넌트")]
        [SerializeField] private PermanentContentUIController mContentController;
        [Header("스티커 구성")]
        [SerializeField] private StickerButtonConfig[] mStickerConfigArray;
        [Header("보상 스프라이트")]
        [SerializeField] private Sprite[] mRewardSpriteArray;
        [SerializeField] private Sprite mGold_20;
        [SerializeField] private Sprite mGold_30;
        [SerializeField] private Sprite mGold_40;
        [SerializeField] private Sprite mGold_50;
        [SerializeField] private Sprite mGold_100;
        [SerializeField] private Sprite mHammer;
        [SerializeField] private Sprite mClock;
        [SerializeField] private Sprite mHat;
        [SerializeField] private Sprite mBomb;
        //스티커 뗐는지 체크
        private bool[] mbCheckArray = new bool[9];
        //보상이 위치한 인덱스
        private int[]  mRewardIndexArray = new int[3];
        private DailyCheckContent mContentData;
        private ProductReward mTodayReward;
       // [SerializeField] private 
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<DailyCheckContent>("DailyCheck");
            if(mContentData == null)
            {
                Debug.Log($"[DailyCheckView] 컨텐츠 데이터 읽어오기에 실패했습니다.");
                return;
            }
            if(!mContentData.Unlock || !mContentData.HasNewThing)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }

            mShowButton.gameObject.SetActive(true);

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentController.PlayShowButtonAnim(mShowButton);

            InitStickerConfigs();

            Show();
        }
        public override void Hide()
        {
            base.Hide();

            mContentController.ActiveRedDot(mContentData.HasNewThing);
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("DailyCheckRewardConfirm", OnDailyCheckRewadConfirm);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("DailyCheckRewardConfirm", OnDailyCheckRewadConfirm);
        }
        private void InitStickerConfigs()
        {
            mTodayReward = mContentData.GetTodayReward();
            List<ProductReward> previewRewadList = mContentData.GetPreviewRewardList();

            int[] randomIndexArray = {0,1,2,3,4,5,6,7,8};
            randomIndexArray.Shuffle();
            for(int i = 0; i < 3; i++)
            {
                int index = randomIndexArray[i];

                RewardDisplayInfo info = mTodayReward.GetRewardDisplayInfo();

                mStickerConfigArray[index].ItemImage.sprite = GetSprite(info);
                mStickerConfigArray[index].ItemCount.text = "X" + info.Amount.ToString();
                mStickerConfigArray[index].StickerButton.onClick.AddListener(() => OnStickerClick(index));

                mRewardIndexArray[i] = index;
            }
            int previewIndex = 3;
            foreach(var item in previewRewadList)
            {
                RewardDisplayInfo info = item.GetRewardDisplayInfo();

                for(int i = 0; i < 2; i++)
                {
                    int index = randomIndexArray[previewIndex];

                    mStickerConfigArray[index].ItemImage.sprite = GetSprite(info);
                    mStickerConfigArray[index].ItemCount.text = "X" + info.Amount.ToString();
                    mStickerConfigArray[index].StickerButton.onClick.AddListener(() => OnStickerClick(index)); 
                    
                    previewIndex++;  
                }
            }
        }
        private Sprite GetSprite(RewardDisplayInfo info)
        {
            int index = 0;
            if(info.Type == ERewardType.Gold)
            {
                if(info.Amount == 100)
                {
                    index = 5;
                }
                else
                {
                    index = info.Amount / 10 - 1;   
                }
            }
            else
            {
                index = 6 + info.ItemId % 1005;
            }
            return mRewardSpriteArray[index];
        }
        private void OnDailyCheckRewadConfirm()
        {
            mShowButton.gameObject.SetActive(false);
        }
        private void OnStickerClick(int index)
        {
            mbCheckArray[index] = true;
            
            PlayStickerOffAnim(index);
        }
        private void PlayStickerOffAnim(int index)
        {
            RectTransform rect = mStickerConfigArray[index].StickerButton.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();

            Vector3 originalScale = rect.localScale;
            Vector2 startPos = rect.anchoredPosition;
            float randomAngle = UnityEngine.Random.Range(-20f, 20f);
            float swayDistance = 40f;
            float flutterAngle = 18f;
            float popDuration = 0.2f;
            float fallDuration = 0.9f;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            //1) 잠깐 커졌다가 원래 크기로 돌아오면서 동시에 랜덤 각도로 기울어짐
            //   팝 회전은 팔랑거림 시작 각도(randomAngle - flutterAngle)에서 끝나게 해서 낙하 시작 시 끊김 방지
            seq.Insert(0f, rect.DOScale(originalScale * 1.2f, popDuration * 0.5f).SetEase(Ease.OutBack));
            seq.Insert(popDuration * 0.5f, rect.DOScale(originalScale, popDuration * 0.5f).SetEase(Ease.InSine));
            seq.Insert(0f, rect.DORotate(new Vector3(0f, 0f, randomAngle - flutterAngle), popDuration).SetEase(Ease.OutSine));

            //2) 아래로 떨어지면서 좌우로 팔랑거리며 FadeOut (팝 직후부터)
            seq.Insert(popDuration, rect.DOAnchorPosY(startPos.y - 900f, fallDuration).SetEase(Ease.InQuad));

            //좌우로 팔랑거리는 흔들림 (현재 위치에서 이어지므로 스냅 없음)
            seq.Insert(popDuration + 0.00f, rect.DOAnchorPosX(startPos.x + swayDistance, 0.15f).SetEase(Ease.InOutSine));
            seq.Insert(popDuration + 0.15f, rect.DOAnchorPosX(startPos.x - swayDistance, 0.3f).SetEase(Ease.InOutSine));
            seq.Insert(popDuration + 0.45f, rect.DOAnchorPosX(startPos.x + swayDistance, 0.3f).SetEase(Ease.InOutSine));
            seq.Insert(popDuration + 0.75f, rect.DOAnchorPosX(startPos.x, 0.15f).SetEase(Ease.InOutSine));

            //회전 흔들림 (From 없이 팝이 끝난 각도에서 그대로 이어서 yoyo)
            seq.Insert(popDuration, rect.DORotate(new Vector3(0f, 0f, randomAngle + flutterAngle), 0.3f)
                .SetEase(Ease.InOutSine)
                .SetLoops(3, LoopType.Yoyo));

            seq.Insert(popDuration, canvasGroup.DOFade(0f, fallDuration).SetEase(Ease.InQuad));

            seq.OnComplete(() => GrantRewardProgress(index));
        }
        private void GrantRewardProgress(int index)
        {
            mStickerConfigArray[index].StickerButton.gameObject.SetActive(false);

            foreach(var item in mRewardIndexArray)
            {
                if(!mbCheckArray[item])
                {
                    return;
                }
            }
            
        }
    }    
}

