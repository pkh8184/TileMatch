using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 앨범 챕터(그룹) 헤더 셀. 챕터명 + 수집 진행 게이지 표시.
	/// Inspector: mNameText, mProgressBar 연결 필요.
	/// </summary>
	public class AlbumChapterHeader : MonoBehaviour
	{
		[SerializeField] private TMP_Text mNameText;
		[SerializeField] private Image    mProgressBar;

		public void Setup(TBAlbumGroupData group, float progress)
		{
			// 로컬라이제이션 연동 전 임시: GroupNameId 표시
			if (mNameText != null)
			{
				mNameText.text = group.GroupNameId.ToString();
			}
			if (mProgressBar != null)
			{
				mProgressBar.fillAmount = progress;
			}
		}
	}
}
