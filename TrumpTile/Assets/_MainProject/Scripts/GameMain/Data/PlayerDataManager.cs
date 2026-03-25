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
        FirstLoginDate
    }

    public class PlayerDataManager : Singleton_GameObject<PlayerDataManager>
    {
        /// <summary>
        /// 플레이어 데이터와 관련된 이벤트들
        /// </summary>

        private List<Sprite> mProfileImageSpriteList = new List<Sprite>();
        private List<Sprite> mProfileFrameSpriteList = new List<Sprite>();

        private UserData mUserData;
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
        public void SetProfileImageIndex(int index)
        {
            mUserData.ProfileImageIndex = index;
        }
        public void SetProfileFrameIndex(int index)
        {
            mUserData.ProfileFrameIndex = index;
        }
        public Sprite GetProfileImage()
        {
            return mProfileImageSpriteList[mUserData.ProfileImageIndex];
        }
        public Sprite GetProfileFrame()
        {
            return mProfileFrameSpriteList[mUserData.ProfileFrameIndex];
        }
        public string GetDataToString(EPlayerDataType ePlayerDataType)
        {
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
                default:
                    break;
            }
            return data;
        }
    }
}
