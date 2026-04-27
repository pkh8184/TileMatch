using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;

namespace TrumpTile.GameMain.Core
{
	public class AlbumManager : Singleton_GameObject<AlbumManager>
	{
		private const string UNLOCK_KEY_PREFIX = "Album_Unlocked_";

		[SerializeField] private TBAlbumGroupTable   mAlbumGroupTable;
		[SerializeField] private TBAlbumPictureTable mAlbumPictureTable;

		private List<TBAlbumPictureData> mRecentlyUnlocked = new List<TBAlbumPictureData>();

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		/// <summary>
		/// 스테이지 클리어 시 호출. 해당 레벨에 해금 조건이 맞는 사진을 즉시 해금하고 보상 지급.
		/// </summary>
		public void CheckAndUnlock(int stageLevel)
		{
			if (mAlbumPictureTable == null || mAlbumPictureTable.items == null)
			{
				return;
			}
			foreach (TBAlbumPictureData picture in mAlbumPictureTable.items)
			{
				if (picture.StageValue == stageLevel && !IsPictureUnlocked(picture.PictureId))
				{
					UnlockPicture(picture);
				}
			}
		}

		private void UnlockPicture(TBAlbumPictureData picture)
		{
			PlayerPrefs.SetInt(UNLOCK_KEY_PREFIX + picture.PictureId, 1);
			PlayerPrefs.Save();

			GiveReward(picture);
			mRecentlyUnlocked.Add(picture);

			EventManager.Inst.ActiveEvent(EventKeys.ALBUM_PHOTO_UNLOCKED, picture);
			CheckChapterComplete(picture.AlbumGroupId);
		}

		private void GiveReward(TBAlbumPictureData picture)
		{
			AddItem(1005, picture.HammerRewardCount);
			AddItem(1006, picture.MagicStickRewardCount);
			AddItem(1007, picture.MagicHatRewardCount);
			AddItem(1008, picture.BombRewardCount);
		}

		private void AddItem(int itemId, int count)
		{
			if (count <= 0)
			{
				return;
			}
			int current = PlayerDataManager.Inst.GetItemCount(itemId);
			PlayerDataManager.Inst.SetItemCount(itemId, current + count);
		}

		private void CheckChapterComplete(int albumGroupId)
		{
			if (mAlbumPictureTable == null)
			{
				return;
			}
			TBAlbumPictureData[] groupPictures = mAlbumPictureTable.GetByGroupId(albumGroupId);
			foreach (TBAlbumPictureData pic in groupPictures)
			{
				if (!IsPictureUnlocked(pic.PictureId))
				{
					return;
				}
			}
			EventManager.Inst.ActiveEvent(EventKeys.ALBUM_CHAPTER_COMPLETE, albumGroupId);
		}

		#region 조회

		public bool IsPictureUnlocked(int pictureId)
		{
			return PlayerPrefs.GetInt(UNLOCK_KEY_PREFIX + pictureId, 0) == 1;
		}

		public bool HasRecentlyUnlocked()
		{
			return mRecentlyUnlocked.Count > 0;
		}

		public List<TBAlbumPictureData> GetRecentlyUnlocked()
		{
			return new List<TBAlbumPictureData>(mRecentlyUnlocked);
		}

		public void ClearRecentlyUnlocked()
		{
			mRecentlyUnlocked.Clear();
		}

		/// <summary>
		/// 특정 그룹의 수집 진행도 (0~1)
		/// </summary>
		public float GetGroupProgress(int albumGroupId)
		{
			if (mAlbumPictureTable == null)
			{
				return 0f;
			}
			TBAlbumPictureData[] pictures = mAlbumPictureTable.GetByGroupId(albumGroupId);
			if (pictures.Length == 0)
			{
				return 0f;
			}
			int unlockedCount = 0;
			foreach (TBAlbumPictureData picture in pictures)
			{
				if (IsPictureUnlocked(picture.PictureId))
				{
					unlockedCount++;
				}
			}
			return (float)unlockedCount / pictures.Length;
		}

		/// <summary>
		/// 현재 진행 중인 그룹 (완료되지 않은 첫 번째 그룹)
		/// </summary>
		public TBAlbumGroupData GetCurrentGroup()
		{
			if (mAlbumGroupTable == null || mAlbumGroupTable.items == null || mAlbumGroupTable.items.Length == 0)
			{
				return null;
			}
			foreach (TBAlbumGroupData group in mAlbumGroupTable.items)
			{
				if (GetGroupProgress(group.AlbumGroupId) < 1f)
				{
					return group;
				}
			}
			return mAlbumGroupTable.items[mAlbumGroupTable.items.Length - 1];
		}

		public TBAlbumGroupData[] GetAllGroups()
		{
			if (mAlbumGroupTable == null || mAlbumGroupTable.items == null)
			{
				return new TBAlbumGroupData[0];
			}
			return mAlbumGroupTable.items;
		}

		public TBAlbumPictureData[] GetGroupPictures(int albumGroupId)
		{
			if (mAlbumPictureTable == null)
			{
				return new TBAlbumPictureData[0];
			}
			return mAlbumPictureTable.GetByGroupId(albumGroupId);
		}

		#endregion
	}
}
