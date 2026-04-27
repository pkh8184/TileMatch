using System;
using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 앨범 사진 셀. 해금/잠금 상태 표시. 클릭 시 OnClick 콜백 호출.
	/// Inspector: mPhotoImage, mLockOverlay, mButton 연결 필요.
	/// </summary>
	public class AlbumPhotoCell : MonoBehaviour
	{
		[SerializeField] private Image    mPhotoImage;
		[SerializeField] private GameObject mLockOverlay;
		[SerializeField] private Button   mButton;

		private TBAlbumPictureData          mPictureData;
		private Action<TBAlbumPictureData>  mOnClick;

		public void Setup(TBAlbumPictureData pictureData, bool bUnlocked, Action<TBAlbumPictureData> onClickCallback)
		{
			mPictureData = pictureData;
			mOnClick     = onClickCallback;

			if (mLockOverlay != null)
			{
				mLockOverlay.SetActive(!bUnlocked);
			}

			if (mButton != null)
			{
				mButton.onClick.RemoveAllListeners();
				mButton.onClick.AddListener(() => mOnClick?.Invoke(mPictureData));
			}
		}

		public void SetSprite(Sprite sprite)
		{
			if (mPhotoImage != null)
			{
				mPhotoImage.sprite = sprite;
			}
		}
	}
}
