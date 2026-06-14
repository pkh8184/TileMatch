using System;
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
    public class RoulettePopup : PopupBase
    {
        [Header("애니메이션을 위한 참조")]
        [SerializeField] private RectTransform mRouletteRect;
        [SerializeField] private PermanentContentUIController mContentUIController;

        [Header("해금 팝업 프리팹")]
        [SerializeField] private GameObject mUnlockPopupPrefab;
        [Header("룰렛 버튼")]
        [SerializeField] private Button mRouletteButton;
        [Header("무료 / 광고 관련 참조")]
        [SerializeField] private GameObject mFreeGroup;
        [SerializeField] private GameObject mAdsGroup;
        [SerializeField] private GameObject mAdsIcon;
        [SerializeField] private GameObject mRemoveAdsIcon;
        [SerializeField] private TMP_Text mAdsCount;

        [Header("보상별 회전각도")]
        [SerializeField] private float[] mAnglesByReward;
        [Header("보상 획득 파티클")]
        [SerializeField] private ParticleSystem mParticle;
        private RouletteContent mContentData; 
        public override void Initialize()
        {
            base.Initialize();
            mContentData = ContentManager.Inst.GetContentData<RouletteContent>("Roulette");
            if(mContentData == null)
            {
                Debug.Log($"[RoulettePopup] 컨텐츠 데이터 읽어오기에 실패했습니다.");
                return;
            }
            if(!mContentData.Unlock || !mContentData.HasNewThing)
            {
                mShowButton.gameObject.SetActive(false);
                return;
            }

            mShowButton.gameObject.SetActive(true);

            mContentUIController.ActiveRedDot(mContentData.HasNewThing);

            if(mContentData.ShowUnlockPopup)
            {
                GameObject obj = Instantiate(mUnlockPopupPrefab.gameObject, Vector2.zero, Quaternion.identity, GameObject.Find("Canvas_Popup").transform);
                UIBase ui = obj.GetComponent<UIBase>();
                ui.Initialize();
                ui.Show();
            }

            mRouletteButton.onClick.AddListener(OnRouletteButton);
        }
        
        protected override void PlayShowAnim()
        {
            SetButtonState();

            base.PlayShowAnim();

            Sequence seq = DOTween.Sequence();
            mRouletteRect.localRotation = Quaternion.Euler(0,0,-207);

            seq.Append(mRouletteRect.DORotate(new Vector3(0,0,-242), 1f).SetEase(Ease.OutQuart));
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

                EventManager.Inst.ActiveEvent("GetPackageReward", CoreContainer.RewardContainer.GetContainer());
                CoreContainer.RewardContainer.Clear();
            }); 
        }
        protected override void SubscribeEvent()
        {
            base.SubscribeEvent();
            EventManager.Inst.AddEvent("RefreshRouletteData", SetButtonState);
        }
        protected override void UnSubscribeEvent()
        {
            base.UnSubscribeEvent();
            EventManager.Inst?.RemoveEvent("RefreshRouletteData", SetButtonState);
        }
        private void OnRouletteButton()
        {
            if(!mContentData.CanProgress())
            {
                return;
            }
            SetInteractable(false);

            int index = mContentData.GetRewardIndex();

            SpinRoulette(mAnglesByReward[index]);
        }
        private void SpinRoulette(float targetAngle)
        {
            const int SPIN_COUNT = 20;     // 도는 바퀴 수
            const float DURATION = 4f;    // 총 연출 시간

            // 매번 같은 시작점에서 출발하도록 리셋
            mRouletteRect.localRotation = Quaternion.identity;
            AudioEvent.Play(EAudioKey.SFX_Roulette_Spin);
            float endZ = -(360f * SPIN_COUNT) + targetAngle;   // 예: -1866

            mRouletteRect
                .DOLocalRotate(new Vector3(0f, 0f, endZ), DURATION, RotateMode.FastBeyond360)
                .SetEase(Ease.OutExpo)        // 초반 빠르게 → 끝에서 확 감속 (룰렛 느낌)
                .OnComplete(() => StartCoroutine(Co_OnSpinComplete()));

            mContentData.RouletteRewardProgress();

            SetButtonState();
        }
        private IEnumerator Co_OnSpinComplete()
        {
            mParticle.Play();
            AudioEvent.Play(EAudioKey.SFX_Roulette_Winning);

            List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
            infos.Add(mContentData.GetProductReward().GetRewardDisplayInfo());

            EventManager.Inst.ActiveEvent("PlayMiniRewardAnim", new MiniRewardPayload{Infos = infos, Type = EMiniRewardAnimType.PopupContent});
            yield return new WaitForSeconds(1f);

            SetInteractable(true);

            if(mContentData.Count == mContentData.MaxCount - mContentData.FreeCount)
            {
                if(!mContentData.IsFree)
                {
                    Hide();          
                }
            }
            else if(mContentData.Count == 0)
            {
                Hide();
                mShowButton.gameObject.SetActive(false);
            }
        }
        private void SetButtonState()
        {
            if(mContentData.Count == mContentData.MaxCount)
            {
                mFreeGroup.SetActive(true);
                mAdsGroup.SetActive(false);
            }
            else
            {
                mFreeGroup.SetActive(false);
                mAdsGroup.SetActive(true);

                if(mContentData.IsFree)
                {
                    mRemoveAdsIcon.SetActive(true);
                    mAdsIcon.SetActive(false);
                }
                else
                {
                    mRemoveAdsIcon.SetActive(false);
                    mAdsIcon.SetActive(true);
                }

                mAdsCount.text = $"({mContentData.Count}/3)";
            }
        }
    }    
}

