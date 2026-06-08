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

		private TBPictureCollectData                               mPictureData;
		private EAlbumPictureState                          mState;
		private Action<TBPictureCollectData, EAlbumPictureState>  mOnClick;

		private void Awake()
		{
			mButton.onClick.AddListener(OnClick);
		}

		public void Setup(TBPictureCollectData picture, EAlbumPictureState state, Action<TBPictureCollectData, EAlbumPictureState> onClick)
		{
			mPictureData = picture;
			mState       = state;
			mOnClick     = onClick;

			mLockIcon.SetActive(state == EAlbumPictureState.Locked);
			mAvailableGlow.SetActive(state == EAlbumPictureState.Available);

			bool bShowThumbnail = state == EAlbumPictureState.Collected;
			mThumbnailImage.gameObject.SetActive(bShowThumbnail);

			// TODO: Addressables.LoadAssetAsync<Sprite>($"Picture_{picture.PictureId}_Thumb") 로 교체
			if (bShowThumbnail)
			{
				Sprite sprite = Resources.Load<Sprite>($"Picture_{picture.PictureId}_Thumb");
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
