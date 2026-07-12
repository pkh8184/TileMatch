using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrumpTile.GameMain.Core;
using TrumpTile.LevelEditor.Editor;
using TrumpTile.FrameLibrary;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    public enum ELevelTheme
    {
        Normal,
        Hard,
        VeryHard,
        Bonus,
        Water,
        Ice,
        Dark,
        Champions
    }
    [System.Serializable]
    public class LevelDifficultyResourceEntry
    {
        public ELevelTheme LevelTheme;
        public EAudioKey BGMKey;
        public EAudioKey TileMoveSFXKey;
        public EAudioKey MatchSFXKey;
        public Sprite BackgroundSprite;
        public Sprite TileBackgroundSprite;
        public GameObject MatchEffectPrefab;
    }
    [System.Serializable]
	[CreateAssetMenu(fileName = "LevelDifficultyResourceDatabase", menuName = "TileMatch/LevelDifficultyResourceDatabase")]
    public class LevelDifficultyResourceDatabase : ScriptableObject
    {
        [SerializeField] private LevelDifficultyResourceEntry[] mEntries;

        private Dictionary<DayOfWeek, LevelDifficultyResourceEntry> mEntriesByDayOfWeek;
        private Dictionary<string, LevelDifficultyResourceEntry> mEntriesByDifficultyString;

        private string mDifficultyKey;
        public void Initialize(LevelData level)
        {
            mEntriesByDayOfWeek = new Dictionary<DayOfWeek, LevelDifficultyResourceEntry>();

            for(int i = 0; i < 7; i++)
            {
                mEntriesByDayOfWeek[(DayOfWeek)i] = mEntries[i];
            }
            
            mEntriesByDifficultyString = new Dictionary<string, LevelDifficultyResourceEntry>();

            mEntriesByDifficultyString["Normal"] = mEntries.FirstOrDefault(x => x.LevelTheme == ELevelTheme.Normal);
            mEntriesByDifficultyString["Hard"] = mEntries.FirstOrDefault(x => x.LevelTheme == ELevelTheme.Hard);
            mEntriesByDifficultyString["VeryHard"] = mEntries.FirstOrDefault(x => x.LevelTheme == ELevelTheme.VeryHard);
            mEntriesByDifficultyString["Bonus"] = mEntries.FirstOrDefault(x => x.LevelTheme == ELevelTheme.Bonus);
            mEntriesByDifficultyString["Champions"] = mEntries.FirstOrDefault(x => x.LevelTheme == ELevelTheme.Champions);

            if(level.ChampionsLevel)
            {
                mDifficultyKey = "Champions";
                return;
            }

            string diff = level.difficulty.ToString();
            if(diff.Contains("VeryHard"))
            {
                mDifficultyKey = "VeryHard";
            }
            else if(diff.Contains("Hard"))
            {
                mDifficultyKey = "Hard";
            }
            else if(diff.Contains("Bonus"))
            {
                mDifficultyKey = "Bonus";
            }
            else
            {
                mDifficultyKey = "Normal";
            }
        }
        /// <summary>현재 레벨의 테마를 반환한다. (일일 퍼즐이면 요일별 테마, 아니면 난이도 테마)</summary>
        public ELevelTheme GetCurrentTheme(bool isDaily)
        {
            if (isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].LevelTheme;
            }
            if (mEntriesByDifficultyString != null
                && mEntriesByDifficultyString.TryGetValue(mDifficultyKey, out LevelDifficultyResourceEntry entry)
                && entry != null)
            {
                return entry.LevelTheme;
            }
            return ELevelTheme.Normal;
        }
        public Sprite GetBackgroundSprite(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].BackgroundSprite;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return null;
            }
            return mEntriesByDifficultyString[mDifficultyKey].BackgroundSprite;
        }
        public Sprite GetTileBackgroundSprite(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].TileBackgroundSprite;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return null;
            }
            return mEntriesByDifficultyString[mDifficultyKey].TileBackgroundSprite;
        }
        public EAudioKey GetBGMKey(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].BGMKey;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return EAudioKey.BGM_Ingame;
            }
            return mEntriesByDifficultyString[mDifficultyKey].BGMKey;
        }
        public EAudioKey GetTileMoveSFXKey(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].TileMoveSFXKey;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return EAudioKey.SFX_TileMove;
            }
            return mEntriesByDifficultyString[mDifficultyKey].TileMoveSFXKey;
        }
        public EAudioKey GetMatchSFXKey(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].MatchSFXKey;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return EAudioKey.SFX_TileMatch;
            }
            return mEntriesByDifficultyString[mDifficultyKey].MatchSFXKey;
        }
        public GameObject GetMatchEffectPrefab(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek].MatchEffectPrefab;
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return null;
            }
            return mEntriesByDifficultyString[mDifficultyKey].MatchEffectPrefab;
        }
        public LevelDifficultyResourceEntry GetResourceEntry(bool isDaily)
        {
            if(isDaily)
            {
                return mEntriesByDayOfWeek[GameTime.DayOfWeek];
            }
            if(!mEntriesByDifficultyString.ContainsKey(mDifficultyKey))
            {
                Debug.Log($"[LevelDifficultyResourceDatabase] {mDifficultyKey} 키가 존재하지 않습니다.");
                return null;
            }
            return mEntriesByDifficultyString[mDifficultyKey];
        }
    }   
}
