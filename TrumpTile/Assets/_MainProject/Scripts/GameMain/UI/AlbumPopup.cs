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
		[SerializeField] private TMP_Text mCollectedCountText;
		[SerializeField] private TMP_Text mMaxCountText;

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
			StartCoroutine(Co_ResetScrollToTop());
		}
        protected override void PlayShowAnim()
        {
            if (mPopupObj == null) return;

            ScrollRect scroll = GetComponentInChildren<ScrollRect>();
            if (scroll != null)
            {
                scroll.enabled = false;
            }

            mCurrentSeq?.Kill();
            mPopupObj.transform.localScale = Vector2.zero;

            mCurrentSeq = DOTween.Sequence();
            mCurrentSeq.SetUpdate(true);
            mCurrentSeq.Append(mPopupObj.transform.DOScale(1, mShowDuration).SetEase(Ease.OutBack));
            mCurrentSeq.OnComplete(() =>
			{
				SetInteractable(true);
				if (scroll != null)
				{
					scroll.enabled = true;
				}
			});
        }
		private IEnumerator Co_ResetScrollToTop()
		{
			// 슬롯 Destroy/재생성과 활성화 직후 레이아웃 패스가 한 번 돈 뒤에 위치를 잡아야 함
			yield return null;

			ScrollRect scroll = GetComponentInChildren<ScrollRect>();
			if (scroll == null)
			{
				yield break;
			}
			Canvas.ForceUpdateCanvases();
			LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
			scroll.velocity = Vector2.zero; // Inertia가 켜져 있어 닫을 때 남은 관성 제거
			scroll.verticalNormalizedPosition = 1f;
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
			if (mCollectedCountText != null) mCollectedCountText.text = $"{collected}";
			if (mMaxCountText != null) mMaxCountText.text = $"/ {total}";
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
			RefreshUI();
			SetInteractable(true);
		}

		private IEnumerator Co_CollectOnePicture(TBPictureCollectData picture)
		{
			AlbumManager.Inst.CollectPicture(picture);
			yield return null;

			RefreshUI();
		}

		private void OnSlotClicked(TBPictureCollectData picture, EAlbumPictureState state)
		{
			switch (state)
			{
				case EAlbumPictureState.Locked:
					AudioEvent.Play(EAudioKey.SFX_UnlockInteract);
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
