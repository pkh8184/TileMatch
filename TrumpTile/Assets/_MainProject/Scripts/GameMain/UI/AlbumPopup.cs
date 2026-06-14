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
		[SerializeField] private AlbumSlotView mSlotViewPrefab;
		[SerializeField] private Transform     mSlotContainer;

		[Header("다음 챕터 잠금")]
		[SerializeField] private GameObject mLockIconObj;

		[Header("프리뷰")]
		[SerializeField] private AlbumPhotoPreviewPopup previewPopup;

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

			foreach (Transform child in mSlotContainer)
			{
				Destroy(child.gameObject);
			}

			for (int i = 0; i < pictureStates.Count; i++)
			{
				AlbumSlotView slot = Instantiate(mSlotViewPrefab, mSlotContainer);
				slot.Setup(pictureStates[i].picture, i + 1, pictureStates[i].state, OnSlotClicked);
			}
		}

		private void UpdateGauge(int collected, int total)
		{
			float ratio = total > 0 ? (float)collected / total : 0F;
			if (mProgressSlider != null) mProgressSlider.value = ratio;
			if (mProgressText != null) mProgressText.text    = $"{collected}/{total}";
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

			_ = AlbumManager.Inst.SaveAlbumRewardedStage();
			SetInteractable(true);
		}

		private IEnumerator Co_CollectOnePicture(TBPictureCollectData picture)
		{
			AlbumManager.Inst.CollectPicture(picture);

			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			float targetRatio = total > 0 ? (float)collected / total : 0F;
			yield return mProgressSlider.DOValue(targetRatio, 0.4F).SetEase(Ease.OutQuad).WaitForCompletion();
			mProgressText.text = $"{collected}/{total}";

			RefreshUI();
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
					if (previewPopup != null)
					{
						previewPopup.Setup(picture);
						previewPopup.Show();
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
