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
	public class AlbumPopup : PopupBase
	{
		[Header("게이지")]
		[SerializeField] private Slider   mProgressSlider;
		[SerializeField] private TMP_Text mProgressText;

		[Header("사진 슬롯 (그리드)")]
		[SerializeField] private AlbumSlotView[] mSlotViewArray;

		[Header("보상 연출")]
		[SerializeField] private GameObject    mGiftBoxObj;
		[SerializeField] private CanvasGroup   mGiftBoxCanvasGroup;
		[SerializeField] private GameObject    mRewardIconsObj;
		[SerializeField] private RectTransform mGoldTargetRect;
		[SerializeField] private RectTransform mStageButtonTargetRect;

		[Header("다음 챕터 잠금")]
		[SerializeField] private GameObject mLockIconObj;

		private CanvasGroup mPopupCanvasGroup;

		public override void Initialize()
		{
			base.Initialize();
			mPopupCanvasGroup = GetComponent<CanvasGroup>();
			if (mPopupCanvasGroup == null)
			{
				mPopupCanvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
			RefreshUI();
		}

		public override void Show()
		{
			base.Show();
			RefreshUI();
		}

		private void RefreshUI()
		{
			if (AlbumManager.Inst == null)
			{
				return;
			}

			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			UpdateGauge(collected, total);

			List<(TBPictureCollectData picture, EAlbumPictureState state)> pictureStates
				= AlbumManager.Inst.GetPictureStates();

			for (int i = 0; i < mSlotViewArray.Length; i++)
			{
				if (i < pictureStates.Count)
				{
					mSlotViewArray[i].gameObject.SetActive(true);
					mSlotViewArray[i].Setup(pictureStates[i].picture, pictureStates[i].state, OnSlotClicked);
				}
				else
				{
					mSlotViewArray[i].gameObject.SetActive(false);
				}
			}
		}

		private void UpdateGauge(int collected, int total)
		{
			float ratio = total > 0 ? (float)collected / total : 0F;
			mProgressSlider.value = ratio;
			mProgressText.text    = $"{collected}/{total}";
		}

		public void PlayRewardSequence(List<TBPictureCollectData> pendingPictures)
		{
			StartCoroutine(Co_RewardSequence(pendingPictures));
		}

		private IEnumerator Co_RewardSequence(List<TBPictureCollectData> pendingPictures)
		{
			SetInteractable(false);

			foreach (TBPictureCollectData picture in pendingPictures)
			{
				yield return StartCoroutine(Co_CollectOnePicture(picture));
			}

			PlayerDataManager.Inst.SetPendingAlbumReward(false);
			SetInteractable(true);
		}

		private IEnumerator Co_CollectOnePicture(TBPictureCollectData picture)
		{
			// 데이터 먼저 반영 후 게이지 애니메이션
			AlbumManager.Inst.CollectPicture(picture);

			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			float targetRatio = total > 0 ? (float)collected / total : 0F;
			yield return mProgressSlider.DOValue(targetRatio, 0.6F).SetEase(Ease.OutQuad).WaitForCompletion();
			mProgressText.text = $"{collected}/{total}";

			mGiftBoxObj.SetActive(true);
			mGiftBoxCanvasGroup.alpha = 0F;
			mGiftBoxObj.transform.localScale = Vector3.zero;

			Sequence boxSeq = DOTween.Sequence();
			boxSeq.Append(mGiftBoxCanvasGroup.DOFade(1F, 0.2F));
			boxSeq.Join(mGiftBoxObj.transform.DOScale(1F, 0.3F).SetEase(Ease.OutBack));
			boxSeq.Append(mGiftBoxObj.transform.DOShakeRotation(0.5F, 15F, 10));
			yield return boxSeq.WaitForCompletion();

			mGiftBoxObj.transform.DOPunchScale(Vector3.one * 0.3F, 0.2F);
			yield return new WaitForSeconds(0.2F);
			mGiftBoxObj.SetActive(false);

			mRewardIconsObj.SetActive(true);
			mRewardIconsObj.transform.localScale = Vector3.zero;
			yield return mRewardIconsObj.transform.DOScale(1F, 0.3F).SetEase(Ease.OutBack).WaitForCompletion();
			yield return new WaitForSeconds(0.3F);

			yield return StartCoroutine(Co_FlyRewardIcons(picture));

			mRewardIconsObj.SetActive(false);

			RefreshUI();
			yield return new WaitForSeconds(0.2F);
		}

		private IEnumerator Co_FlyRewardIcons(TBPictureCollectData picture)
		{
			bool bHasItem = picture.HammerRewardCount > 0
				|| picture.ClockRewardCount > 0
				|| picture.HatRewardCount > 0
				|| picture.BombRewardCount > 0;

			List<Tween> tweens = new List<Tween>();

			if (bHasItem && mStageButtonTargetRect != null)
			{
				RectTransform iconRect = mRewardIconsObj.GetComponent<RectTransform>();
				tweens.Add(iconRect.DOMove(mStageButtonTargetRect.position, 0.5F).SetEase(Ease.InQuad));
			}

			foreach (Tween t in tweens)
			{
				yield return t.WaitForCompletion();
			}
		}

		private void OnSlotClicked(TBPictureCollectData picture, EAlbumPictureState state)
		{
			switch (state)
			{
				case EAlbumPictureState.Locked:
					Debug.Log("[AlbumPopup] Locked: 아직 수집할 수 없습니다.");
					break;
				case EAlbumPictureState.Available:
					Debug.Log("[AlbumPopup] Available: 튜토리얼 가이드 표시.");
					break;
				case EAlbumPictureState.Collected:
					AlbumPhotoPreviewPopup preview = FindObjectOfType<AlbumPhotoPreviewPopup>(true);
					if (preview != null)
					{
						preview.Setup(picture);
						preview.Show();
					}
					break;
			}
		}

		private void SetInteractable(bool bInteractable)
		{
			if (mPopupCanvasGroup != null)
			{
				mPopupCanvasGroup.interactable   = bInteractable;
				mPopupCanvasGroup.blocksRaycasts = bInteractable;
			}
		}
	}
}
