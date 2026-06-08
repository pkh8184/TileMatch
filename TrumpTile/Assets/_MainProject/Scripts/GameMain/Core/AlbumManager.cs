using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class AlbumManager : Singleton_GameObject<AlbumManager>
	{
		[Header("데이터 테이블")]
		[SerializeField] private TBAlbumTable   mAlbumTable;
		[SerializeField] private TBPictureTable mPictureTable;

		[Header("아이템 ID (TB_Item 기준)")]
		[SerializeField] private int mHammerItemId     = 1006;
		[SerializeField] private int mMagicStickItemId = 1005;
		[SerializeField] private int mMagicHatItemId   = 1007;
		[SerializeField] private int mBombItemId       = 1008;

		public event System.Action<TBPictureData> OnPictureCollected;
		public event System.Action<TBAlbumData>   OnChapterCompleted;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		public void OnStageClear(int clearedStage)
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);

			foreach (TBPictureData picture in groupPictures)
			{
				if (clearedStage >= picture.StageValue
					&& !PlayerDataManager.Inst.IsPictureCollected(groupId, picture.PictureId))
				{
					PlayerDataManager.Inst.SetPendingAlbumReward(true);
					return;
				}
			}
		}

		public void CheckPendingReward(System.Action<List<TBPictureData>> onPendingFound)
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null || !PlayerDataManager.Inst.HasPendingAlbumReward)
			{
				return;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			List<TBPictureData> pendingPictures = new List<TBPictureData>();
			foreach (TBPictureData picture in groupPictures)
			{
				if (currentStage >= picture.StageValue
					&& !collectedIds.Contains(picture.PictureId))
				{
					pendingPictures.Add(picture);
				}
			}

			if (pendingPictures.Count > 0)
			{
				onPendingFound?.Invoke(pendingPictures);
			}
			else
			{
				PlayerDataManager.Inst.SetPendingAlbumReward(false);
			}
		}

		public void CollectPicture(TBPictureData picture)
		{
			if (picture == null || PlayerDataManager.Inst == null)
			{
				return;
			}

			int groupId = picture.AlbumGroupId;

			PlayerDataManager.Inst.AddCollectedPicture(groupId, picture.PictureId);

			GrantItemRewards(picture);

			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			if (AlbumContent.IsChapterComplete(groupPictures, collectedIds))
			{
				TBAlbumData albumData = mAlbumTable.GetById(groupId);
				if (albumData != null
					&& !PlayerDataManager.Inst.CompletedAlbumGroupIds.Contains(groupId))
				{
					PlayerDataManager.Inst.AddGold(albumData.GoldRewardCount);
					PlayerDataManager.Inst.SetAlbumGroupComplete(groupId);
					OnChapterCompleted?.Invoke(albumData);
				}
			}

			OnPictureCollected?.Invoke(picture);
		}

		public (int collected, int total) GetCurrentProgress()
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return (0, 0);
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			int collected = 0;
			foreach (TBPictureData picture in groupPictures)
			{
				if (collectedIds.Contains(picture.PictureId))
				{
					collected++;
				}
			}

			return (collected, groupPictures.Length);
		}

		public List<(TBPictureData picture, EAlbumPictureState state)> GetCurrentGroupPictureStates()
		{
			List<(TBPictureData, EAlbumPictureState)> result = new List<(TBPictureData, EAlbumPictureState)>();

			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return result;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			foreach (TBPictureData picture in groupPictures)
			{
				EAlbumPictureState state = AlbumContent.GetPictureState(
					picture.PictureId, picture.StageValue, currentStage, collectedIds);
				result.Add((picture, state));
			}

			return result;
		}

		private void GrantItemRewards(TBPictureData picture)
		{
			if (picture.HammerRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mHammerItemId, picture.HammerRewardCount);
			}
			if (picture.MagicStickRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mMagicStickItemId, picture.MagicStickRewardCount);
			}
			if (picture.MagicHatRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mMagicHatItemId, picture.MagicHatRewardCount);
			}
			if (picture.BombRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mBombItemId, picture.BombRewardCount);
			}
		}
	}
}
