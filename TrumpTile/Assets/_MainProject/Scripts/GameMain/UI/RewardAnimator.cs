using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 보상(골드/아이템/패키지) 획득 연출 컴포넌트.
    /// 뷰에 부착하고 PlayReward / PlayPackageReward 호출로 사용.
    /// 뷰별로 달라지는 처리(입력 잠금, 버튼 두근거림 등)는 이벤트로 위임한다.
    /// </summary>
    public class RewardAnimator : MonoBehaviour
    {
        [Serializable]
        public class ItemSpriteConfig
        {
            public int ItemId;
            public Sprite sprite;
            [Tooltip("이 아이템이 날아갈 도착 위치. 비우면 공용 mItemTargetRect 사용")]
            public RectTransform target;
        }

        [Header("커버 풀")]
        [SerializeField] private Transform mInstanceContainer;
        [SerializeField] private GameObject mRewardCoverPrefab;
        [SerializeField] private int mPoolSize = 100;

        [Header("획득 카운트(+N) 텍스트")]
        [SerializeField] private TMP_Text mRewardCountText;

        [Header("골드 HUD 텍스트 (할당 시 골드 카운트업, 비우면 생략)")]
        [SerializeField] private TMP_Text mGoldText;

        [Header("코인이 날아갈 타겟")]
        [SerializeField] private RectTransform mGoldTargetRect;
        [Header("아이템 공용 도착 타겟 (ItemSpriteConfig.target 미지정 시 폴백)")]
        [SerializeField] private RectTransform mItemTargetRect;
        [Header("골드 스프라이트")]
        [SerializeField] private Sprite mGoldSprite;

        [Header("아이템 ID → 스프라이트 매핑")]
        [SerializeField] private ItemSpriteConfig[] mItemSpriteConfig;

        [Header("아이템 종류가 여러개일 때 커버 사이 가로 간격")]
        [SerializeField] private float mItemRewardSpacing = 100f;

        private List<RectTransform> mRewardCoverPool = new List<RectTransform>();

        /// <summary>연출 시작 시 호출 (입력 잠금 등에 사용)</summary>
        public event Action OnPlayStart;
        /// <summary>연출 종료 시 호출 (입력 잠금 해제 등에 사용)</summary>
        public event Action OnPlayComplete;
        /// <summary>골드 코인이 타겟에 도착할 때마다 호출 (샵 버튼 두근거림 등에 사용)</summary>
        public event Action OnGoldArrived;

        /// <summary>커버 풀 생성. 뷰 Initialize에서 1회 호출.</summary>
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
            mRewardCountText.gameObject.SetActive(false);
        }

        /// <summary>단일 보상(골드 또는 아이템) 연출.</summary>
        public void PlayReward(RewardDisplayInfo info)
        {
            OnPlayStart?.Invoke();
            StartCoroutine(Co_PlaySingleReward(info));
        }

        /// <summary>패키지 보상(골드 + 아이템 여러 종류) 연출. 골드 먼저, 그다음 아이템.</summary>
        public void PlayPackageReward(List<ProductReward> rewards)
        {
            OnPlayStart?.Invoke();

            List<RewardDisplayInfo> goldInfos = new List<RewardDisplayInfo>();
            List<RewardDisplayInfo> itemInfos = new List<RewardDisplayInfo>();
            foreach(var reward in rewards)
            {
                RewardDisplayInfo info = reward.GetRewardDisplayInfo();
                if(info == null)
                {
                    continue;
                }
                if(info.Type == ERewardType.Gold)
                {
                    goldInfos.Add(info);
                }
                else if(info.Type == ERewardType.Item)
                {
                    itemInfos.Add(info);
                }
            }

            StartCoroutine(Co_GetPackageReward(goldInfos, itemInfos));
        }

        private IEnumerator Co_PlaySingleReward(RewardDisplayInfo info)
        {
            ShowRewardCountText(info.Amount);

            if(info.Type == ERewardType.Gold)
            {
                yield return Co_GetGoldReward(info.Amount);
            }
            else
            {
                yield return Co_GetItemReward(new List<RewardDisplayInfo>{ info });
            }

            OnPlayComplete?.Invoke();
        }

        private IEnumerator Co_GetPackageReward(List<RewardDisplayInfo> goldInfos, List<RewardDisplayInfo> itemInfos)
        {
            //골드 먼저 기존 방식으로 지급 연출
            foreach(var gold in goldInfos)
            {
                ShowRewardCountText(gold.Amount);
                yield return Co_GetGoldReward(gold.Amount);
            }

            //아이템: 종류 수만큼 커버를 펼치되, 종류별 지급 개수는 동일하므로 카운트는 한 번만 표시
            if(itemInfos.Count > 0)
            {
                ShowRewardCountText(itemInfos[0].Amount);
                yield return Co_GetItemReward(itemInfos);
            }

            OnPlayComplete?.Invoke();
        }

        private void ShowRewardCountText(int amount)
        {
            mRewardCountText.gameObject.SetActive(true);
            mRewardCountText.text = "+" + amount.ToString();
            mRewardCountText.color = Color.white;
            mRewardCountText.rectTransform.anchoredPosition = Vector2.zero;
        }

        private IEnumerator Co_GetGoldReward(int amount)
        {
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
            seq.Append(mRewardCountText.DOFade(0, 0.5f));
            mRewardCountText.rectTransform.anchoredPosition = Vector2.zero;
            seq.Join(mRewardCountText.rectTransform.DOLocalMoveY(50, 0.5f));

            yield return seq.WaitForCompletion();

            Sequence moveSeq = DOTween.Sequence();
            for(int i = 0; i < max; i++)
            {
                RectTransform coin = mRewardCoverPool[i];
                moveSeq.Insert(0.1f * i, coin.DOMove(mGoldTargetRect.position, 0.3f).SetEase(Ease.InQuad)
                    .OnComplete(() => {
                        OnGoldArrived?.Invoke();
                        coin.gameObject.SetActive(false);
                        coin.anchoredPosition = Vector2.zero;
                    }));
            }

            Tween countTween = null;
            if(mGoldText != null)
            {
                float val = PlayerDataManager.Inst.Gold - amount;
                countTween = DOTween.To(() => val, x =>
                {
                    val = x;
                    mGoldText.text = Mathf.RoundToInt(x).ToString();
                }, PlayerDataManager.Inst.Gold, 0.3f + 0.1f * max).SetDelay(0.31f);
            }

            yield return moveSeq.WaitForCompletion();
            if(countTween != null)
            {
                yield return countTween.WaitForCompletion();
            }
        }

        private IEnumerator Co_GetItemReward(List<RewardDisplayInfo> itemInfos)
        {
            int count = itemInfos.Count;
            //종류 수만큼 컨테이너 위치에서 가로로 펼쳐 오프셋(가운데 정렬)
            float startX = -(count - 1) * 0.5f * mItemRewardSpacing;

            Sequence seq = DOTween.Sequence();
            for(int i = 0; i < count; i++)
            {
                RectTransform cover = mRewardCoverPool[i];
                ItemSpriteConfig config = FindItemConfig(itemInfos[i].ItemId);
                if(config != null && config.sprite != null)
                {
                    cover.GetComponent<Image>().sprite = config.sprite;
                }
                cover.anchoredPosition = new Vector2(startX + mItemRewardSpacing * i, 0f);
                cover.localScale = Vector2.zero;
                cover.gameObject.SetActive(true);

                seq.Insert(0.1f * i, cover.DOScale(1.1f, 0.2f));
                seq.Append(cover.DOScale(1f, 0.1f));
            }
            seq.Append(mRewardCountText.DOFade(0, 0.5f));
            mRewardCountText.rectTransform.anchoredPosition = Vector2.zero;
            seq.Join(mRewardCountText.rectTransform.DOLocalMoveY(50, 0.5f));

            yield return seq.WaitForCompletion();

            Sequence moveSeq = DOTween.Sequence();
            for(int i = 0; i < count; i++)
            {
                RectTransform cover = mRewardCoverPool[i];
                //아이템 종류별 타겟 위치로 이동(종류별 미지정 시 공용 타겟)
                RectTransform target = GetItemTarget(itemInfos[i].ItemId);
                moveSeq.Insert(0.1f * i, cover.DOMove(target.position, 0.3f).SetEase(Ease.InQuad)
                    .OnComplete(() => {
                        cover.gameObject.SetActive(false);
                        cover.anchoredPosition = Vector2.zero;
                    }));
            }

            yield return moveSeq.WaitForCompletion();
        }

        private ItemSpriteConfig FindItemConfig(int itemId)
        {
            foreach(var config in mItemSpriteConfig)
            {
                if(config.ItemId == itemId)
                {
                    return config;
                }
            }
            return null;
        }

        private RectTransform GetItemTarget(int itemId)
        {
            ItemSpriteConfig config = FindItemConfig(itemId);
            if(config != null && config.target != null)
            {
                return config.target;
            }
            return mItemTargetRect;
        }
    }
}
