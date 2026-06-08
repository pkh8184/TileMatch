using System;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumSlotView : MonoBehaviour
	{
		[SerializeField] private Image      mThumbnailImage;
		[SerializeField] private GameObject mLockIcon;
		[SerializeField] private GameObject mAvailableGlow;
		[SerializeField] private Button     mButton;

		private TBPictureData                               mPictureData;
		private EAlbumPictureState                          mState;
		private Action<TBPictureData, EAlbumPictureState>  mOnClick;

		private void Awake()
		{
			mButton.onClick.AddListener(OnClick);
		}

		public void Setup(TBPictureData picture, EAlbumPictureState state, Action<TBPictureData, EAlbumPictureState> onClick)
		{
			mPictureData = picture;
			mState       = state;
			mOnClick     = onClick;

			mLockIcon.SetActive(state == EAlbumPictureState.Locked);
			mAvailableGlow.SetActive(state == EAlbumPictureState.Available);

			bool bShowThumbnail = state == EAlbumPictureState.Collected;
			mThumbnailImage.gameObject.SetActive(bShowThumbnail);

			if (bShowThumbnail && !string.IsNullOrEmpty(picture.PictureThumbnailSrc))
			{
				Sprite sprite = Resources.Load<Sprite>(picture.PictureThumbnailSrc);
				if (sprite != null)
				{
					mThumbnailImage.sprite = sprite;
				}
			}
		}

		private void OnClick()
		{
			mOnClick?.Invoke(mPictureData, mState);
		}
	}
}
