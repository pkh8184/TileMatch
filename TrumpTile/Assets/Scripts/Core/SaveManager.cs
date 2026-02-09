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
        public float bgmVolume = 0.5f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;
        
        // 현재 진행
        public int currentLevel = 1;
        public int currentScore = 0;
    }
    
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        
        private const string SAVE_KEY = "TileMatchSaveData";
        
        private GameSaveData saveData;
        
        public GameSaveData Data => saveData;
        
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
                saveData = JsonUtility.FromJson<GameSaveData>(json);
            }
            else
            {
                saveData = new GameSaveData();
            }
        }
        
        public void SaveData()
        {
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }
        
        public void UpdateHighScore(int score)
        {
            if (score > saveData.highScore)
            {
                saveData.highScore = score;
                SaveData();
            }
        }
        
        public void UpdateHighestLevel(int level)
        {
            if (level > saveData.highestLevel)
            {
                saveData.highestLevel = level;
                SaveData();
            }
        }
        
        public void IncrementGamesPlayed()
        {
            saveData.totalGamesPlayed++;
            SaveData();
        }
        
        public void AddMatchesMade(int count)
        {
            saveData.totalMatchesMade += count;
            SaveData();
        }
        
        public void UpdateMaxCombo(int combo)
        {
            if (combo > saveData.maxCombo)
            {
                saveData.maxCombo = combo;
                SaveData();
            }
        }
        
        public void SaveProgress(int level, int score)
        {
            saveData.currentLevel = level;
            saveData.currentScore = score;
            SaveData();
        }
        
        public void SaveSettings(float bgmVolume, float sfxVolume, bool vibration)
        {
            saveData.bgmVolume = bgmVolume;
            saveData.sfxVolume = sfxVolume;
            saveData.vibrationEnabled = vibration;
            SaveData();
        }
        
        public void ResetProgress()
        {
            saveData.currentLevel = 1;
            saveData.currentScore = 0;
            SaveData();
        }
        
        public void ResetAllData()
        {
            saveData = new GameSaveData();
            SaveData();
        }
        
        // 통계 가져오기
        public int GetHighScore() => saveData.highScore;
        public int GetHighestLevel() => saveData.highestLevel;
        public int GetTotalGamesPlayed() => saveData.totalGamesPlayed;
        public int GetTotalMatchesMade() => saveData.totalMatchesMade;
        public int GetMaxCombo() => saveData.maxCombo;
    }
}
