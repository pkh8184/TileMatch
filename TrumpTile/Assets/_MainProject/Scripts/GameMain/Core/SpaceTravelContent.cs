using System;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	[Serializable]
	public class SpaceTravelContent : TemporaryContent
	{
		[Header("최종 보상 목록")]
		[SerializeReference, SubclassSelector] private ProductReward[] mFinalRewardArray;
		[Header("목표 연승 수")]
		[SerializeField] private int mTargetStreakCount = 7;
		[Header("시작 가상 인원")]
		[SerializeField] private int mStartFakePlayerCount = 100;
		[Header("단계별 최소 탈락 수 (7개)")]
		[SerializeField] private int[] mMinEliminationPerStage = { 5, 7, 8, 8, 10, 12, 20 };

		private ESpaceTravelState mState;
		private int mCurrentStreakCount;
		private int mCurrentFakePlayerCount;

		private const float ACTIVE_LIMIT_SECONDS     = 3600F;
		private const float FAIL_COOLDOWN_SECONDS    = 3600F;
		private const float SUCCESS_COOLDOWN_SECONDS = 86400F;

		public override void Initialize()
		{
			base.Initialize();

			mState                  = PlayerDataManager.Inst.SpaceTravelState;
			mCurrentStreakCount     = PlayerDataManager.Inst.SpaceTravelStreakCount;
			mCurrentFakePlayerCount = PlayerDataManager.Inst.SpaceTravelFakePlayerCount;

			CheckCooldownExpired();

			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_STAGE_CLEAR, OnStageClear);
			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_STAGE_FAIL, OnStageFail);
		}

		public override void Refresh()
		{
			base.Refresh();

			mState                  = PlayerDataManager.Inst.SpaceTravelState;
			mCurrentStreakCount     = PlayerDataManager.Inst.SpaceTravelStreakCount;
			mCurrentFakePlayerCount = PlayerDataManager.Inst.SpaceTravelFakePlayerCount;

			CheckCooldownExpired();
		}

		public override void CheckUnlock()
		{
			if (PlayerDataManager.Inst.CurrentStage > mLevelToUnlock)
			{
				SetUnlock();

				if (!PlayerDataManager.Inst.IsSpaceTravelUnlocked)
				{
					PlayerDataManager.Inst.UnlockSpaceTravel();
					mbShowUnlockPopup = true;
				}
				else
				{
					mbShowUnlockPopup = false;
				}
			}
		}

		public ESpaceTravelState GetState() => mState;
		public int GetStreakCount() => mCurrentStreakCount;
		public int GetFakePlayerCount() => mCurrentFakePlayerCount;
		public int GetTargetStreakCount() => mTargetStreakCount;

		public float GetRemainingTime()
		{
			if (mState != ESpaceTravelState.Active)
			{
				return 0F;
			}
			float elapsed = (float)(DateTime.UtcNow - PlayerDataManager.Inst.SpaceTravelStateChangeTime).TotalSeconds;
			return Mathf.Max(0F, ACTIVE_LIMIT_SECONDS - elapsed);
		}

		public float GetCooldownTime()
		{
			if (mState != ESpaceTravelState.SuccessCooldown && mState != ESpaceTravelState.FailCooldown)
			{
				return 0F;
			}
			float cooldown = mState == ESpaceTravelState.SuccessCooldown
				? SUCCESS_COOLDOWN_SECONDS
				: FAIL_COOLDOWN_SECONDS;
			float elapsed = (float)(DateTime.UtcNow - PlayerDataManager.Inst.SpaceTravelStateChangeTime).TotalSeconds;
			return Mathf.Max(0F, cooldown - elapsed);
		}

		public int GetEliminationForStage(int streakIndexBeforeClear)
		{
			int[] budget = PlayerDataManager.Inst.SpaceTravelEliminationBudget;
			if (budget == null || streakIndexBeforeClear >= budget.Length)
			{
				return 0;
			}
			return budget[streakIndexBeforeClear];
		}

		public void StartEvent()
		{
			int[] budget = CalculateEliminationBudget(
				mStartFakePlayerCount,
				mMinEliminationPerStage,
				mTargetStreakCount,
				(min, max) => UnityEngine.Random.Range(min, max)
			);
			PlayerDataManager.Inst.StartSpaceTravel(budget, mStartFakePlayerCount);
			mState                  = ESpaceTravelState.Active;
			mCurrentStreakCount     = 0;
			mCurrentFakePlayerCount = mStartFakePlayerCount;
			mbHasNewthing           = false;
		}

		public void GrantFinalReward()
		{
			List<RewardDisplayInfo> infos = new List<RewardDisplayInfo>();
			foreach (ProductReward reward in mFinalRewardArray)
			{
				reward.GrantReward();
				RewardDisplayInfo info = reward.GetRewardDisplayInfo();
				infos.Add(info);
				CoreContainer.RewardContainer.AddReward(info);
			}
			PlayerDataManager.Inst.CompleteSpaceTravel();
			mState = ESpaceTravelState.SuccessCooldown;
		}

		private void OnStageClear()
		{
			if (mState != ESpaceTravelState.Active)
			{
				return;
			}
			int elimination = GetEliminationForStage(mCurrentStreakCount);
			mCurrentStreakCount++;
			mCurrentFakePlayerCount = Mathf.Max(0, mCurrentFakePlayerCount - elimination);

			PlayerDataManager.Inst.OnSpaceTravelStageClear(mCurrentStreakCount, mCurrentFakePlayerCount);

			if (mCurrentStreakCount >= mTargetStreakCount)
			{
				EventManager.Inst.ActiveEvent(EventKeys.SPACE_TRAVEL_SHOW_REWARD);
			}
		}

		private void OnStageFail()
		{
			if (mState != ESpaceTravelState.Active)
			{
				return;
			}
			PlayerDataManager.Inst.FailSpaceTravel();
			mState              = ESpaceTravelState.FailCooldown;
			mCurrentStreakCount = 0;
		}

		private void CheckCooldownExpired()
		{
			if (mState == ESpaceTravelState.Active)
			{
				if (GetRemainingTime() <= 0F)
				{
					PlayerDataManager.Inst.FailSpaceTravel();
					mState              = ESpaceTravelState.FailCooldown;
					mCurrentStreakCount = 0;
				}
				return;
			}

			if (mState == ESpaceTravelState.FailCooldown || mState == ESpaceTravelState.SuccessCooldown)
			{
				if (GetCooldownTime() <= 0F)
				{
					PlayerDataManager.Inst.ResetSpaceTravel();
					mState                  = ESpaceTravelState.Idle;
					mCurrentStreakCount     = 0;
					mCurrentFakePlayerCount = 0;
				}
			}
		}

		internal static int[] CalculateEliminationBudget(
			int startCount,
			int[] minPerStage,
			int stageCount,
			System.Func<int, int, int> randomRange)
		{
			int remaining        = randomRange(1, 4);
			int totalElimination = startCount - remaining;

			int[] result        = new int[stageCount];
			int guaranteedTotal = 0;
			for (int i = 0; i < stageCount; i++)
			{
				result[i]        = minPerStage[i];
				guaranteedTotal += minPerStage[i];
			}

			int freeBudget = totalElimination - guaranteedTotal;
			if (freeBudget < 0)
			{
				freeBudget = 0;
			}

			for (int i = 0; i < stageCount - 1 && freeBudget > 0; i++)
			{
				int remainingStages = stageCount - i;
				int maxAdd          = (freeBudget / remainingStages) + 1;
				int add             = randomRange(0, maxAdd + 1);
				if (add > freeBudget)
				{
					add = freeBudget;
				}
				result[i]  += add;
				freeBudget -= add;
			}
			result[stageCount - 1] += freeBudget;

			return result;
		}
	}
}
