using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class PiggyBankRewardView : ViewBase
    {
        [Header("탭 횟수 / 탭 버튼")]
        [SerializeField] private int mTapCount;
        [SerializeField] private Button mTapButton;
        [Header("골드 텍스트")]
        [SerializeField] private TMP_Text mGoldText;
        [Header("연출 효과를 줄 돼지 렉트")]
        [SerializeField] private RectTransform mPigRect;
        [Header("보상 오브젝트")]
        [SerializeField] private GameObject mRewardObject;
        [Header("돼지 터질 때 파티클")]
        [SerializeField] private ParticleSystem mParticle;
        [Header("탭하여 계속 텍스트")]
        [SerializeField] private GameObject mTapText;
        private int mCurrentCount;
        private CanvasGroup mRewardCanvasGroup;

        private bool mbIsAnim;
        private bool mbComplete;
        private Sequence mSeq;
        public void SetView(int amount)
        {
            mGoldText.text = "x" + amount.ToString();
            mGoldText.gameObject.SetActive(false);

            mTapText.SetActive(false);

            mRewardObject.SetActive(false);
            mRewardCanvasGroup = mRewardObject.GetComponent<CanvasGroup>();
            mRewardCanvasGroup.alpha = 0;

            mTapButton.onClick.AddListener(Tap);

            mbIsAnim = false;
            mbComplete = false;
        }
        public override void Hide()
        {
            base.Hide();

            EventManager.Inst.ActiveEvent("PiggyRewardConfirm");
        }
        private void Tap()
        {
            if(mbComplete)
            {
                mbComplete = false;
                Hide();
            }
            if(mbIsAnim)
            {
                return;
            }
            mbIsAnim = true;

            mCurrentCount++;
            if(mCurrentCount >= mTapCount)
            {
                BoomPig();
                return;
            }

            if(mSeq != null && mSeq.active)
            {
                mSeq.Kill();
            }
            mSeq = DOTween.Sequence();

            mSeq.Append(mPigRect.DOScale(1.1f, 0.2f));
            mSeq.Append(mPigRect.DOScale(1f, 0.2f));

            mSeq.OnComplete(() => mbIsAnim = false);
        }
        private void BoomPig()
        {
            if(mSeq != null && mSeq.active)
            {
                mSeq.Kill();
            }
            mSeq = DOTween.Sequence();

            mSeq.Append(mPigRect.DOScale(1.1f, 0.2f));
            mSeq.Append(mPigRect.DORotate(new Vector3(0, 0, 15f), 0.15f).SetRelative().SetEase(Ease.InOutSine));
            mSeq.Append(mPigRect.DORotate(new Vector3(0, 0, -30f), 0.3f).SetRelative().SetEase(Ease.InOutSine));
            mSeq.Append(mPigRect.DORotate(new Vector3(0, 0, 15f), 0.15f).SetRelative().SetEase(Ease.InOutSine));

            mSeq.OnComplete(() => 
            {
                mPigRect.transform.parent.gameObject.SetActive(false);
                mParticle.Play();
                ShowReward();
            });
        }
        private void ShowReward()
        {
            mRewardObject.transform.localScale = Vector2.zero;
            mGoldText.transform.localScale = Vector2.zero;

            mRewardObject.SetActive(true);
            mGoldText.gameObject.SetActive(true);

            mSeq = DOTween.Sequence();
            mSeq.Append(mRewardObject.transform.DOScale(1, 1f).SetEase(Ease.OutBack));
            mSeq.Join(mRewardCanvasGroup.DOFade(1, 1f));

            mSeq.Append(mGoldText.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack));

            mSeq.OnComplete(() => 
            {
                mbComplete = true;
                mTapText.SetActive(true);
            });
        }
    }   
}
