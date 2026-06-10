using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class AlbumManager : Singleton_GameObject<AlbumManager>
	{
		[Header("데이터 테이블")]
		[SerializeField] private TBPictureCollectTable mPictureTable;

		[Header("아이템 ID (TB_Item 기준)")]
		[SerializeField] private int mHammerItemId = 1006;
		[SerializeField] private int mClockItemId  = 1005;
		[SerializeField] private int mHatItemId    = 1007;
		[SerializeField] private int mBombItemId   = 1008;

		public event System.Action<TBPictureCollectData> OnPictureCollected;

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

			TBPictureCollectData[] allPictures = mPictureTable.GetAll();
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds();

			foreach (TBPictureCollectData picture in allPictures)
			{
				if (clearedStage >= picture.StageValue && !collectedIds.Contains(picture.PictureId))
				{
					PlayerDataManager.Inst.SetPendingAlbumReward(true);
					return;
				}
			}
		}

		public void CheckPendingReward(System.Action<List<TBPictureCollectData>> onPendingFound)
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null || !PlayerDataManager.Inst.HasPendingAlbumReward)
			{
				return;
			}

			TBPictureCollectData[] allPictures = mPictureTable.GetAll();
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds();

			List<TBPictureCollectData> pendingPictures = new List<TBPictureCollectData>();
			foreach (TBPictureCollectData picture in allPictures)
			{
				if (currentStage >= picture.StageValue && !collectedIds.Contains(picture.PictureId))
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

		public void CollectPicture(TBPictureCollectData picture)
		{
			if (picture == null || PlayerDataManager.Inst == null)
			{
				return;
			}

			PlayerDataManager.Inst.AddCollectedPicture(picture.PictureId);

			if (picture.GoldRewardCount > 0)
			{
				PlayerDataManager.Inst.AddGold(picture.GoldRewardCount);
			}

			GrantItemRewards(picture);

			OnPictureCollected?.Invoke(picture);
		}

		public (int collected, int total) GetCurrentProgress()
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return (0, 0);
			}

			TBPictureCollectData[] allPictures = mPictureTable.GetAll();
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds();

			int collected = 0;
			foreach (TBPictureCollectData picture in allPictures)
			{
				if (collectedIds.Contains(picture.PictureId))
				{
					collected++;
				}
			}

			return (collected, allPictures.Length);
		}

		public List<(TBPictureCollectData picture, EAlbumPictureState state)> GetPictureStates()
		{
			List<(TBPictureCollectData, EAlbumPictureState)> result = new List<(TBPictureCollectData, EAlbumPictureState)>();

			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return result;
			}

			TBPictureCollectData[] allPictures = mPictureTable.GetAll();
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds();

			foreach (TBPictureCollectData picture in allPictures)
			{
				EAlbumPictureState state = AlbumContent.GetPictureState(
					picture.PictureId, picture.StageValue, currentStage, collectedIds);
				result.Add((picture, state));
			}

			return result;
		}

		private void GrantItemRewards(TBPictureCollectData picture)
		{
			if (picture.HammerRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mHammerItemId, picture.HammerRewardCount);
			}
			if (picture.ClockRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mClockItemId, picture.ClockRewardCount);
			}
			if (picture.HatRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mHatItemId, picture.HatRewardCount);
			}
			if (picture.BombRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mBombItemId, picture.BombRewardCount);
			}
		}
	}
}
