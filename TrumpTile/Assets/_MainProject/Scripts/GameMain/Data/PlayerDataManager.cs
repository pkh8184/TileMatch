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
        }
        private void OnApplicationPause(bool pause)
        {
            if(pause)
			{
				SaveUserData();
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
		}
		public void ClearChampionsStage()
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ChampionsLevel++;
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
		}
		public void AddItemCount(int itemId, int count)
		{
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return;
			}
			mUserData.ItemCounts[itemId] += count;
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
		}
		public void UnlockSeasonPass()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 시즌패스 해금 완료");
			mUserData.SeasonPassUnlock = true;
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
		}
		public void UnlockRoulette()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 룰렛 해금 완료");
			mUserData.RouletteUnlock = true;	
		}
		public void UnlockExcitTravel()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 기차 여행 해금 완료");
			mUserData.ExcitTravelUnlock = true;	
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
		}
		public void UnlockTreasureBox()
		{
			if(mUserData == null)
			{
				return;
			}
			Debug.Log("[PlayerDataManager] 트레져 박스 해금 완료");
			mUserData.TreasureBoxUnlock = true;
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
		private void RefreshStreakLoginCount()
		{
			if(mUserData == null)
			{
				return;
			}
			int day = (mUserData.CurrentLoginDate.Date - mUserData.LogoutDate.Date).Days;
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
		}
		public void SetDailyCheckDone()
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.IsDailyCheckToday = true;
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
		public void AddGemCount(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCount += value;
			if(mUserData.GemCount < 0) mUserData.GemCount = 0;
		}
		public void SetGemCount(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCount = value;
			if(mUserData.GemCount < 0) mUserData.GemCount = 0;
		}
		public void SetGemIndex(int value)
		{
			if(mUserData == null)
			{
				return;
			}
			mUserData.GemCollectionIndex = value;
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

		public void SetProfileImageIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ProfileImageIndex = index;
			PlayerPrefs.SetInt("ProfileImageIndex", index);
		}

		public void SetProfileFrameIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.ProfileFrameIndex = index;
			PlayerPrefs.SetInt("ProfileFrameIndex", index);
		}

		public void SetBGMOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.BGMOn = isOn;
			PlayerPrefs.SetFloat("BGMVolume", isOn ? 0.5f : 0);
		}

		public void SetSFXOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.SFXOn = isOn;
			PlayerPrefs.SetFloat("SFXVolume", isOn ? 1f : 0);
		}

		public void SetHapticOn(bool isOn)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.HapticOn = isOn;
			PlayerPrefs.SetInt("Haptic", isOn ? 1 : 0);
		}

		public void SetLocaleIndex(int index)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.LocaleIndex = index;
			PlayerPrefs.SetInt("LocaleIndex", index);
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
            PlayerPrefs.SetString("NickName", nickName);
        }
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

			RefreshStreakLoginCount();
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
		//직전 로그인 시점을 로그아웃으로 간주하고 오프셋이 반영된 현재 시각으로 재로그인 처리한다.
		public void DebugRecheckDailyReset()
		{
			if(mUserData == null)
			{
				return;
			}

			mUserData.LogoutDate = mUserData.CurrentLoginDate;
			mUserData.CurrentLoginDate = GameTime.Now;

			RefreshStreakLoginCount();
			SaveUserData();

			EventManager.Inst.ActiveEvent(EventKeys.CONTENT_DATA_REFRESH);

			Debug.Log($"[Debug] 일일 리셋 재검사 → streak={mUserData.StreakLoginCount}, dailyChecked={mUserData.IsDailyCheckToday}, roulette={mUserData.RouletteCount}");
		}
#endif
	}
}
