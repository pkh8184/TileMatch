using System;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;

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

		public event Action OnGoldChanged;
		public event Action<int> OnStageChanged;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		public void Initialize(Dictionary<object, object> dictionary)
		{
			mUserData = new UserData(dictionary);
		}

		public void Initialize()
		{
			if(mUserData != null) return;

			mUserData = new UserData();
			mUserData.InitOnlyLoacalData();
		}
		#region 프로퍼티

		public int Gold => mUserData != null ? mUserData.Gold.Value : 0;
		public int CurrentStage => mUserData != null ? mUserData.CurrentStage.Value : 1;
		public int MaxClearedStage => CurrentStage - 1;
		public int SelectedStage => mSelectedStage;
		public string UID => mUserData?.UID ?? string.Empty;
		public bool IsExtraSlotUnlocked => PlayerPrefs.GetInt("ExtraSlotUnlocked", 0) == 1;
		public List<int> CollectedPictureIds   => mUserData?.CollectedPictureIds ?? new List<int>();
		public bool      HasPendingAlbumReward => mUserData != null && mUserData.HasPendingAlbumReward;

		#endregion

		#region 재화

		public bool HasEnoughGold(int amount) => Gold >= amount;

		public void AddGold(int amount)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.Gold.Value += amount;
			OnGoldChanged?.Invoke();
		}

		public bool UseGold(int amount)
		{
			if (!HasEnoughGold(amount))
			{
				return false;
			}
			mUserData.Gold.Value -= amount;
			OnGoldChanged?.Invoke();
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
				mUserData.CurrentStage.Value = level + 1;
				OnStageChanged?.Invoke(mUserData.CurrentStage.Value);
			}
			SaveStageStars(level, stars);
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

		#region 슬롯 해금

		public bool PurchaseExtraSlot(int goldCost)
		{
			if (IsExtraSlotUnlocked)
			{
				return false;
			}
			if (!UseGold(goldCost))
			{
				return false;
			}
			PlayerPrefs.SetInt("ExtraSlotUnlocked", 1);
			PlayerPrefs.Save();
			return true;
		}

		#endregion

		#region 아이템

		public int GetItemCount(int itemId)
		{
			if (mUserData == null || mUserData.ItemCounts == null)
			{
				return 0;
			}
			ObscuredInt count;
			return mUserData.ItemCounts.TryGetValue(itemId, out count) ? count.Value : 0;
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
			foreach (KeyValuePair<int, ObscuredInt> pair in mUserData.ItemCounts)
			{
				result[pair.Key] = pair.Value.Value;
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
			Debug.Log("[PlayerDataManager] 광고 제거 해금 완료");
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
		}
	#endregion

	#region 앨범 수집

		public bool IsPictureCollected(int pictureId)
		{
			if (mUserData?.CollectedPictureIds == null)
			{
				return false;
			}
			return mUserData.CollectedPictureIds.Contains(pictureId);
		}

		public void AddCollectedPicture(int pictureId)
		{
			if (mUserData == null)
			{
				return;
			}
			if (!mUserData.CollectedPictureIds.Contains(pictureId))
			{
				mUserData.CollectedPictureIds.Add(pictureId);
			}
		}

		public void SetPendingAlbumReward(bool bHasPending)
		{
			if (mUserData == null)
			{
				return;
			}
			mUserData.HasPendingAlbumReward = bHasPending;
		}

		public List<int> GetCollectedPictureIds()
		{
			return mUserData?.CollectedPictureIds ?? new List<int>();
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
					data = mUserData.Gold.Value.ToString("N0");
					break;
				case EPlayerDataType.Star:
					data = mUserData.Star.Value.ToString("N0");
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
				case EPlayerDataType.CurrentHousingChapter:
					data = mUserData.CurrentHousingChapter.ToString();
					break;
				case EPlayerDataType.CurrentHousingSubChapter:
					data = mUserData.CurrentHousingSubChapter.ToString();
					break;
				case EPlayerDataType.CompletedChapterCount:
					data = mUserData.CompletedChapterCount.ToString();
					break;
				case EPlayerDataType.MaxStreakLoginCount:
					data = mUserData.MaxStreakLoginCount.ToString();
					break;
				case EPlayerDataType.FirstLoginDate:
					string date = mUserData.FirstLoginDate.ToString().Substring(0, 10);
					data = "플레이 시작 시점 : " + date;
					break;
				case EPlayerDataType.UID:
					data = mUserData.UID;
					break;
				case EPlayerDataType.TermsAndConditionVersion:
					data = mUserData.TermsAndConditionVersion.ToString();
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
	}
}
