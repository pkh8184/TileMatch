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
        [SerializeField] private int mGold = 99999;
        [SerializeField] private int mGem = 0;

        [Header("스테이지")]
        [SerializeField] private int mCurrentStage = 1;      // 현재 진행 중인 스테이지
        [SerializeField] private int mMaxClearedStage = 0;   // 클리어한 최대 스테이지
        [SerializeField] private int mSelectedStage = 1;     // 선택한 스테이지 (게임 시작 시)

        [Header("아이템 보유량")]
        [SerializeField] private int mStrikeCount = 3;
        [SerializeField] private int mBlackHoleCount = 3;
        [SerializeField] private int mBoomCount = 3;

        [Header("프로필")]
        [SerializeField] private string mPlayerName = "Player";
        [SerializeField] private int mProfileIconId = 0;

        // 이벤트
        public event Action<int> OnGoldChanged;
        public event Action<int> OnGemChanged;
        public event Action<int> OnStageChanged;

        // 프로퍼티
        public int Gold => mGold;
        public int Gem => mGem;
        public int CurrentStage => mCurrentStage;
        public int MaxClearedStage => mMaxClearedStage;
        public int SelectedStage => mSelectedStage;
        public string PlayerName => mPlayerName;
        public int ProfileIconId => mProfileIconId;

        // 아이템 프로퍼티
        public int StrikeCount => mStrikeCount;
        public int BlackHoleCount => mBlackHoleCount;
        public int BoomCount => mBoomCount;

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
            mGold += amount;
            OnGoldChanged?.Invoke(mGold);
            SaveData();
        }

        /// <summary>
        /// 골드 사용
        /// </summary>
        public bool UseGold(int amount)
        {
            if (mGold < amount) return false;

            mGold -= amount;
            OnGoldChanged?.Invoke(mGold);
            SaveData();
            return true;
        }

        /// <summary>
        /// 골드 충분한지 체크
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return mGold >= amount;
        }

        /// <summary>
        /// 보석 추가
        /// </summary>
        public void AddGem(int amount)
        {
            mGem += amount;
            OnGemChanged?.Invoke(mGem);
            SaveData();
        }

        /// <summary>
        /// 보석 사용
        /// </summary>
        public bool UseGem(int amount)
        {
            if (mGem < amount) return false;

            mGem -= amount;
            OnGemChanged?.Invoke(mGem);
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
            if (stageLevel > mMaxClearedStage)
            {
                mMaxClearedStage = stageLevel;
                mCurrentStage = stageLevel + 1;
                OnStageChanged?.Invoke(mCurrentStage);
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
            mSelectedStage = stageLevel;
        }

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(int stageLevel)
        {
            return stageLevel <= mMaxClearedStage;
        }

        /// <summary>
        /// 스테이지 플레이 가능 여부
        /// </summary>
        public bool CanPlayStage(int stageLevel)
        {
            return stageLevel <= mMaxClearedStage + 1;
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
        public void AddItem(EItemType itemType, int amount)
        {
            switch (itemType)
            {
                case EItemType.Strike:
                    mStrikeCount += amount;
                    break;
                case EItemType.BlackHole:
                    mBlackHoleCount += amount;
                    break;
                case EItemType.Boom:
                    mBoomCount += amount;
                    break;
            }
            SaveData();
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(EItemType itemType)
        {
            switch (itemType)
            {
                case EItemType.Strike:
                    if (mStrikeCount <= 0) return false;
                    mStrikeCount--;
                    break;
                case EItemType.BlackHole:
                    if (mBlackHoleCount <= 0) return false;
                    mBlackHoleCount--;
                    break;
                case EItemType.Boom:
                    if (mBoomCount <= 0) return false;
                    mBoomCount--;
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
        public int GetItemCount(EItemType itemType)
        {
            switch (itemType)
            {
                case EItemType.Strike: return mStrikeCount;
                case EItemType.BlackHole: return mBlackHoleCount;
                case EItemType.Boom: return mBoomCount;
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
            mPlayerName = name;
            SaveData();
        }

        /// <summary>
        /// 프로필 아이콘 설정
        /// </summary>
        public void SetProfileIcon(int iconId)
        {
            mProfileIconId = iconId;
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
                gold = this.mGold,
                gem = this.mGem,
                currentStage = this.mCurrentStage,
                maxClearedStage = this.mMaxClearedStage,
                strikeCount = this.mStrikeCount,
                blackHoleCount = this.mBlackHoleCount,
                boomCount = this.mBoomCount,
                playerName = this.mPlayerName,
                profileIconId = this.mProfileIconId
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

                mGold = saveData.gold;
                mGem = saveData.gem;
                mCurrentStage = saveData.currentStage;
                mMaxClearedStage = saveData.maxClearedStage;
                mStrikeCount = saveData.strikeCount;
                mBlackHoleCount = saveData.blackHoleCount;
                mBoomCount = saveData.boomCount;
                mPlayerName = saveData.playerName;
                mProfileIconId = saveData.profileIconId;

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
            mGold = 99999;
            mGem = 0;
            mCurrentStage = 1;
            mMaxClearedStage = 0;
            mStrikeCount = 3;
            mBlackHoleCount = 3;
            mBoomCount = 3;
            mPlayerName = "Player";
            mProfileIconId = 0;

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
