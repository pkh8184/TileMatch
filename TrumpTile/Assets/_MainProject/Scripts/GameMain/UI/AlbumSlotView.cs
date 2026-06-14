using System;
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
	public class AlbumSlotView : MonoBehaviour
	{
		[SerializeField] private Image         mAlbumImage;
		[SerializeField] private GameObject    mLockImage;
		[SerializeField] private RectTransform mLockIcon;		// 흔들 자물쇠 아이콘(텍스트와 분리)
		[SerializeField] private TMP_Text      mNumberText;
		[SerializeField] private Button        mButton;

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

			if (mState == EAlbumPictureState.Locked)
			{
				ShakeLockIcon();
			}
		}

		private void ShakeLockIcon()
		{
			if (mLockIcon == null)
			{
				return;
			}

			// 이전 흔들림이 남아있으면 원위치로 즉시 정리 후 다시 흔든다(누적 드리프트 방지)
			mLockIcon.DOKill(true);
			mLockIcon.DOShakeAnchorPos(0.4F, new Vector2(12F, 0F), 18, 90F, false, true);
		}
	}
}
