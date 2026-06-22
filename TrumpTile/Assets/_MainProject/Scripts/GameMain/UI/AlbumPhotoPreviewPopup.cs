using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumPhotoPreviewPopup : PopupBase
	{
		[Header("사진 프리뷰")]
		[SerializeField] private Image    mBackgroundImage;
		[SerializeField] private TMP_Text mTitleText;
		[SerializeField] private TMP_Text mDescriptionText;
		[SerializeField] private Button   mCloseButton;

		private CanvasGroup                  mPopupCanvasGroup;
		private AsyncOperationHandle<Sprite> mBgImageHandle;

		public override void Initialize()
		{
			base.Initialize();
			mCloseButton.onClick.AddListener(Hide);

			mPopupCanvasGroup = mPopupObj.GetComponent<CanvasGroup>();
			if (mPopupCanvasGroup == null)
			{
				mPopupCanvasGroup = mPopupObj.AddComponent<CanvasGroup>();
			}

			gameObject.SetActive(false);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (mBgImageHandle.IsValid())
			{
				Addressables.Release(mBgImageHandle);
			}
		}

		public void Setup(TBPictureCollectData picture)
		{
			if (mBgImageHandle.IsValid())
			{
				Addressables.Release(mBgImageHandle);
			}

			ClearBackgroundImage();

			string key = $"Picture_{picture.PictureId}";
			mBgImageHandle = Addressables.LoadAssetAsync<Sprite>(key);
			mBgImageHandle.Completed += handle =>
			{
				if (handle.Status == AsyncOperationStatus.Succeeded && mBackgroundImage != null)
				{
					mBackgroundImage.sprite = handle.Result;
					mBackgroundImage.color  = Color.white;
				}
			};

			mTitleText.text       = LocalizeManager.Inst.GetString(picture.PictureNameId);
			mDescriptionText.text = LocalizeManager.Inst.GetString(picture.PictureDescriptionId);
		}

		protected override void PlayShowAnim()
		{
			mCurrentSeq?.Kill();
			mPopupObj.transform.localScale = Vector3.one;
			mPopupCanvasGroup.alpha         = 0F;

			mCurrentSeq = DOTween.Sequence();
			mCurrentSeq.SetUpdate(true);
			mCurrentSeq.Append(mPopupCanvasGroup.DOFade(1F, mShowDuration).SetEase(Ease.OutQuad));
			mCurrentSeq.OnComplete(() => SetInteractable(true));
		}

		protected override void PlayHideAnim()
		{
			mCurrentSeq?.Kill();

			mCurrentSeq = DOTween.Sequence();
			mCurrentSeq.SetUpdate(true);
			mCurrentSeq.Append(mPopupCanvasGroup.DOFade(0F, mHideDuration).SetEase(Ease.InQuad));
			mCurrentSeq.OnComplete(() =>
			{
				mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
				ClearBackgroundImage();
				gameObject.SetActive(false);
			});
		}

		private void ClearBackgroundImage()
		{
			mBackgroundImage.sprite = null;
			mBackgroundImage.color  = Color.black;
		}
	}
}
