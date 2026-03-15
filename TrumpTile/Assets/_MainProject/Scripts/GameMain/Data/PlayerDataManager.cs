using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.UI;

namespace TrumpTile.GameMain.Data
{
    public class PlayerDataManager : Singleton_GameObject<PlayerDataManager>
    {
        private UserData mUserData;
        public UserData UserData { get => mUserData; set => mUserData = value;}

        public string GetDataToString(EPlayerDataType ePlayerDataType)
        {
            string data = null;
            switch (ePlayerDataType)
            {
                case EPlayerDataType.Gold:
                    data = ((int)mUserData.Gold).ToString("N0");
                    break;
                case EPlayerDataType.Star:
                    data = ((int)mUserData.Star).ToString("NO");
                    break;
                case EPlayerDataType.Bomb:
                    data = ((int)mUserData.Bomb).ToString("NO");
                    break;
                case EPlayerDataType.BlackHole:
                    data = ((int)mUserData.Blackhole).ToString("NO");
                    break;
                case EPlayerDataType.Timer:
                    data = ((int)mUserData.Timer).ToString("N0");
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
                    data = "플레이 시작 시점 : " + mUserData.FirstLoginDate.ToString();
                    break;
                default:
                    break;
            }
            return data;
        }
    }
}
