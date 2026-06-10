using TMPro;
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

		public override void Initialize()
		{
			base.Initialize();
			mCloseButton.onClick.AddListener(Hide);
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

			// TODO: StringMaster 로컬라이징 연동 후 PictureNameId / PictureDescriptionId로 실제 텍스트 조회
			mTitleText.text       = $"Picture_{picture.PictureId}";
			mDescriptionText.text = string.Empty;
		}
	}
}
