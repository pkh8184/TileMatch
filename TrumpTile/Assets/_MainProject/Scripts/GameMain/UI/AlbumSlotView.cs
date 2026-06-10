using System;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumSlotView : MonoBehaviour
	{
		[SerializeField] private Image      mAlbumImage;
		[SerializeField] private GameObject mLockImage;
		[SerializeField] private TMP_Text   mNumberText;
		[SerializeField] private Button     mButton;

		private TBPictureCollectData mPictureData;
		private EAlbumPictureState   mState;
		private Action<TBPictureCollectData, EAlbumPictureState> mOnClick;
		private AsyncOperationHandle<Sprite> mImageHandle;

		private void Awake()
		{
			mButton.onClick.AddListener(OnClick);
		}

		private void OnDestroy()
		{
			if (mImageHandle.IsValid())
			{
				Addressables.Release(mImageHandle);
			}
		}

		public void Setup(TBPictureCollectData picture, int number, EAlbumPictureState state, Action<TBPictureCollectData, EAlbumPictureState> onClick)
		{
			mPictureData = picture;
			mState       = state;
			mOnClick     = onClick;

			mNumberText.text = $"No. {number}";

			bool bViewable = state != EAlbumPictureState.Locked;
			mLockImage.SetActive(!bViewable);
			mAlbumImage.gameObject.SetActive(bViewable);

			if (bViewable)
			{
				LoadAlbumImage(picture.PictureId);
			}
		}

		private void LoadAlbumImage(int pictureId)
		{
			string key = $"Picture_ThumbNail_{pictureId}";
			mImageHandle = Addressables.LoadAssetAsync<Sprite>(key);
			mImageHandle.Completed += handle =>
			{
				if (handle.Status == AsyncOperationStatus.Succeeded)
				{
					mAlbumImage.sprite = handle.Result;
				}
			};
		}

		private void OnClick()
		{
			mOnClick?.Invoke(mPictureData, mState);
		}
	}
}
