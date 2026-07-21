using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
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
        [Header("연출 효과를 줄 돼지 스파인")]
        [SerializeField] private SkeletonGraphic mPigSpine;
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

            EventManager.Inst.ActiveEvent(EventKeys.PIGGY_REWARD_CONFIRM);
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

            mPigSpine.AnimationState.SetAnimation(0, "interact", false);
            mPigSpine.AnimationState.Complete += _ => mbIsAnim = false;
        }
        private void BoomPig()
        {
            mPigSpine.AnimationState.SetAnimation(0, "destroy", false);
            mPigSpine.AnimationState.Complete += _ =>
            {
                mPigSpine.gameObject.SetActive(false);
                mParticle.Play();
                ShowReward();
            };
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
