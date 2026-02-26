using UnityEngine;
using System;

namespace TileMatch
{
    [Serializable]
    public class GameSaveData
    {
        public int highScore;
        public int highestLevel;
        public int totalGamesPlayed;
        public int totalMatchesMade;
        public int maxCombo;

        // 설정
        public float bgmVolume = 0.5F;
        public float sfxVolume = 1F;
        public bool vibrationEnabled = true;

        // 현재 진행
        public int currentLevel = 1;
        public int currentScore = 0;
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "TileMatchSaveData";

        private GameSaveData mSaveData;

        public GameSaveData Data => mSaveData;

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

        public void LoadData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                mSaveData = JsonUtility.FromJson<GameSaveData>(json);
            }
            else
            {
                mSaveData = new GameSaveData();
            }
        }

        public void SaveData()
        {
            string json = JsonUtility.ToJson(mSaveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public void UpdateHighScore(int score)
        {
            if (score > mSaveData.highScore)
            {
                mSaveData.highScore = score;
                SaveData();
            }
        }

        public void UpdateHighestLevel(int level)
        {
            if (level > mSaveData.highestLevel)
            {
                mSaveData.highestLevel = level;
                SaveData();
            }
        }

        public void IncrementGamesPlayed()
        {
            mSaveData.totalGamesPlayed++;
            SaveData();
        }

        public void AddMatchesMade(int count)
        {
            mSaveData.totalMatchesMade += count;
            SaveData();
        }

        public void UpdateMaxCombo(int combo)
        {
            if (combo > mSaveData.maxCombo)
            {
                mSaveData.maxCombo = combo;
                SaveData();
            }
        }

        public void SaveProgress(int level, int score)
        {
            mSaveData.currentLevel = level;
            mSaveData.currentScore = score;
            SaveData();
        }

        public void SaveSettings(float bgmVolume, float sfxVolume, bool vibration)
        {
            mSaveData.bgmVolume = bgmVolume;
            mSaveData.sfxVolume = sfxVolume;
            mSaveData.vibrationEnabled = vibration;
            SaveData();
        }

        public void ResetProgress()
        {
            mSaveData.currentLevel = 1;
            mSaveData.currentScore = 0;
            SaveData();
        }

        public void ResetAllData()
        {
            mSaveData = new GameSaveData();
            SaveData();
        }

        // 통계 가져오기
        public int GetHighScore() => mSaveData.highScore;
        public int GetHighestLevel() => mSaveData.highestLevel;
        public int GetTotalGamesPlayed() => mSaveData.totalGamesPlayed;
        public int GetTotalMatchesMade() => mSaveData.totalMatchesMade;
        public int GetMaxCombo() => mSaveData.maxCombo;
    }
}
