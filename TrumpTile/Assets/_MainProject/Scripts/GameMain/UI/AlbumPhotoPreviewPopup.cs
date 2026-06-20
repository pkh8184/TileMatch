using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
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

		private CanvasGroup mPopupCanvasGroup;

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

		public void Setup(TBPictureCollectData picture)
		{
			// TODO: Addressables.LoadAssetAsync<Sprite>($"Picture_{picture.PictureId}_BG") 로 교체
			Sprite bg = Resources.Load<Sprite>($"Picture_{picture.PictureId}_BG");
			if (bg != null)
			{
				mBackgroundImage.sprite = bg;
			}

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
				gameObject.SetActive(false);
			});
		}
	}
}
