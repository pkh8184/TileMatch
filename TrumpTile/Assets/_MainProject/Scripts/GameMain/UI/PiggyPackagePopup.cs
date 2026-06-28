using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using TMPro;

namespace TrumpTile.GameMain.UI
{
    public class PiggyPackagePopup : PopupBase, IPurchasable
    {
        [Header("애니메이션 효과를 줄 이미지들")]
        [SerializeField] private RectTransform mPigRect;
        [SerializeField] private RectTransform mLeftGoldRect;
        [SerializeField] private RectTransform mRightGoldRect;
        [SerializeField] private TemporaryContentUIController mContentController;

        [Header("축적된 돈 텍스트")]
        [SerializeField] private TMP_Text mStackedGoldText;
        [Header("구매 버튼")]
        [SerializeField] private Button mPurchaseButton;
        [Header("구매 버튼 텍스트")]
        [SerializeField] private TMP_Text mPurchaseButtonText;
        [Header("구매 완료 시 교체할 버튼 스프라이트 / 표시할 텍스트 렉트")]
        [SerializeField] private Sprite mAfterPurchaseSprite;
        [SerializeField] private TMP_Text mAfterPurchasePush;
        [Header("획득 진행도를 나타낼 슬라이더 / 체크포인트")]
        [SerializeField] private Slider mSlider;
        [SerializeField] private RectTransform[] mCheckPointRectArray;
        [Header("보상 수령 뷰")]
        [SerializeField] private PiggyBankRewardView mRewardProgressView;
        [Header("체크포인트 버튼 / 말풍선 (버튼 i 누르면 말풍선 i 표시)")]
        [SerializeField] private Button[] mCheckPointButtonArray;
        [SerializeField] private RectTransform[] mBallonRectArray;

        private Sequence mShowAnimSeq;
        private Sequence mProgressAnimSeq;
        private Sequence mAfterPurchasePushSeq;
        private Sequence mBallonSequence;
        // 인스펙터 직렬화된 Y를 Rect별 최초 1회 캐싱해 매번 같은 기준으로 사용(누적 오프셋 방지)
        private readonly Dictionary<RectTransform, float> mBallonBaseYMap = new Dictionary<RectTransform, float>();
        private PiggyBankContent mContentData;
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<PiggyBankContent>("PiggyBank");
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

            SetupCheckPointButtons();

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentController.PlayShowButtonAnim(mShowButton);

            mContentController.SetLimitTimeText(mContentData.GetContentInfo().ActiveTime);

            SetState();
        }
        public void OnPurchaseSuccess()
        {
            if(!gameObject.activeSelf) return;

            mContentController.ActiveRedDot(mContentData.HasNewThing);
            mContentData.PiggyBankDefaultRewardProgress();

            SetState();
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();

            EventManager.Inst.AddEvent("PurchaseSuccess", OnPurchaseSuccess);
            EventManager.Inst.AddEvent("PiggyRewardConfirm", OnPiggyRewardConfirm);
            EventManager.Inst.AddEvent("RewardAnimDone", InitAfterRewardAnim);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();

            EventManager.Inst?.RemoveEvent("PurchaseSuccess", OnPurchaseSuccess);
            EventManager.Inst?.RemoveEvent("PiggyRewardConfirm", OnPiggyRewardConfirm);
            EventManager.Inst?.RemoveEvent("RewardAnimDone", InitAfterRewardAnim);
        }
        protected override void PlayShowAnim()
        {
            mPopupObj.transform.localScale = Vector2.zero;
            mPigRect.anchoredPosition = Vector2.up * 500;
            if(mShowAnimSeq != null && mShowAnimSeq.active)
            {
                mShowAnimSeq.Kill();
            }

            mStackedGoldText.text = "0";
            mSlider.value = 0;
            foreach(var item in mCheckPointRectArray)
            {
                item.gameObject.SetActive(false);
                item.localScale = Vector2.zero;
            }
            if(mProgressAnimSeq != null && mProgressAnimSeq.active)
            {
                mProgressAnimSeq.Kill();
            }

            mShowAnimSeq = DOTween.Sequence();
            mShowAnimSeq.SetUpdate(true);

            mShowAnimSeq.Append(mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack));
            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.InQuad));

            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.up * 80, 0.1f).SetEase(Ease.OutQuad));
            mShowAnimSeq.Join(mLeftGoldRect.DOAnchorPos(Vector2.up * 60, 0.1f).SetEase(Ease.OutQuad));
            mShowAnimSeq.Join(mRightGoldRect.DOAnchorPos(Vector2.up * 60, 0.1f).SetEase(Ease.OutQuad));

            mShowAnimSeq.Append(mPigRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));
            mShowAnimSeq.Join(mLeftGoldRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));
            mShowAnimSeq.Join(mRightGoldRect.DOAnchorPos(Vector2.zero, 0.1f).SetEase(Ease.InQuad));

            mShowAnimSeq.OnComplete(() => PlayProgressAnim());
        }
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                gameObject.SetActive(false);

                EventManager.Inst.ActiveEvent("PlayRewardAnim");
            }); 
        }
        private void InitAfterRewardAnim()
        {
            if(mContentData.CanGetRewardAfterEndActive)
            {
                Show();
                RewardProgress();
                return;
            }
            if(mContentData.IsFull && mContentData.CanConfirm)
            {
                Show();
                RewardProgress();
                return;
            }
        }
        private void PlayProgressAnim()
        {
            mProgressAnimSeq = DOTween.Sequence();

            float value = 0;
            mProgressAnimSeq.Append(DOTween.To(() => value, x =>
            {
                value = x;
                mStackedGoldText.text = Mathf.RoundToInt(x).ToString();
            }, mContentData.GetCurrentStackedGold(), 0.5f));

            if(mContentData.CanConfirm)
            {
                mCheckPointRectArray[0].gameObject.SetActive(true);
                mProgressAnimSeq.Append(mCheckPointRectArray[0].DOScale(1, 0.3f).SetEase(Ease.OutBack));
            }
            float sliderValue = (float)((float)mContentData.GetCurrentStageCount() /(float) mContentData.GetMaxCount());
            if(sliderValue <= 0)
            {
                mSlider.value = 0.001f;
            }
            mProgressAnimSeq.Join(mSlider.DOValue(sliderValue, 0.5f));
            if(sliderValue == 1)
            {
                mCheckPointRectArray[1].gameObject.SetActive(true);
                mProgressAnimSeq.Append(mCheckPointRectArray[1].DOScale(1, 0.3f).SetEase(Ease.OutBack));
            }

            mProgressAnimSeq.OnComplete(() => SetInteractable(true));
        }
        private void SetState()
        {
            if(mContentData.CanConfirm)
            {
                mPurchaseButton.image.sprite = mAfterPurchaseSprite;
                mPurchaseButtonText.text = "구매 완료";
                mPurchaseButton.onClick.RemoveAllListeners();
                mPurchaseButton.onClick.AddListener(OnPurchaseButtonAfterPurchase);

                if(mContentData.IsFull)
                {
                    RewardProgress();
                }
                else
                {
                    Hide();
                }
            }
            else
            {
                mPurchaseButtonText.text = IAPManager.Instance.GetProductPrice(EProductId.PiggyBank);
                mPurchaseButton.onClick.AddListener(() => IAPManager.Instance.PurchaseProduct(EProductId.PiggyBank));
            }    
        }
        private void RewardProgress()
        {
            mRewardProgressView.SetView(mContentData.GetCurrentStackedGold());
            mRewardProgressView.Show();
        }
        private void OnPiggyRewardConfirm()
        {
            SetInteractable(false);
            mContentData.PiggyBankRewardProgress();

            mShowButton.gameObject.SetActive(false);

            Hide();
        }
        private void OnPurchaseButtonAfterPurchase()
        {
            if(mAfterPurchasePushSeq != null && mAfterPurchasePushSeq.active)
            {
                mAfterPurchasePushSeq.Kill();
            }
            mAfterPurchasePush.gameObject.SetActive(true);
            mAfterPurchasePush.color = new Color(1,1,1,0);
            mAfterPurchasePush.rectTransform.anchoredPosition = Vector2.zero;

            mAfterPurchasePushSeq = DOTween.Sequence();

            mAfterPurchasePushSeq.Append(mAfterPurchasePush.DOFade(1, 0.5f));
            mAfterPurchasePushSeq.Join(mAfterPurchasePush.rectTransform.DOAnchorPosY(30, 0.5f));
            mAfterPurchasePushSeq.AppendInterval(0.3f);

            mAfterPurchasePushSeq.OnComplete(() => mAfterPurchasePush.gameObject.SetActive(false));
        }
        private void SetupCheckPointButtons()
        {
            if(mCheckPointButtonArray != null)
            {
                for(int i = 0; i < mCheckPointButtonArray.Length; i++)
                {
                    if(mCheckPointButtonArray[i] == null)
                    {
                        continue;
                    }
                    int index = i;
                    mCheckPointButtonArray[i].onClick.RemoveAllListeners();
                    mCheckPointButtonArray[i].onClick.AddListener(() => OnCheckPointButton(index));
                }
            }
            // 시작 시 말풍선 숨김
            if(mBallonRectArray != null)
            {
                foreach(var ballon in mBallonRectArray)
                {
                    if(ballon != null)
                    {
                        ballon.gameObject.SetActive(false);
                    }
                }
            }
        }
        private void OnCheckPointButton(int index)
        {
            if(mBallonRectArray == null || index < 0 || index >= mBallonRectArray.Length)
            {
                return;
            }
            // 다른 말풍선은 숨기고 해당 말풍선만 표시
            for(int i = 0; i < mBallonRectArray.Length; i++)
            {
                if(i != index && mBallonRectArray[i] != null)
                {
                    mBallonRectArray[i].gameObject.SetActive(false);
                }
            }
            PlayBallonAnim(mBallonRectArray[index]);
        }
        private void PlayBallonAnim(RectTransform ballonRect)
        {
            if(ballonRect == null)
            {
                return;
            }
            if(!mBallonBaseYMap.TryGetValue(ballonRect, out float baseY))
            {
                baseY = ballonRect.anchoredPosition.y;
                mBallonBaseYMap.Add(ballonRect, baseY);
            }

            if(mBallonSequence != null && mBallonSequence.IsActive())
            {
                mBallonSequence.Kill();
            }

            ballonRect.anchoredPosition = new Vector2(ballonRect.anchoredPosition.x, baseY);
            ballonRect.localScale = Vector3.zero;
            ballonRect.localRotation = Quaternion.Euler(0f, 0f, -8f);

            ballonRect.gameObject.SetActive(true);

            mBallonSequence = DOTween.Sequence();
            // 등장: 회전 풀기 + 스케일 튀어오름 + 위로 살짝 떠오름
            mBallonSequence.Append(ballonRect.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2.5f));
            mBallonSequence.Join(ballonRect.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutCubic));
            mBallonSequence.Join(ballonRect.DOAnchorPosY(baseY + 4f, 0.35f).SetEase(Ease.OutCubic));
            // 흔들림 1회 (살짝 내려갔다 다시 위로)
            mBallonSequence.Append(ballonRect.DOAnchorPosY(baseY - 2f, 0.25f).SetEase(Ease.InOutSine));
            mBallonSequence.Append(ballonRect.DOAnchorPosY(baseY + 4f, 0.25f).SetEase(Ease.InOutSine));
            // 잠시 유지
            mBallonSequence.AppendInterval(0.3f);
            // 사라짐: 안으로 빨려들어가듯
            mBallonSequence.Append(ballonRect.DOScale(0f, 0.22f).SetEase(Ease.InBack));
            mBallonSequence.OnComplete(() => ballonRect.gameObject.SetActive(false));
        }
    }
}

