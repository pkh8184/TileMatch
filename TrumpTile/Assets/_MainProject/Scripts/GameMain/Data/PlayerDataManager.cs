using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.UI;
using System;
using UnityEngine.AddressableAssets;

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
        //UID(Firebase UID 아니고 커스텀 UID임)
        UID,
        TermsAndConditionVersion
    }

    public class PlayerDataManager : Singleton_GameObject<PlayerDataManager>
    {
        /// <summary>
        /// 플레이어 데이터와 관련된 이벤트들
        /// </summary>

        private List<Sprite> mProfileImageSpriteList = new List<Sprite>();
        private List<Sprite> mProfileFrameSpriteList = new List<Sprite>();

        private UserData mUserData = null;
        public UserData UserData { get => mUserData; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        public void Initialize(Dictionary<object, object> dictionary)
        {
            mUserData = new UserData(dictionary);

            //Addressables.LoadAssetsAsync<Sprite>("ProfileImages", (sprite) =>
            //{
            //    mProfileImageSpriteList.Add(sprite);
            //});

            //Addressables.LoadAssetsAsync<Sprite>("ProfileFrames", (sprite) =>
            //{
            //    mProfileFrameSpriteList.Add(sprite);
            //});
        }
        #region Getters
        public (bool BGMOn,bool SFXOn,bool HapticOn) GetUserSoundSettingDatas()
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
        public Sprite GetProfileImage()
        {
            if (mUserData == null)
            {
                return null;
            }
            return mProfileImageSpriteList[mUserData.ProfileImageIndex];
        }
        public Sprite GetProfileFrame()
        {
            if (mUserData == null)
            {
                return null;
            }
            return mProfileFrameSpriteList[mUserData.ProfileFrameIndex];
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

        #region setters
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
        /// <summary>
        /// 현재 언어 설정 저장
        /// </summary>
        /// <param name="index">0 = ko, 1 = en, 2 = ja, 3 = zh, 4 = vi, 5 = hi, 6 = ar</param>
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
            if(mUserData == null)
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
                    data = mUserData.Bomb.Value.ToString("N0");
                    break;
                case EPlayerDataType.BlackHole:
                    data = mUserData.Blackhole.Value.ToString("N0");
                    break;
                case EPlayerDataType.Timer:
                    data = mUserData.Timer.Value.ToString("N0");
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
    }
}
