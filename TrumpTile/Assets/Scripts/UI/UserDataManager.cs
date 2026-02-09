using UnityEngine;
using System;

namespace TrumpTile.Data
{
    /// <summary>
    /// 유저 데이터 관리 (재화, 스테이지 진행, 아이템 보유량 등)
    /// DontDestroyOnLoad로 유지
    /// </summary>
    public class UserDataManager : MonoBehaviour
    {
        public static UserDataManager Instance { get; private set; }

        [Header("재화")]
        [SerializeField] private int gold = 99999;
        [SerializeField] private int gem = 0;

        [Header("스테이지")]
        [SerializeField] private int currentStage = 1;      // 현재 진행 중인 스테이지
        [SerializeField] private int maxClearedStage = 0;   // 클리어한 최대 스테이지
        [SerializeField] private int selectedStage = 1;     // 선택한 스테이지 (게임 시작 시)

        [Header("아이템 보유량")]
        [SerializeField] private int strikeCount = 3;
        [SerializeField] private int blackHoleCount = 3;
        [SerializeField] private int boomCount = 3;

        [Header("프로필")]
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int profileIconId = 0;

        // 이벤트
        public event Action<int> OnGoldChanged;
        public event Action<int> OnGemChanged;
        public event Action<int> OnStageChanged;

        // 프로퍼티
        public int Gold => gold;
        public int Gem => gem;
        public int CurrentStage => currentStage;
        public int MaxClearedStage => maxClearedStage;
        public int SelectedStage => selectedStage;
        public string PlayerName => playerName;
        public int ProfileIconId => profileIconId;

        // 아이템 프로퍼티
        public int StrikeCount => strikeCount;
        public int BlackHoleCount => blackHoleCount;
        public int BoomCount => boomCount;

        private const string SAVE_KEY = "UserData";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region 재화

        /// <summary>
        /// 골드 추가
        /// </summary>
        public void AddGold(int amount)
        {
            gold += amount;
            OnGoldChanged?.Invoke(gold);
            SaveData();
        }

        /// <summary>
        /// 골드 사용
        /// </summary>
        public bool UseGold(int amount)
        {
            if (gold < amount) return false;

            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            SaveData();
            return true;
        }

        /// <summary>
        /// 골드 충분한지 체크
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return gold >= amount;
        }

        /// <summary>
        /// 보석 추가
        /// </summary>
        public void AddGem(int amount)
        {
            gem += amount;
            OnGemChanged?.Invoke(gem);
            SaveData();
        }

        /// <summary>
        /// 보석 사용
        /// </summary>
        public bool UseGem(int amount)
        {
            if (gem < amount) return false;

            gem -= amount;
            OnGemChanged?.Invoke(gem);
            SaveData();
            return true;
        }

        #endregion

        #region 스테이지

        /// <summary>
        /// 스테이지 클리어 처리
        /// </summary>
        public void ClearStage(int stageLevel, int stars)
        {
            if (stageLevel > maxClearedStage)
            {
                maxClearedStage = stageLevel;
                currentStage = stageLevel + 1;
                OnStageChanged?.Invoke(currentStage);
            }

            // 스테이지별 별 저장 (나중에 구현)
            SaveStageStars(stageLevel, stars);
            SaveData();
        }

        /// <summary>
        /// 선택한 스테이지 설정
        /// </summary>
        public void SetSelectedStage(int stageLevel)
        {
            selectedStage = stageLevel;
        }

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(int stageLevel)
        {
            return stageLevel <= maxClearedStage;
        }

        /// <summary>
        /// 스테이지 플레이 가능 여부
        /// </summary>
        public bool CanPlayStage(int stageLevel)
        {
            return stageLevel <= maxClearedStage + 1;
        }

        /// <summary>
        /// 스테이지 별 개수 가져오기
        /// </summary>
        public int GetStageStars(int stageLevel)
        {
            return PlayerPrefs.GetInt($"Stage_{stageLevel}_Stars", 0);
        }

        /// <summary>
        /// 스테이지 별 저장
        /// </summary>
        private void SaveStageStars(int stageLevel, int stars)
        {
            int currentStars = GetStageStars(stageLevel);
            if (stars > currentStars)
            {
                PlayerPrefs.SetInt($"Stage_{stageLevel}_Stars", stars);
            }
        }

        #endregion

        #region 아이템

        /// <summary>
        /// 아이템 추가
        /// </summary>
        public void AddItem(ItemType itemType, int amount)
        {
            switch (itemType)
            {
                case ItemType.Strike:
                    strikeCount += amount;
                    break;
                case ItemType.BlackHole:
                    blackHoleCount += amount;
                    break;
                case ItemType.Boom:
                    boomCount += amount;
                    break;
            }
            SaveData();
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Strike:
                    if (strikeCount <= 0) return false;
                    strikeCount--;
                    break;
                case ItemType.BlackHole:
                    if (blackHoleCount <= 0) return false;
                    blackHoleCount--;
                    break;
                case ItemType.Boom:
                    if (boomCount <= 0) return false;
                    boomCount--;
                    break;
                default:
                    return false;
            }
            SaveData();
            return true;
        }

        /// <summary>
        /// 아이템 개수 가져오기
        /// </summary>
        public int GetItemCount(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Strike: return strikeCount;
                case ItemType.BlackHole: return blackHoleCount;
                case ItemType.Boom: return boomCount;
                default: return 0;
            }
        }

        #endregion

        #region 프로필

        /// <summary>
        /// 플레이어 이름 설정
        /// </summary>
        public void SetPlayerName(string name)
        {
            playerName = name;
            SaveData();
        }

        /// <summary>
        /// 프로필 아이콘 설정
        /// </summary>
        public void SetProfileIcon(int iconId)
        {
            profileIconId = iconId;
            SaveData();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// 데이터 저장
        /// </summary>
        public void SaveData()
        {
            UserSaveData saveData = new UserSaveData
            {
                gold = this.gold,
                gem = this.gem,
                currentStage = this.currentStage,
                maxClearedStage = this.maxClearedStage,
                strikeCount = this.strikeCount,
                blackHoleCount = this.blackHoleCount,
                boomCount = this.boomCount,
                playerName = this.playerName,
                profileIconId = this.profileIconId
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            Debug.Log("[UserDataManager] Data saved");
        }

        /// <summary>
        /// 데이터 로드
        /// </summary>
        public void LoadData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                UserSaveData saveData = JsonUtility.FromJson<UserSaveData>(json);

                gold = saveData.gold;
                gem = saveData.gem;
                currentStage = saveData.currentStage;
                maxClearedStage = saveData.maxClearedStage;
                strikeCount = saveData.strikeCount;
                blackHoleCount = saveData.blackHoleCount;
                boomCount = saveData.boomCount;
                playerName = saveData.playerName;
                profileIconId = saveData.profileIconId;

                Debug.Log("[UserDataManager] Data loaded");
            }
            else
            {
                Debug.Log("[UserDataManager] No save data, using defaults");
            }
        }

        /// <summary>
        /// 데이터 리셋 (디버그용)
        /// </summary>
        public void ResetData()
        {
            gold = 99999;
            gem = 0;
            currentStage = 1;
            maxClearedStage = 0;
            strikeCount = 3;
            blackHoleCount = 3;
            boomCount = 3;
            playerName = "Player";
            profileIconId = 0;

            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();

            Debug.Log("[UserDataManager] Data reset");
        }

        #endregion

        /// <summary>
        /// 저장 데이터 구조
        /// </summary>
        [Serializable]
        private class UserSaveData
        {
            public int gold;
            public int gem;
            public int currentStage;
            public int maxClearedStage;
            public int strikeCount;
            public int blackHoleCount;
            public int boomCount;
            public string playerName;
            public int profileIconId;
        }
    }
}
