using System;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;
using System.IO;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.Data
{
	public enum EPlayerDataType
	{
		//재화
		Gold,
		Star,
		//아이템
		Bomb,
		BlackHole,
		Timer,
		//스테이지
		CurrentStage,
		FirstTryClearCount,
		MaxStreakClearStageCount,
		ClearedStage,
		CurrentStageForStageStart,
		//하우징
		CurrentHousingChapter,
		CurrentHousingSubChapter,
		CompletedChapterCount,
		//로그인
		MaxStreakLoginCount,
		FirstLoginDate,
		//UID
		UID,
		TermsAndConditionVersion
	}

	public class PlayerDataManager : Singleton_GameObject<PlayerDataManager>
	{
		private UserData mUserData = null;
		public UserData UserData { get => mUserData; }

		// 선택한 스테이지 (메인 맵에서 선택한 값, 서버 저장 불필요)
		private int mSelectedStage = 0;

		[Header("에디터 테스트용 (Firebase 없이 실행 시 적용)")]
		[SerializeField] private int mTestCurrentStage = 1;
		[SerializeField] private int mTestLastAlbumRewardedStage = 0;

		public event Action OnGoldChanged;
		public event Action<int> OnStageChanged;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
			LoadUserData();
		}
		private void OnApplicationQuit()
        {
            SaveUserData();
            FlushServerSaveIfPending();
        }
        private void OnApplicationPause(bool pause)
        {
            if(pause)
			{
				SaveUserData();
				//백그라운드로 가면 디바운스 타이머가 끝까지 못 돌 수 있으니 예약분을 여기서 올린다.
				FlushServerSaveIfPending();
				return;
			}

			//백그라운드에 있는 동안 자정을 넘겼을 수 있다. 복귀 시점에 하루 경계를 다시 판정한다.
			if(RefreshDailyReset())
			{
				EventManager.Inst?.ActiveEvent(EventKeys.CONTENT_DATA_REFRESH);
			}
        }
        public void Initialize(Dictionary<object, object> dictionary)
		{
			//mUserData = new UserData(dictionary);
		}

		public void Initialize()
		{
			// if (mUserData != null)
			// {
			// 	return;
			// }

			// mUserData = new UserData();
			// mUserData.InitOnlyLoacalData();
			// mUserData.CurrentStage = mTestCurrentStage;
			// mUserData.LastAlbumRewardedStage  = mTestLastAlbumRewardedStage;
		}
		#region 프로퍼티

		public int Gold => mUserData != null ? mUserData.Gold : 0;
		public int CurrentStage => mUserData != null ? mUserData.CurrentStage : 1;
		public int MaxClearedStage => CurrentStage - 1;
		public int SelectedStage => mSelectedStage;
		public bool IsAdsRemoved => mUserData != null && mUserData.RemoveAds;
		public int LastAlbumRewardedStage => mUserData != null ? mUserData.LastAlbumRewardedStage : 0;
		public int StreakLoginCount => mUserData.StreakLoginCount;
		public int RouletteCount => mUserData.RouletteCount;
		public int ExcitTravelIndex => mUserData.ExcitTravelIndex;
		public bool IsExcitTravelActive => mUserData.IsExcitTravelActive;
		public int PiggyBankStageClearCount => mUserData.PiggyBankStageClearCount;
		public bool IsPiggyBankActive => mUserData.IsPiggyBankActive;
		public bool PiggyBankPurchase => mUserData.PiggyBankPurchase;
		public bool IsGemCollectionActive => mUserData.IsGemCollectionActive;
		public int GemCollectionIndex => mUserData.GemCollectionIndex;
		public int GemCollectionCount => mUserData.GemCount;

		//젬 2배 버프
		public bool IsGemDoubleActive => mUserData != null && mUserData.IsGemDoubleActive;
		public DateTime GemDoubleExpireDate => mUserData != null ? mUserData.GemDoubleExpireDate : default;
		//스테이지에서 젬을 획득할 때 곱할 배율. 버프가 없으면 1.
		public int GemMultiplier => IsGemDoubleActive ? 2 : 1;
		public int ChampionsLevel => mUserData.ChampionsLevel;
		public bool IsChampionsActive => mUserData.IsChampionsActive;

		//컨텐츠 활성/비활성 시각 (쿨타임 비교용 - UTC)
		public DateTime ExcitTravelActiveDate => mUserData.ExcitTravelActiveDate;
		public DateTime ExcitTravelUnActiveDate => mUserData.ExcitTravelUnActiveDate;
		public DateTime PiggyBankActiveDate => mUserData.PiggyBankActiveDate;
		public DateTime PiggyBankUnActiveDate => mUserData.PiggyBankUnActiveDate;
		public DateTime GemCollectionActiveDate => mUserData.GemCollectionActiveDate;
		public DateTime GemCollectionUnActiveDate => mUserData.GemCollectionUnActiveDate;
		//트레져 박스 재활성화 시각 (구매 후 이 시각이 지나면 다시 활성 - UTC)
		public DateTime TreasureBoxActiveDate => mUserData.TreasureBoxActiveDate;
		#endregion

		#region 재화

		public bool HasEnoughGold(int amount) => Gold >= amount;

		public void AddGold(int amount)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.Gold += amount;
			OnGoldChanged?.Invoke();

			SaveUserData();
			MarkServerSaveDirty();
		}

		public bool UseGold(int amount)
		{
			if (!HasEnoughGold(amount))
			{
				return false;
			}
			mUserData.Gold -= amount;
			OnGoldChanged?.Invoke();

			SaveUserData();
			MarkServerSaveDirty();
			return true;
		}

		#endregion

		#region 스테이지

		public void SetSelectedStage(int stage)
		{
			mSelectedStage = stage;
		}

		public bool IsStageCleared(int stage) => stage < CurrentStage;

		public bool CanPlayStage(int stage) => stage <= CurrentStage;

		public void ClearStage(int level, int stars)
		{
			if (mUserData == null)
			{
				return;
			}
			if (level >= CurrentStage)
			{
				mUserData.CurrentStage = level + 1;
				OnStageChanged?.Invoke(mUserData.CurrentStage);
			}
			SaveStageStars(level, stars);

			if(mUserData.IsPiggyBankActive)
			{
				mUserData.PiggyBankStageClearCount++;
			}
			if(CurrentStage > CoreData.MAX_STAGE)
			{
				mUserData.IsChampionsActive = true;
				mUserData.ChampionsLevel = 1;
			}

			SaveUserData();
		}
		public void ClearChampionsStage()
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ChampionsLevel++;

			SaveUserData();
		}
		public int GetStageStars(int level)
		{
			return PlayerPrefs.GetInt($"Stage_{level}_Stars", 0);
		}

		private void SaveStageStars(int level, int stars)
		{
			int currentStars = GetStageStars(level);
			if (stars > currentStars)
			{
				PlayerPrefs.SetInt($"Stage_{level}_Stars", stars);
				PlayerPrefs.Save();
			}
		}

		#endregion

		#region 아이템

		public int GetItemCount(int itemId)
		{
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return 0;
			}
			int count;
			return mUserData.ItemCounts.TryGetValue(itemId, out count) ? count : 0;
		}

		public void SetItemCount(int itemId, int count)
		{
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return;
			}
			mUserData.ItemCounts[itemId] = count;

			SaveUserData();
			MarkServerSaveDirty();
		}
		public void AddItemCount(int itemId, int count)
		{
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return;
			}
			mUserData.ItemCounts[itemId] += count;

			SaveUserData();
			MarkServerSaveDirty();
		}

		public Dictionary<int, int> GetAllItemCounts()
		{
			Dictionary<int, int> result = new Dictionary<int, int>();
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return result;
			}
			foreach (KeyValuePair<int, int> pair in mUserData.ItemCounts)
			{
				result[pair.Key] = pair.Value;
			}
			return result;
		}

	#endregion

	#region 컨텐츠 해금
		public void RemoveAds()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 광고 제거 구독 완료");
			mUserData.RemoveAds = true;

			SaveUserData();
		}
		public void UnlockSeasonPass()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 시즌패스 해금 완료");
			mUserData.SeasonPassUnlock = true;

			SaveUserData();
		}
		public void UnlockPiggyBank()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 돼지저금통 해금 완료");
			mUserData.PiggyBankUnlock = true;
			StartPiggyBankContent();
		}
		public void UnlockDailyCheck()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 출석체크 해금 완료");
			mUserData.DailyCheckUnlock = true;

			SaveUserData();
		}
		public void UnlockRoulette()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 룰렛 해금 완료");
			mUserData.RouletteUnlock = true;

			SaveUserData();
		}
		public void UnlockExcitTravel()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 기차 여행 해금 완료");
			mUserData.ExcitTravelUnlock = true;

			SaveUserData();
		}
		public void UnlockGemCollection()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 보석 수집 해금 완료");
			mUserData.GemCollectionUnlock = true;
			//활성화는 컨텐츠 쪽 EvaluateActiveState에서 처리 (여기서 강제 활성화하지 않음)

			SaveUserData();
		}
		public void UnlockTreasureBox()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 트레져 박스 해금 완료");
			mUserData.TreasureBoxUnlock = true;

			SaveUserData();
		}

		/// <summary>
		/// 트레져 박스 구매 시 호출. 재활성화 시각을 (지금 + 쿨타임)으로 저장해 그때까지 비활성 처리한다. (UTC)
		/// </summary>
		public void StartTreasureBoxCoolTime(double coolTimeSeconds)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.TreasureBoxActiveDate = GameTime.UtcNow.AddSeconds(coolTimeSeconds);
			SaveUserData();
		}
		public void UnlockChampions()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 챔피언스 해금 완료");
			mUserData.ChampionsUnlock = true;

			SaveUserData();
		}

	#endregion

	#region 앨범 수집

		public void SetLastAlbumRewardedStage(int stage)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.LastAlbumRewardedStage = stage;

			SaveUserData();
		}

	#endregion
	#region 출석체크 관련
		public bool CanActiveDailyCheck()
		{
			if(mUserData == null)
			{
				return false;
			}
			return !mUserData.IsDailyCheckToday;
		}
		/// <summary>
		/// 마지막 판정일(LastDailyResetDate)과 오늘(로컬)을 비교해 연속로그인/일일 컨텐츠를 갱신한다.
		/// 판정이 끝나면 LastDailyResetDate를 오늘로 찍어, 같은 날 여러 번 불려도 한 번만 적용되게 한다.
		/// </summary>
		private void RefreshStreakLoginCount()
		{
			if(mUserData == null)
			{
				return;
			}
			int day = (GameTime.Today - mUserData.LastDailyResetDate.Date).Days;
			if(day == 1)
			{
				mUserData.StreakLoginCount++;
				mUserData.IsDailyCheckToday = false;
			}
			else if(day > 1)
			{
				mUserData.StreakLoginCount = 1;
				mUserData.IsDailyCheckToday = false;
			}

			//날짜가 바뀌면 일일 컨텐츠 초기화(룰렛 이용 횟수 등)
			if(day >= 1)
			{
				mUserData.RouletteCount = 0;
			}

			//기기 시계를 되돌린 경우(day < 0)에도 여기서 오늘로 맞춰야 판정이 멈추지 않는다.
			mUserData.LastDailyResetDate = GameTime.Today;

			SaveUserData();
		}
		/// <summary>
		/// 마지막 판정 이후 로컬 날짜가 바뀌었으면 일일 컨텐츠(출석·룰렛)를 다시 초기화한다.
		/// 앱 실행 시 1회(LoadUserData)뿐 아니라 메인씬 진입/앱 복귀 시점에도 호출해,
		/// DailyPuzzle(GameTime.Today로 매번 판정)과 초기화 시점을 맞춘다.
		/// </summary>
		/// <returns>날짜가 바뀌어 실제로 초기화했으면 true</returns>
		public bool RefreshDailyReset()
		{
			if(mUserData == null)
			{
				return false;
			}
			//마지막 판정이 오늘(로컬)이면 넘어갈 하루 경계가 없다.
			if(mUserData.LastDailyResetDate.Date == GameTime.Today)
			{
				return false;
			}

			RefreshStreakLoginCount();

			Debug.Log($"[PlayerDataManager] 일일 리셋 재평가 → streak={mUserData.StreakLoginCount}, dailyChecked={mUserData.IsDailyCheckToday}, roulette={mUserData.RouletteCount}");
			return true;
		}
		public void SetDailyCheckDone()
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.IsDailyCheckToday = true;

			SaveUserData();
		}
	#endregion
	#region 룰렛 관련
		public void SetRouletteCount(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.RouletteCount = value;

			SaveUserData();
		}
	#endregion
	#region 돼지저금통 관련
		public void PurchasePiggyBank()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 돼지저금통 구매 완료");
			mUserData.PiggyBankPurchase = true;

			SaveUserData();
		}
		public void StartPiggyBankContent()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 돼지저금통 컨텐트 시작");
			mUserData.IsPiggyBankActive = true;
			mUserData.PiggyBankActiveDate = GameTime.UtcNow;
			//새 사이클 진행도 초기화
			mUserData.PiggyBankStageClearCount = 0;
			mUserData.PiggyBankPurchase = false;
			SaveUserData();
		}
		public void EndPiggyBankContent()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 돼지저금통 컨텐트 종료");
			mUserData.IsPiggyBankActive = false;
			mUserData.PiggyBankUnActiveDate = GameTime.UtcNow;

			mUserData.PiggyBankPurchase = false;
			mUserData.PiggyBankStageClearCount = 0;
			SaveUserData();
		}
	#endregion
	#region 기차 여행 관련
		public void IncreaseExcitTravelIndex()
		{
			mUserData.ExcitTravelIndex++;

			SaveUserData();
		}
		public void ActiveExcitTravel()
		{
			mUserData.IsExcitTravelActive = true;
			mUserData.ExcitTravelActiveDate = GameTime.UtcNow;
			//새 사이클 진행도 초기화
			mUserData.ExcitTravelIndex = 0;
			SaveUserData();
		}
		public void UnActiveExcitTravel()
		{
			mUserData.IsExcitTravelActive = false;
			mUserData.ExcitTravelUnActiveDate = GameTime.UtcNow;
			SaveUserData();
		}
	#endregion
	#region 보석 수집 관련
		public void ActiveGemCollection()
		{
			mUserData.IsGemCollectionActive = true;
			//기존 버그 수정: 활성화 시각은 ActiveDate에 저장 (이전엔 UnActiveDate에 잘못 저장)
			mUserData.GemCollectionActiveDate = GameTime.UtcNow;
			//새 사이클 진행도 초기화
			mUserData.GemCollectionIndex = 0;
			mUserData.GemCount = 0;
			SaveUserData();
		}
		public void UnActiveGemCollection()
		{
			mUserData.IsGemCollectionActive = false;
			mUserData.GemCollectionUnActiveDate = GameTime.UtcNow;
			SaveUserData();
		}
		/// <summary>
		/// 젬 2배 버프 시간을 추가한다.
		/// 이미 버프 중이면 남은 시간에 이어 붙이고(중첩), 아니면 지금부터 시작한다.
		/// 쿨타임 컨벤션대로 UTC 기준으로 저장한다.
		/// </summary>
		public void AddGemDoubleTime(int minutes)
		{
			if(mUserData == null || minutes <= 0)
			{
				return;
			}

			//만료 시각이 이미 지났으면 남은 시간이 없는 것이므로 지금부터 다시 센다.
			bool bStillRunning = mUserData.IsGemDoubleActive && mUserData.GemDoubleExpireDate > GameTime.UtcNow;
			DateTime baseDate = bStillRunning ? mUserData.GemDoubleExpireDate : GameTime.UtcNow;

			mUserData.GemDoubleExpireDate = baseDate.AddMinutes(minutes);
			mUserData.IsGemDoubleActive = true;

			Debug.Log($"[PlayerDataManager] 젬 2배 버프 +{minutes}분 → 만료(UTC): {mUserData.GemDoubleExpireDate}");
			SaveUserData();
		}

		/// <summary>
		/// 젬 2배 버프의 만료 여부를 시간 기준으로 재평가한다.
		/// 메인씬 진입/컨텐츠 갱신 시점에 다른 컨텐츠 쿨타임과 함께 호출된다.
		/// </summary>
		public void EvaluateGemDoubleState()
		{
			if(mUserData == null || !mUserData.IsGemDoubleActive)
			{
				return;
			}
			if(mUserData.GemDoubleExpireDate > GameTime.UtcNow)
			{
				return;
			}

			mUserData.IsGemDoubleActive = false;
			Debug.Log("[PlayerDataManager] 젬 2배 버프 만료");
			SaveUserData();
		}

		/// <summary>젬 2배 버프의 남은 시간(초). 비활성이면 0. (UI 타이머 표기용)</summary>
		public float GetGemDoubleRemainSeconds()
		{
			if(!IsGemDoubleActive)
			{
				return 0f;
			}

			double remain = (mUserData.GemDoubleExpireDate - GameTime.UtcNow).TotalSeconds;
			return remain > 0 ? (float)remain : 0f;
		}

		public void AddGemCount(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCount += value;
			if(mUserData.GemCount < 0) mUserData.GemCount = 0;

			SaveUserData();
		}
		public void SetGemCount(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCount = value;
			if(mUserData.GemCount < 0) mUserData.GemCount = 0;

			SaveUserData();
		}
		public void SetGemIndex(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCollectionIndex = value;

			SaveUserData();
		}
	#endregion
	#region 구매 패키지 (초보자/중급자/상급자 재구매 쿨타임)

		/// <summary>
		/// 패키지 구매 기록. 구매 플래그를 true로 세우고 구매 시각(UTC)을 저장한다.
		/// </summary>
		public void PurchasePackage(EProductId eProductId)
		{
			if(mUserData == null)
			{
				return;
			}
			SetPackagePurchased(eProductId, true);
			SetPackagePurchaseDate(eProductId, GameTime.UtcNow);
			SaveUserData();
			Debug.Log($"[PlayerDataManager] 패키지 구매 기록: {eProductId}");
		}

		/// <summary>
		/// 현재 구매 상태 (쿨타임 재평가는 하지 않음).
		/// </summary>
		public bool IsPackagePurchased(EProductId eProductId)
		{
			return mUserData != null && GetPackagePurchased(eProductId);
		}

		/// <summary>
		/// 재구매 가능 여부. 구매 후 리필 시간(RefreshTimeMinute*60초)이 지났으면
		/// 구매 플래그를 false로 리셋하고 true를 반환한다.
		/// </summary>
		public bool CanPurchasePackage(EProductId eProductId)
		{
			if(mUserData == null)
			{
				return false;
			}

			//구매한 적 없으면 바로 구매 가능
			if(!GetPackagePurchased(eProductId))
			{
				return true;
			}

			int refreshSeconds = GetPackageRefreshSeconds(eProductId);
			if(refreshSeconds <= 0)
			{
				//리필 시간 미설정(0) → 쿨타임 없이 항상 재구매 가능
				SetPackagePurchased(eProductId, false);
				SaveUserData();
				return true;
			}

			double elapsed = (GameTime.UtcNow - GetPackagePurchaseDate(eProductId)).TotalSeconds;
			if(elapsed >= refreshSeconds)
			{
				//쿨타임 종료 → 재구매 가능하도록 플래그 리셋
				SetPackagePurchased(eProductId, false);
				SaveUserData();
				return true;
			}
			return false;
		}

		/// <summary>
		/// 3개 패키지의 쿨타임을 일괄 재평가 (지난 것은 플래그 리셋).
		/// 접속/컨텐츠 갱신 시점에 호출. (IAPManager 초기화 이후에 호출해야 리필 시간을 읽을 수 있음)
		/// </summary>
		public void RefreshAllPackagePurchaseStates()
		{
			CanPurchasePackage(EProductId.NewbiePackage);
			CanPurchasePackage(EProductId.BigginerPackage);
			CanPurchasePackage(EProductId.MasterPackage);
		}

		private int GetPackageRefreshSeconds(EProductId eProductId)
		{
			if(IAPManager.Instance == null)
			{
				return 0;
			}
			return IAPManager.Instance.GetPackageRefreshSeconds(eProductId);
		}

		private bool GetPackagePurchased(EProductId eProductId)
		{
			switch(eProductId)
			{
				case EProductId.NewbiePackage:   return mUserData.NewbiePackagePurchased;
				case EProductId.BigginerPackage: return mUserData.BigginerPackagePurchased;
				case EProductId.MasterPackage:   return mUserData.MasterPackagePurchased;
				default:                         return false;
			}
		}

		private void SetPackagePurchased(EProductId eProductId, bool value)
		{
			switch(eProductId)
			{
				case EProductId.NewbiePackage:   mUserData.NewbiePackagePurchased = value;   break;
				case EProductId.BigginerPackage: mUserData.BigginerPackagePurchased = value; break;
				case EProductId.MasterPackage:   mUserData.MasterPackagePurchased = value;   break;
			}
		}

		private DateTime GetPackagePurchaseDate(EProductId eProductId)
		{
			switch(eProductId)
			{
				case EProductId.NewbiePackage:   return mUserData.NewbiePackagePurchaseDate;
				case EProductId.BigginerPackage: return mUserData.BigginerPackagePurchaseDate;
				case EProductId.MasterPackage:   return mUserData.MasterPackagePurchaseDate;
				default:                         return default;
			}
		}

		private void SetPackagePurchaseDate(EProductId eProductId, DateTime value)
		{
			switch(eProductId)
			{
				case EProductId.NewbiePackage:   mUserData.NewbiePackagePurchaseDate = value;   break;
				case EProductId.BigginerPackage: mUserData.BigginerPackagePurchaseDate = value; break;
				case EProductId.MasterPackage:   mUserData.MasterPackagePurchaseDate = value;   break;
			}
		}

	#endregion
	#region Getters (기존)

		public (bool BGMOn, bool SFXOn, bool HapticOn) GetUserSoundSettingDatas()
		{
			if (mUserData == null)
			{
				return (false, false, false);
			}
			return (mUserData.BGMOn, mUserData.SFXOn, mUserData.HapticOn);
		}

		public int GetProfileImageIndex()
		{
			if (mUserData == null)
			{
				return 0;
			}
			return mUserData.ProfileImageIndex;
		}

		public int GetProfileFrameIndex()
		{
			if (mUserData == null)
			{
				return 0;
			}
			return mUserData.ProfileFrameIndex;
		}

		public string GetNickname()
		{
			if (mUserData == null)
			{
				return string.Empty;
			}
			return mUserData.NickName;
		}

		public int GetLocaleIndex()
		{
			if (mUserData == null)
			{
				return 0;
			}
			return mUserData.LocaleIndex;
		}

		#endregion

		#region Setters (기존)

		//로컬 설정은 PlayerPrefs에 쓰기만 하면 실제 디스크 반영이 앱 종료 시점까지 미뤄진다.
		//강제 종료·크래시로 종료 콜백이 안 오면 설정이 통째로 날아가므로 변경 즉시 Save한다.
		public void SetProfileImageIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ProfileImageIndex = index;
			PlayerPrefs.SetInt(UserData.KEY_PROFILE_IMAGE_INDEX, index);
			PlayerPrefs.Save();
		}

		public void SetProfileFrameIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ProfileFrameIndex = index;
			PlayerPrefs.SetInt(UserData.KEY_PROFILE_FRAME_INDEX, index);
			PlayerPrefs.Save();
		}

		public void SetBGMOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.BGMOn = isOn;
			PlayerPrefs.SetInt(UserData.KEY_BGM_ON, isOn ? 1 : 0);
			PlayerPrefs.Save();
		}

		public void SetSFXOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.SFXOn = isOn;
			PlayerPrefs.SetInt(UserData.KEY_SFX_ON, isOn ? 1 : 0);
			PlayerPrefs.Save();
		}

		public void SetHapticOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.HapticOn = isOn;
			PlayerPrefs.SetInt(UserData.KEY_HAPTIC_ON, isOn ? 1 : 0);
			PlayerPrefs.Save();
		}

		public void SetLocaleIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.LocaleIndex = index;
			PlayerPrefs.SetInt(UserData.KEY_LOCALE_INDEX, index);
			PlayerPrefs.Save();
		}

		#endregion

		public string GetDataToString(EPlayerDataType ePlayerDataType)
		{
			if (mUserData == null)
			{
				return string.Empty;
			}
			string data = null;

			switch (ePlayerDataType)
			{
				case EPlayerDataType.Gold:
					data = mUserData.Gold.ToString("N0");
					break;
				case EPlayerDataType.Star:
					data = mUserData.Star.ToString("N0");
					break;
				case EPlayerDataType.Bomb:
					data = GetItemCount(1008).ToString("N0");
					break;
				case EPlayerDataType.BlackHole:
					data = GetItemCount(1007).ToString("N0");
					break;
				case EPlayerDataType.Timer:
					data = GetItemCount(1005).ToString("N0");
					break;
				case EPlayerDataType.CurrentStage:
					data = mUserData.CurrentStage.ToString();
					break;
				case EPlayerDataType.FirstTryClearCount:
					data = mUserData.FirstTryClearCount.ToString();
					break;
				case EPlayerDataType.MaxStreakClearStageCount:
					data = mUserData.MaxStreakClearStageCount.ToString();
					break;
				case EPlayerDataType.ClearedStage:
					data = (mUserData.CurrentStage - 1).ToString();
					break;
				case EPlayerDataType.CurrentStageForStageStart:
					data = "LEVEL " + mUserData.CurrentStage.ToString();
					break;
				case EPlayerDataType.MaxStreakLoginCount:
					data = mUserData.MaxStreakLoginCount.ToString();
					break;
				case EPlayerDataType.FirstLoginDate:
					string date = mUserData.FirstLoginDate.ToString().Substring(0, 10);
					data = "플레이 시작 시점 : " + date;
					break;
				default:
					break;
			}
			return data;
		}
		public void SetNickName(string nickName)
		{
            if (mUserData == null)
            {
				return;
            }
			mUserData.NickName = nickName;
            PlayerPrefs.SetString(UserData.KEY_NICKNAME, nickName);
            PlayerPrefs.Save();
        }
		#region 서버 동기화 (saveData/loadData용)

		#region 서버 저장 예약

		//재화·아이템 변동은 잦아서 변할 때마다 서버에 쓰면 Functions 호출 비용이 커진다.
		//변경을 표시해두고 일정 시간 뒤 1회만 올리며, 앱 일시정지/종료처럼 이탈하는 시점엔 즉시 올린다.
		private const float SERVER_SAVE_DEBOUNCE_SECONDS = 30F;
		private bool mbServerSaveDirty = false;
		private float mServerSaveTimer = 0F;
		private bool mbServerSaveInFlight = false;

		/// <summary>
		/// 서버에 올려야 할 변경이 생겼음을 표시한다. 실제 전송은 디바운스 후 1회만 일어난다.
		/// </summary>
		public void MarkServerSaveDirty()
		{
			mbServerSaveDirty = true;
			mServerSaveTimer = SERVER_SAVE_DEBOUNCE_SECONDS;
		}

		/// <summary>
		/// 예약된 변경을 기다리지 않고 즉시 서버에 올린다.
		/// (스테이지 클리어·구매 완료·앱 이탈처럼 반드시 남겨야 하는 시점용)
		/// </summary>
		public void FlushServerSaveNow()
		{
			mbServerSaveDirty = false;
			mServerSaveTimer = 0F;

			SendServerSave();
		}

		/// <summary>
		/// 예약된 변경이 있을 때만 즉시 올린다. 변경이 없으면 아무것도 하지 않는다.
		/// 앱 일시정지처럼 자주 일어나는 시점에서 불필요한 서버 호출을 막기 위한 경로.
		/// </summary>
		public void FlushServerSaveIfPending()
		{
			if(!mbServerSaveDirty)
			{
				return;
			}

			FlushServerSaveNow();
		}

		private void Update()
		{
			if(!mbServerSaveDirty)
			{
				return;
			}

			//타임스케일 0(팝업 등)에서도 흐르도록 unscaled 사용
			mServerSaveTimer -= Time.unscaledDeltaTime;
			if(mServerSaveTimer > 0F)
			{
				return;
			}

			mbServerSaveDirty = false;
			SendServerSave();
		}

		private async void SendServerSave()
		{
			//부팅 시 서버 복원이 확정되지 않은 세션에서는 서버에 쓰지 않는다.
			//(재설치 유저의 서버 데이터를 새 로컬 데이터로 덮어쓰는 사고 방지)
			//이 상태는 세션 중에 바뀌지 않으므로 재시도 예약도 하지 않는다. 로컬 저장은 이미 끝나 있다.
			if(!ServerSyncService.IsRestoreResolved)
			{
				return;
			}

			//이전 전송이 안 끝났으면 겹쳐 보내지 않고 다음 기회로 미룬다.
			if(mbServerSaveInFlight)
			{
				MarkServerSaveDirty();
				return;
			}

			mbServerSaveInFlight = true;
			try
			{
				bool bSuccess = await ServerSyncService.SaveToServer();
				if(!bSuccess)
				{
					//오프라인·로그인 실패 등. 로컬엔 이미 저장돼 있으니 표시만 되살려 다음 기회에 재시도한다.
					MarkServerSaveDirty();
				}
			}
			catch(Exception e)
			{
				Debug.LogError($"[PlayerDataManager] 서버 저장 실패: {e}");
				MarkServerSaveDirty();
			}
			finally
			{
				mbServerSaveInFlight = false;
			}
		}

		#endregion

		/// <summary>
		/// 서버 저장용 유저 데이터(5필드)를 서버 스키마 형태의 딕셔너리로 만든다.
		/// { removeAds, currentStage, gold, itemCounts{id:count}, championsLevel, isChampionsActive }
		/// </summary>
		public Dictionary<string, object> BuildServerUserData()
		{
			Dictionary<string, object> itemCounts = new Dictionary<string, object>();
			if(mUserData != null && mUserData.ItemCounts != null)
			{
				foreach(KeyValuePair<int, int> pair in mUserData.ItemCounts)
				{
					itemCounts[pair.Key.ToString()] = pair.Value;
				}
			}

			return new Dictionary<string, object>
			{
				{ "removeAds", mUserData != null && mUserData.RemoveAds },
				{ "currentStage", mUserData != null ? mUserData.CurrentStage : 1 },
				{ "gold", mUserData != null ? mUserData.Gold : 0 },
				{ "itemCounts", itemCounts },
				{ "championsLevel", mUserData != null ? mUserData.ChampionsLevel : 0 },
				{ "isChampionsActive", mUserData != null && mUserData.IsChampionsActive }
			};
		}

		/// <summary>
		/// 서버에서 불러온 유저 데이터를 로컬 UserData에 반영하고 저장한다.
		/// </summary>
		public void ApplyServerUserData(Dictionary<object, object> data)
		{
			if(mUserData == null || data == null)
			{
				return;
			}

			if(data.TryGetValue("removeAds", out object removeAds))
			{
				mUserData.RemoveAds = Convert.ToBoolean(removeAds);
			}
			if(data.TryGetValue("currentStage", out object currentStage))
			{
				mUserData.CurrentStage = Convert.ToInt32(currentStage);
			}
			if(data.TryGetValue("gold", out object gold))
			{
				mUserData.Gold = Convert.ToInt32(gold);
			}
			if(data.TryGetValue("championsLevel", out object championsLevel))
			{
				mUserData.ChampionsLevel = Convert.ToInt32(championsLevel);
			}
			if(data.TryGetValue("isChampionsActive", out object isChampionsActive))
			{
				mUserData.IsChampionsActive = Convert.ToBoolean(isChampionsActive);
			}
			if(data.TryGetValue("itemCounts", out object itemCountsObj) && itemCountsObj is Dictionary<object, object> itemCounts)
			{
				foreach(KeyValuePair<object, object> pair in itemCounts)
				{
					if(int.TryParse(pair.Key.ToString(), out int itemId))
					{
						mUserData.ItemCounts[itemId] = Convert.ToInt32(pair.Value);
					}
				}
			}

			SaveUserData();
		}

		#endregion

		private void LoadUserData()
		{
			string encryptedText = PlayerPrefs.GetString("UserData", "");
			if(!string.IsNullOrEmpty(encryptedText) && DataEncryptor.TryDecrypt(encryptedText, out string json))
			{
				EncryptedUserData data = JsonUtility.FromJson<EncryptedUserData>(json);
				mUserData = new UserData();
				mUserData.FromEncryptedUserData(data);
			}
			else
			{
				mUserData = new UserData();
			}
			mUserData.LoadUnEncryptedData();

			RefreshDailyReset();
		}
		private void SaveUserData()
		{
			if(mUserData == null)
			{
				return;
			}
			EncryptedUserData userData = mUserData.ToEncryptedUserData();
			string json = JsonUtility.ToJson(userData);

			string encryptedText = DataEncryptor.Encrypt(json);
			if(!string.IsNullOrEmpty(encryptedText))
			{
				PlayerPrefs.SetString("UserData", encryptedText);
				PlayerPrefs.Save();
			}
		}
		public void LoadUserDataForDebug()
		{
			bool debugMode = Debug.isDebugBuild;
#if UNITY_EDITOR
			debugMode = true;
#endif
			if(!debugMode)
			{
				return;
			}

			LoadUserData();
		}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
		//런타임에서 GameTime 오프셋을 바꾼 뒤, 앱 재시작 없이 일일 리셋을 재평가하기 위한 디버그 훅.
		//오프셋으로 GameTime.Today가 바뀌면 RefreshDailyReset이 하루 경계를 감지한다.
		public void DebugRecheckDailyReset()
		{
			if(mUserData == null)
			{
				return;
			}

			RefreshDailyReset();

			EventManager.Inst.ActiveEvent(EventKeys.CONTENT_DATA_REFRESH);

			Debug.Log($"[Debug] 일일 리셋 재검사 → streak={mUserData.StreakLoginCount}, dailyChecked={mUserData.IsDailyCheckToday}, roulette={mUserData.RouletteCount}");
		}
#endif
	}
}
