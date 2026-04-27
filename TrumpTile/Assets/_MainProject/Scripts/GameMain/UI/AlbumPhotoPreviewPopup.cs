using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 사진 프리뷰 팝업. 사진 이미지 + 제목 + 설명 표시. 사용자 인터랙션 없음 (닫기 버튼만).
	/// Inspector: mPhotoImage, mTitleText, mDescriptionText 연결 필요.
	/// </summary>
	public class AlbumPhotoPreviewPopup : PopupBase
	{
		[Header("사진 표시")]
		[SerializeField] private Image    mPhotoImage;
		[SerializeField] private TMP_Text mTitleText;
		[SerializeField] private TMP_Text mDescriptionText;

		public void Show(TBAlbumPictureData picture, Sprite photoSprite = null)
		{
			if (mPhotoImage != null && photoSprite != null)
			{
				mPhotoImage.sprite = photoSprite;
			}

			// 로컬라이제이션 연동 전 임시: Id를 텍스트로 표시
			if (mTitleText != null)
			{
				mTitleText.text = picture.PictureNameId.ToString();
			}
			if (mDescriptionText != null)
			{
				mDescriptionText.text = picture.PictureDescriptionId.ToString();
			}

			base.Show();
		}
	}
}
