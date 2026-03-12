using UnityEngine;
using System;
using Firebase.Firestore;
using System.Collections.Generic;

namespace TrumpTile.GameMain.Data
{
    /// <summary>
    /// 유저 데이터 중에서 클라이언트 측에서 표시해야 하는 데이터들의 집합입니다.
    /// 값을 임의로 변경시킬 경우 악용할 수 있는 데이터는 암호화합니다.
    /// 로그인 시 서버에 요청하여 데이터를 딕셔너리로 읽어오고, 이후 서버에서 데이터가 수정될 때마다 수정된 값을 반환받아 수정합니다.
    /// </summary>
    [Serializable]
    public class UserData
    {
        //광고 제거 여부
        public ObscuredBool RemoveAds;

        //스테이지 관련 데이터
        public ObscuredInt CurrentStage;
        public ObscuredInt FirstTryClearCount;
        public ObscuredInt MaxStreakClearStageCount;

        //재화 데이터
        public ObscuredInt Gold;
        public ObscuredInt Star;

        //아이템 데이터
        public ObscuredInt Blackhole;
        public ObscuredInt Timer;
        public ObscuredInt Bomb;

        //하우징 데이터
        public ObscuredInt CurrentHousingChapter;
        public ObscuredInt CurrentHousingSubChapter;
        public ObscuredInt CompletedChapterCount;

        //로그인 데이터
        public DateTime FirstLoginDate;
        public ObscuredInt MaxStreakLoginCount;

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
            Blackhole = (int)Convert.ToInt64(itemData["blackhole"]);
            Timer = (int)Convert.ToInt64(itemData["timer"]);
            Bomb = (int)Convert.ToInt64(itemData["bomb"]);

            Dictionary<object, object> housingData = dataDictionary["housingData"] as Dictionary<object, object>;
            CurrentHousingChapter = (int)Convert.ToInt64(housingData["currentChapter"]);
            CurrentHousingSubChapter = (int)Convert.ToInt64(housingData["currentSubChapter"]);
            CompletedChapterCount = (int)Convert.ToInt64(housingData["completedChapterCount"]);

            Dictionary<object, object> loginData = dataDictionary["loginData"] as Dictionary<object, object>;
            Dictionary<object, object> timestampData = loginData["firstLoginDate"] as Dictionary<object, object>;
            long seconds = Convert.ToInt64(timestampData["_seconds"]);
            FirstLoginDate = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
            MaxStreakLoginCount = (int)Convert.ToInt64(loginData["maxStreakLoginCount"]);
        }
        public void SetUserDataOnEndStage(Dictionary<object, object> dataDictionary)
        {

        }
        public void SetUserDataOnPurchaseProduct(Dictionary<object, object> dataDictionary)
        {

        }
    }
}
