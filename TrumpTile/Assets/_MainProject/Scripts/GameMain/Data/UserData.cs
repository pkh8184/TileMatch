using UnityEngine;
using System;
using System.Collections.Generic;

namespace TrumpTile.GameMain.Data
{
    // 딕셔너리 직렬화를 위한 래퍼
    [Serializable]
    public class ItemDictionaryEntry
    {
        public int itemId;
        public int count;
    }
    [Serializable]
    public class ItemDictionaryWrapper
    {
        public List<ItemDictionaryEntry> itemDictionaryList = new List<ItemDictionaryEntry>();
    }
    [Serializable]
    public class EncryptedUserData
    {
        //광고 제거
        public bool RemoveAds;

        //스테이지 관련
        public int CurrentStage;
        public int FirstTryClearCount;
        public int MaxStreakClearStageCount;

        //재화 데이터
        public int Gold;
        public int Star;
        public ItemDictionaryWrapper ItemDictionaryWrapper;

        // 앨범 수집 데이터
        public int LastAlbumRewardedStage;

        //로그인 데이터
        public long FirstLoginDate;
        public long CurrentLoginDate;
        public long LogoutDate;
        public int MaxStreakLoginCount;

        //컨텐츠 해금 데이터
        public bool SeasonPassUnlock;
        public bool PiggyBankUnlock;
        public bool DailyCheckUnlock;
        public bool RouletteUnlock;
        public bool ExcitTravelUnlock;
        public bool GemCollectionUnlock;

        //출석체크 관련 데이터
        public int StreakLoginCount;
        public bool IsFirstLoginToday;

        //룰렛 관련 데이터
        public int RouletteCount;
        
        //기차 여행 관련 데이터
        public int ExcitTravelIndex;
        public bool IsExcitTravelActive;
        public long ExcitTravelActiveDate;
        public long ExcitTravelUnActiveDate;
        
        //돼지저금통 관련 데이터
        public bool PiggyBankPurchase;
        public int PiggyBankStageClearCount;
        public bool IsPiggyBankActive;
        public long PiggyBankActiveDate;
        public long PiggyBankUnActiveDate;
        
        //보석 수집 관련 데이터
        public bool IsGemCollectionActive;
        public long GemCollectionActiveDate;
        public long GemCollectionUnActiveDate;
        public int GemCollectionIndex;
        public int GemCount;

        public EncryptedUserData(UserData data)
        {
            RemoveAds = data.RemoveAds;

            //스테이지 관련
            CurrentStage = data.CurrentStage;
            FirstTryClearCount = data.FirstTryClearCount;
            MaxStreakClearStageCount = data.MaxStreakClearStageCount;

            //재화 데이터
            Gold = data.Gold;
            Star = data.Star;

            //아이템 딕셔너리 -> 리스트 래퍼 변환
            ItemDictionaryWrapper = new ItemDictionaryWrapper();
            if (data.ItemCounts != null)
            {
                foreach (KeyValuePair<int, int> pair in data.ItemCounts)
                {
                    ItemDictionaryEntry entry = new ItemDictionaryEntry();
                    entry.itemId = pair.Key;
                    entry.count = pair.Value;
                    ItemDictionaryWrapper.itemDictionaryList.Add(entry);
                }
            }

            //앨범 수집 데이터
            LastAlbumRewardedStage = data.LastAlbumRewardedStage;

            //로그인 데이터
            FirstLoginDate = data.FirstLoginDate.ToUniversalTime().Ticks;
            CurrentLoginDate = data.CurrentLoginDate.ToUniversalTime().Ticks;
            LogoutDate = DateTime.Now.ToUniversalTime().Ticks;
            MaxStreakLoginCount = data.MaxStreakLoginCount;

            //컨텐츠 해금 데이터
            SeasonPassUnlock = data.SeasonPassUnlock;
            PiggyBankUnlock = data.PiggyBankUnlock;
            DailyCheckUnlock = data.DailyCheckUnlock;
            RouletteUnlock = data.RouletteUnlock;
            ExcitTravelUnlock = data.ExcitTravelUnlock;
            GemCollectionUnlock = data.GemCollectionUnlock;

            //출석체크 관련 데이터
            StreakLoginCount = data.StreakLoginCount;
            IsFirstLoginToday = data.IsFirstLoginToday;

            //룰렛 관련 데이터
            RouletteCount = data.RouletteCount;

            //기차 여행 관련 데이터
            ExcitTravelIndex = data.ExcitTravelIndex;
            IsExcitTravelActive = data.IsExcitTravelActive;
            ExcitTravelActiveDate = data.ExcitTravelActiveDate.ToUniversalTime().Ticks;
            ExcitTravelUnActiveDate = data.ExcitTravelUnActiveDate.ToUniversalTime().Ticks;

            //돼지저금통 관련 데이터
            PiggyBankPurchase = data.PiggyBankPurchase;
            PiggyBankStageClearCount = data.PiggyBankStageClearCount;
            IsPiggyBankActive = data.IsPiggyBankActive;
            PiggyBankActiveDate = data.PiggyBankActiveDate.ToUniversalTime().Ticks;
            PiggyBankUnActiveDate = data.PiggyBankUnActiveDate.ToUniversalTime().Ticks;

            //보석 수집 관련 데이터
            IsGemCollectionActive = data.IsGemCollectionActive;
            GemCollectionActiveDate = data.GemCollectionActiveDate.ToUniversalTime().Ticks;
            GemCollectionUnActiveDate = data.GemCollectionUnActiveDate.ToUniversalTime().Ticks;
            GemCollectionIndex = data.GemCollectionIndex;
            GemCount = data.GemCount;
        }
    }
    [Serializable]
    public class UserData
    {
        //로컬 데이터
        public string NickName;
        public int ProfileImageIndex;
        public int ProfileFrameIndex;
        public bool BGMOn;
        public bool SFXOn;
        public bool HapticOn;
        public int LocaleIndex;

        //광고 제거 여부
        public bool RemoveAds;

        //스테이지 관련 데이터
        public int CurrentStage;
        public int FirstTryClearCount;
        public int MaxStreakClearStageCount;

        //재화 데이터
        public int Gold;
        public int Star;

        //아이템 데이터 (key: ItemId, value: 보유 개수)
        public Dictionary<int, int> ItemCounts;

        // 앨범 수집 데이터
        public int LastAlbumRewardedStage;

        //로그인 데이터
        public DateTime FirstLoginDate;
        public DateTime CurrentLoginDate;
        public DateTime LogoutDate;
        public int MaxStreakLoginCount;

        //컨텐츠 해금 데이터
        public bool SeasonPassUnlock;
        public bool PiggyBankUnlock;
        public bool DailyCheckUnlock;
        public bool RouletteUnlock;
        public bool ExcitTravelUnlock;
        public bool GemCollectionUnlock;

        //출석체크 관련 데이터
        public int StreakLoginCount;
        public bool IsFirstLoginToday;

        //룰렛 관련 데이터
        public int RouletteCount;

        //기차 여행 관련 데이터
        public int ExcitTravelIndex;
        public bool IsExcitTravelActive;
        public DateTime ExcitTravelActiveDate;
        public DateTime ExcitTravelUnActiveDate;
        
        //돼지저금통 관련 데이터
        public bool PiggyBankPurchase;
        public int PiggyBankStageClearCount;
        public bool IsPiggyBankActive;
        public DateTime PiggyBankActiveDate;
        public DateTime PiggyBankUnActiveDate;
        
        //보석 수집 관련 데이터
        public bool IsGemCollectionActive;
        public DateTime GemCollectionActiveDate;
        public DateTime GemCollectionUnActiveDate;
        public int GemCollectionIndex;
        public int GemCount;

        //딕셔너리 파싱 생성자
        public UserData(Dictionary<object, object> dataDictionary)
        {
            RemoveAds = dataDictionary["removeAdsPurchaseDate"] != null;

            Dictionary<object, object> stageData = dataDictionary["stageData"] as Dictionary<object, object>;
            CurrentStage = (int)Convert.ToInt64(stageData["currentStage"]);
            FirstTryClearCount = (int)Convert.ToInt64(stageData["firstTryCount"]);
            MaxStreakClearStageCount = (int)Convert.ToInt64(stageData["maxStreakStageCount"]);

            Dictionary<object, object> currencyData = dataDictionary["currency"] as Dictionary<object, object>;
            Gold = (int)Convert.ToInt64(currencyData["gold"]);
            Star = (int)Convert.ToInt64(currencyData["star"]);

            Dictionary<object, object> itemData = dataDictionary["item"] as Dictionary<object, object>;
            ItemCounts = new Dictionary<int, int>();
            foreach (KeyValuePair<object, object> pair in itemData)
            {
                if (int.TryParse(pair.Key.ToString(), out int itemId))
                {
                    ItemCounts[itemId] = (int)Convert.ToInt64(pair.Value);
                }
            }

            Dictionary<object, object> housingData = dataDictionary["housingData"] as Dictionary<object, object>;

            Dictionary<object, object> loginData = dataDictionary["loginData"] as Dictionary<object, object>;
            Dictionary<object, object> timestampData = loginData["firstLoginDate"] as Dictionary<object, object>;
            long seconds = Convert.ToInt64(timestampData["_seconds"]);
            FirstLoginDate = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
            MaxStreakLoginCount = (int)Convert.ToInt64(loginData["maxStreakLoginCount"]);

            // 앨범 수집 데이터 파싱
            LastAlbumRewardedStage = 0;

            if (dataDictionary.ContainsKey("albumData"))
            {
                Dictionary<object, object> albumData = dataDictionary["albumData"] as Dictionary<object, object>;
                if (albumData != null && albumData.ContainsKey("lastAlbumRewardedStage"))
                {
                    LastAlbumRewardedStage = (int)Convert.ToInt64(albumData["lastAlbumRewardedStage"]);
                }
            }

            LoadUnEncryptedData();
        }
        public UserData()
        {
            InitData();
        }
        public void InitData()
        {
            LoadUnEncryptedData();

            RemoveAds = false;
            CurrentStage = 1;
            FirstTryClearCount = 0;
            MaxStreakClearStageCount = 0;

            ItemCounts = new Dictionary<int, int>();
            ItemCounts[1005] = 0;
            ItemCounts[1006] = 0;
            ItemCounts[1007] = 0;
            ItemCounts[1008] = 0;
            
            Gold = 0;
            Star = 0;
            LastAlbumRewardedStage = 0;
            //로그인 데이터
            FirstLoginDate = DateTime.Now;
            CurrentLoginDate = DateTime.Now;
            LogoutDate = DateTime.MinValue;
            
            MaxStreakLoginCount = 0;
            StreakLoginCount = 0;
            IsFirstLoginToday = true;
            DailyCheckUnlock = false;

            RouletteCount = 0;
            RouletteUnlock = false;

            IsExcitTravelActive = true;
            ExcitTravelIndex = 0;
            ExcitTravelUnlock = false;

            PiggyBankPurchase = false;
            PiggyBankStageClearCount = 0;
            IsPiggyBankActive = true;
            PiggyBankUnlock = false;

            IsGemCollectionActive = true;
            GemCollectionUnlock = false;
            GemCollectionIndex = 0;
            GemCount = 0;
        }
        public void SetUserDataOnEndStage(Dictionary<object, object> dataDictionary)
        {

        }
        public void SetUserDataOnPurchaseProduct(Dictionary<object, object> dataDictionary)
        {

        }
        /// <summary>
        /// 로컬에 저장한 데이터 읽어오기
        /// </summary>
        private void LoadUnEncryptedData()
        {
            NickName = PlayerPrefs.GetString("NickName", "USER");
            ProfileImageIndex = PlayerPrefs.GetInt("ProfileImageIndex", 0);
            ProfileFrameIndex = PlayerPrefs.GetInt("ProfileFrameIndex", 0);
            BGMOn = PlayerPrefs.GetFloat("BGMVolume", 0.5f) > 0;
            SFXOn = PlayerPrefs.GetFloat("SFXVolume", 1f) > 0;
            HapticOn = PlayerPrefs.GetInt("Haptic", 1) == 1;
            LocaleIndex = PlayerPrefs.GetInt("LocaleIndex", 0);
        }
        public EncryptedUserData ToEncryptedUserData()
        {
            EncryptedUserData encryptedUserData = new EncryptedUserData(this);
            return encryptedUserData;
        }
        public void FromEncryptedUserData(EncryptedUserData data)
        {
            RemoveAds = data.RemoveAds;

            //스테이지 관련
            CurrentStage = data.CurrentStage;
            if(CurrentStage <= 0)
            {
                CurrentStage = 1;
            }

            FirstTryClearCount = data.FirstTryClearCount;
            MaxStreakClearStageCount = data.MaxStreakClearStageCount;

            //재화 데이터
            Gold = data.Gold;
            Star = data.Star;

            ItemCounts = new Dictionary<int, int>();
            if (data.ItemDictionaryWrapper != null)
            {
                foreach (ItemDictionaryEntry item in data.ItemDictionaryWrapper.itemDictionaryList)
                {
                    ItemCounts[item.itemId] = item.count;
                }
            }

            //앨범 수집 데이터
            LastAlbumRewardedStage = data.LastAlbumRewardedStage;

            //로그인 데이터
            FirstLoginDate = new DateTime(data.FirstLoginDate, DateTimeKind.Local);
            CurrentLoginDate = DateTime.Now;
            LogoutDate = new DateTime(data.LogoutDate, DateTimeKind.Local);
            MaxStreakLoginCount = data.MaxStreakLoginCount;

            //컨텐츠 해금 데이터    
            SeasonPassUnlock = data.SeasonPassUnlock;
            PiggyBankUnlock = data.PiggyBankUnlock;
            DailyCheckUnlock = data.DailyCheckUnlock;
            RouletteUnlock = data.RouletteUnlock;
            ExcitTravelUnlock = data.ExcitTravelUnlock;
            GemCollectionUnlock = data.GemCollectionUnlock;

            //출석체크 관련 데이터
            StreakLoginCount = data.StreakLoginCount;
            IsFirstLoginToday = data.IsFirstLoginToday;

            //룰렛 관련 데이터
            RouletteCount = data.RouletteCount;

            //기차 여행 관련 데이터
            ExcitTravelIndex = data.ExcitTravelIndex;
            IsExcitTravelActive = data.IsExcitTravelActive;
            ExcitTravelActiveDate = new DateTime(data.ExcitTravelActiveDate, DateTimeKind.Local);
            ExcitTravelUnActiveDate = new DateTime(data.ExcitTravelUnActiveDate, DateTimeKind.Local);

            //돼지저금통 관련 데이터
            PiggyBankPurchase = data.PiggyBankPurchase;
            PiggyBankStageClearCount = data.PiggyBankStageClearCount;
            IsPiggyBankActive = data.IsPiggyBankActive;
            PiggyBankActiveDate = new DateTime(data.PiggyBankActiveDate, DateTimeKind.Local);
            PiggyBankUnActiveDate = new DateTime(data.PiggyBankUnActiveDate, DateTimeKind.Local);

            //보석 수집 관련 데이터
            IsGemCollectionActive = data.IsGemCollectionActive;
            GemCollectionActiveDate = new DateTime(data.GemCollectionActiveDate, DateTimeKind.Local);
            GemCollectionUnActiveDate = new DateTime(data.GemCollectionUnActiveDate, DateTimeKind.Local);
            GemCollectionIndex = data.GemCollectionIndex;
            GemCount = data.GemCount;

            LoadUnEncryptedData();
        }
    }
}
