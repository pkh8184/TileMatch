using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class RewardAnimator : MonoBehaviour
    {
        [Serializable]
        public class ItemSpriteConfig
        {
            public int ItemId;
            public Sprite sprite;
        }

        [Header("커버 풀")]
        [SerializeField] private Transform mInstanceContainer;
        [SerializeField] private GameObject mRewardCoverPrefab;
        [SerializeField] private int mPoolSize = 100;

        [Header("획득 카운트(+N) 텍스트 — 아이템 종류 수만큼 + 여유분(권장 5개) 할당")]
        [SerializeField] private TMP_Text[] mRewardCountTexts;

        // 골드
        [Header("골드 스프라이트")]
        [SerializeField] private Sprite mGoldSprite;
        [Header("골드가 날아갈 타겟")]
        [SerializeField] private RectTransform mGoldTargetRect;

        // 아이템
        [Header("아이템 스프라이트")]
        [SerializeField] private Sprite[] mItemSpriteArray;
        [Header("아이템 종류가 여러개일 때 커버 사이 가로 간격")]
        [SerializeField] private float mItemRewardSpacing = 100f;
        [Header("아이템이 날아갈 타겟 렉트")]
        [SerializeField] private RectTransform mItemTargetRect;

        // 젬
        [Header("젬 스프라이트")]
        [SerializeField] private Sprite mGemSprite;
        [Header("젬이 날아갈 타겟 렉트")]
        [SerializeField] private RectTransform mGemTargetRect;

        private List<RectTransform> mRewardCoverPool = new List<RectTransform>();

        private bool mbIsPlaying;
        public void Initialize()
        {
            for(int i = 0; i < mPoolSize; i++)
            {
                GameObject obj = Instantiate(mRewardCoverPrefab, mInstanceContainer);
                obj.SetActive(false);
                RectTransform rt = obj.GetComponent<RectTransform>();
                if(rt == null)
                {
                    break;
                }
                mRewardCoverPool.Add(rt);
            }
            foreach(TMP_Text countText in mRewardCountTexts)
            {
                if(countText != null)
                {
                    countText.gameObject.SetActive(false);
                }
            }
        }
        public void PlayRewardAnim(Action OnPlayStart, Action OnPlayComplete)
        {
            if(mbIsPlaying)
            {
                return;
            }
            
            mbIsPlaying = true;

            StartCoroutine(Co_PlayRewardAnim(OnPlayStart, OnPlayComplete));
        }
        private IEnumerator Co_PlayRewardAnim(Action OnPlayStart, Action OnPlayComplete)
        {
            OnPlayStart?.Invoke();

            yield return StartCoroutine(Co_GoldAnim());

            yield return StartCoroutine(Co_ItemAnim());

            yield return StartCoroutine(Co_GemAnim());

            OnPlayComplete?.Invoke();

            mbIsPlaying = false;
        }
        private IEnumerator Co_GoldAnim()
        {
            int amount = CoreContainer.RewardContainer.UseGold();
            if(amount <= 0)
            {
                yield break;
            }

            Sequence seq = DOTween.Sequence();

            int max = Mathf.Min(20, amount / 10);

            for(int i = 0; i < max; i++)
            {
                float randX = UnityEngine.Random.Range(-50,50);
                float randY = UnityEngine.Random.Range(-50,50);

                mRewardCoverPool[i].GetComponent<Image>().sprite = mGoldSprite;
                mRewardCoverPool[i].anchoredPosition += new Vector2(randX, randY);
                mRewardCoverPool[i].localScale = Vector2.zero;
                mRewardCoverPool[i].gameObject.SetActive(true);

                seq.Insert(0.1f * i, mRewardCoverPool[i].DOScale(1.1f, 0.2f));
                seq.Append(mRewardCoverPool[i].DOScale(1f, 0.1f));
            }

            mRewardCountTexts[0].text = "+" + amount;
            mRewardCountTexts[0].gameObject.SetActive(true);
            mRewardCountTexts[0].rectTransform.anchoredPosition = Vector2.zero;
            mRewardCountTexts[0].color = Color.white;

            seq.Append(mRewardCountTexts[0].DOFade(0, 0.5f));
            seq.Join(mRewardCountTexts[0].rectTransform.DOLocalMoveY(50, 0.5f));

            yield return seq.WaitForCompletion();

            Sequence moveSeq = DOTween.Sequence();

            for(int i = 0; i < max; i++)
            {
                RectTransform coin = mRewardCoverPool[i];
                moveSeq.Insert(0.1f * i, coin.DOMove(mGoldTargetRect.position, 0.3f).SetEase(Ease.InQuad)
                    .OnComplete(() => {
                        coin.gameObject.SetActive(false);
                        coin.anchoredPosition = Vector2.zero;
                        EventManager.Inst.ActiveEvent("GoldRewardArrived");
                    }));
            }

            float duration = 0.1f * (max - 1) + 0.3f;
            moveSeq.InsertCallback(0.4f, () => EventManager.Inst.ActiveEvent("RefreshGoldText", (duration, amount)));

            yield return moveSeq.WaitForCompletion();
        }
        private IEnumerator Co_ItemAnim()
        {
            int[] items = CoreContainer.RewardContainer.UseItem();
            int sum = items.Sum();
            if(sum <= 0)
            {
                yield break;
            }
            int count = items.Count(x => x > 0);
            //종류 수만큼 컨테이너 위치에서 가로로 펼쳐 오프셋(가운데 정렬)
            float startX = -(count - 1) * 0.5f * mItemRewardSpacing;
            
            int interval = 0;

            Sequence seq = DOTween.Sequence();

            for(int i = 0; i < items.Length; i++)
            {
                if(items[i] <= 0)
                {
                    continue;
                }
                RectTransform cover = mRewardCoverPool[interval];
                cover.GetComponent<Image>().sprite = mItemSpriteArray[i];

                Vector2 pos = new Vector2(startX + mItemRewardSpacing * interval, 0f);
                cover.anchoredPosition = pos;
                cover.localScale = Vector2.zero;
                cover.gameObject.SetActive(true);

                mRewardCountTexts[interval].text = "+" + items[i];
                mRewardCountTexts[interval].gameObject.SetActive(true);
                mRewardCountTexts[interval].rectTransform.anchoredPosition = pos;
                mRewardCountTexts[interval].color = Color.white;

                float t = 0.1f * interval;

                seq.Insert(t, cover.DOScale(1.3f, 0.2f));
                seq.Insert(t + 0.2f, cover.DOScale(1.2f, 0.1f));

                seq.Insert(t + 0.3f, mRewardCountTexts[interval].DOFade(0, 0.5f));
                seq.Insert(t + 0.3f, mRewardCountTexts[interval].rectTransform.DOLocalMoveY(50, 0.5f));

                interval++;
            }
            yield return seq.WaitForCompletion();

            interval = 0;
            Sequence moveSeq = DOTween.Sequence();

            for(int i = 0; i < items.Length; i++)
            {
                if(items[i] <= 0)
                {
                    continue;
                }

                RectTransform cover = mRewardCoverPool[interval];
                //아이템 종류별 타겟 위치로 이동(종류별 미지정 시 공용 타겟)
                moveSeq.Insert(0.1f * interval, cover.DOMove(mItemTargetRect.position, 0.3f).SetEase(Ease.InQuad)
                    .OnComplete(() => {
                        cover.gameObject.SetActive(false);
                        cover.anchoredPosition = Vector2.zero;
                        EventManager.Inst.ActiveEvent("ItemRewardArrived");
                    }));

                interval++;
            }
            yield return moveSeq.WaitForCompletion();
        }
        private IEnumerator Co_GemAnim()
        {
            int amount = CoreContainer.RewardContainer.UseGem();
            if(amount <= 0)
            {
                yield break;
            }

            Sequence seq = DOTween.Sequence();

            int max = Mathf.Min(10, amount);

            for(int i = 0; i < max; i++)
            {
                float randX = UnityEngine.Random.Range(-30,30);
                float randY = UnityEngine.Random.Range(-30,30);

                mRewardCoverPool[i].GetComponent<Image>().sprite = mGemSprite;
                mRewardCoverPool[i].anchoredPosition += new Vector2(randX, randY);
                mRewardCoverPool[i].localScale = Vector2.zero;
                mRewardCoverPool[i].gameObject.SetActive(true);

                seq.Insert(0.1f * i, mRewardCoverPool[i].DOScale(1.1f, 0.2f));
                seq.Append(mRewardCoverPool[i].DOScale(1f, 0.1f));
            }

            mRewardCountTexts[0].text = "+" + amount;
            mRewardCountTexts[0].gameObject.SetActive(true);
            mRewardCountTexts[0].rectTransform.anchoredPosition = Vector2.zero;
            mRewardCountTexts[0].color = Color.white;

            seq.Append(mRewardCountTexts[0].DOFade(0, 0.5f));
            seq.Join(mRewardCountTexts[0].rectTransform.DOLocalMoveY(50, 0.5f));

            yield return seq.WaitForCompletion();

            Sequence moveSeq = DOTween.Sequence();

            for(int i = 0; i < max; i++)
            {
                RectTransform coin = mRewardCoverPool[i];
                moveSeq.Insert(0.1f * i, coin.DOMove(mGemTargetRect.position, 0.3f).SetEase(Ease.InQuad)
                    .OnComplete(() => {
                        coin.gameObject.SetActive(false);
                        coin.anchoredPosition = Vector2.zero;
                        EventManager.Inst.ActiveEvent("GemRewardArrived");
                    }));
            }

            moveSeq.InsertCallback(0.4f, () => EventManager.Inst.ActiveEvent("RefreshGemUI", amount));

            yield return moveSeq.WaitForCompletion();
        }
    }
}