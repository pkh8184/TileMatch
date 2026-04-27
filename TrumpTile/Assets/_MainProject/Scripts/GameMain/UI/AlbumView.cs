using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 앨범 전체 목록 화면. 챕터(그룹) 헤더 + 사진 셀 그리드로 구성.
	/// Inspector: mChapterHeaderPrefab, mPhotoCellPrefab, mContentRoot 연결 필요.
	/// </summary>
	public class AlbumView : ViewBase
	{
		[Header("프리팹")]
		[SerializeField] private AlbumChapterHeader      mChapterHeaderPrefab;
		[SerializeField] private AlbumPhotoCell          mPhotoCellPrefab;

		[Header("스크롤 콘텐츠 루트")]
		[SerializeField] private Transform mContentRoot;

		[Header("사진 프리뷰 팝업")]
		[SerializeField] private AlbumPhotoPreviewPopup mPreviewPopup;

		private List<GameObject> mSpawnedItems = new List<GameObject>();

		public override void Show()
		{
			base.Show();
			BuildList();
		}

		private void BuildList()
		{
			ClearList();

			TBAlbumGroupData[] groups = AlbumManager.Inst.GetAllGroups();
			foreach (TBAlbumGroupData group in groups)
			{
				SpawnChapterHeader(group);
				SpawnPhotoCells(group);
			}
		}

		private void SpawnChapterHeader(TBAlbumGroupData group)
		{
			AlbumChapterHeader header = Instantiate(mChapterHeaderPrefab, mContentRoot);
			header.Setup(group, AlbumManager.Inst.GetGroupProgress(group.AlbumGroupId));
			mSpawnedItems.Add(header.gameObject);
		}

		private void SpawnPhotoCells(TBAlbumGroupData group)
		{
			TBAlbumPictureData[] pictures = AlbumManager.Inst.GetGroupPictures(group.AlbumGroupId);
			foreach (TBAlbumPictureData picture in pictures)
			{
				AlbumPhotoCell cell = Instantiate(mPhotoCellPrefab, mContentRoot);
				bool bUnlocked = AlbumManager.Inst.IsPictureUnlocked(picture.PictureId);
				cell.Setup(picture, bUnlocked, OnPhotoCellClicked);
				mSpawnedItems.Add(cell.gameObject);
			}
		}

		private void OnPhotoCellClicked(TBAlbumPictureData picture)
		{
			if (!AlbumManager.Inst.IsPictureUnlocked(picture.PictureId))
			{
				return;
			}
			if (mPreviewPopup != null)
			{
				mPreviewPopup.Show(picture);
			}
		}

		private void ClearList()
		{
			foreach (GameObject item in mSpawnedItems)
			{
				Destroy(item);
			}
			mSpawnedItems.Clear();
		}
	}
}
