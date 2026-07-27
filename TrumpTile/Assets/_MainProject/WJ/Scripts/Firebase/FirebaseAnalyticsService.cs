using Firebase.Analytics;
using UnityEngine;

namespace TrumpTile.FirebaseLibrary
{
    /// <summary>
    /// Firebase Analytics 이벤트 이름 모음.
    /// 규칙: 영문 소문자 + 언더스코어, 40자 이내, firebase_/google_/ga_ 접두사 금지.
    /// 앱당 커스텀 이벤트 '이름'은 500개까지만 등록되므로 이름을 동적으로 만들지 않는다.
    /// </summary>
    public static class FirebaseAnalyticsEvents
    {
        //스테이지
        public const string STAGE_START        = "stage_start";
        public const string STAGE_CLEAR        = "stage_clear";
        public const string STAGE_FAIL         = "stage_fail";
        public const string STAGE_REVIVE_AD    = "stage_revive_ad";
        public const string STAGE_REVIVE_GOLD  = "stage_revive_gold";

        //컨텐츠 - 광고 제거
        public const string REMOVE_ADS_OPEN    = "remove_ads_open";
        public const string REMOVE_ADS_CLICK   = "remove_ads_click";
        public const string REMOVE_ADS_FAIL    = "remove_ads_fail";
        public const string REMOVE_ADS_SUCCESS = "remove_ads_success";

        //컨텐츠 - 트레져 박스
        public const string TREASURE_BOX_OPEN    = "treasure_box_open";
        public const string TREASURE_BOX_CLICK   = "treasure_box_click";
        public const string TREASURE_BOX_FAIL    = "treasure_box_fail";
        public const string TREASURE_BOX_SUCCESS = "treasure_box_success";

        //컨텐츠 - 기차 여행
        public const string TRAIN_TRAVEL_OPEN         = "train_travel_open";
        public const string TRAIN_TRAVEL_FREE_REWARD  = "train_travel_free_reward";
        public const string TRAIN_TRAVEL_PAID_CLICK   = "train_travel_paid_click";
        public const string TRAIN_TRAVEL_PAID_FAIL    = "train_travel_paid_fail";
        public const string TRAIN_TRAVEL_PAID_SUCCESS = "train_travel_paid_success";
    }

    /// <summary>
    /// Firebase Analytics 전송 래퍼.
    /// 게임 코드는 이 클래스만 호출하고 Firebase SDK 타입을 직접 다루지 않는다.
    /// (Analytics는 로그인이 필요 없고, 오프라인이면 SDK가 로컬에 쌓았다가 나중에 전송한다)
    /// </summary>
    public static class FirebaseAnalyticsService
    {
        //파라미터 이름
        private const string PARAM_STAGE     = "stage";
        private const string PARAM_STAR      = "star";
        private const string PARAM_PLAY_TIME = "play_time";
        private const string PARAM_REASON    = "reason";
        private const string PARAM_COST      = "cost";
        private const string PARAM_PRODUCT   = "product_id";

        //에디터/디버그(개발) 빌드에서는 실제 전송을 하지 않는다.
        //- 에디터: Analytics는 데스크톱 네이티브 구현이 없어서 호출하면 유니티가 크래시한다.
        //- 디버그 빌드: 개발 중 플레이가 실제 지표에 섞이면 안 된다.
        private static bool bIsSendEnabled
        {
            get
            {
#if UNITY_EDITOR
                return false;
#else
                return !Debug.isDebugBuild;
#endif
            }
        }

        //Firebase 초기화(CheckAndFixDependenciesAsync) 전에 호출하면 예외가 나므로 상태를 확인한다.
        private static bool IsReady => bIsSendEnabled && FirebaseService.IsInitialized;

        /// <summary>수집을 켠다. FirebaseService.Initialize()에서 한 번 호출된다.</summary>
        public static void SetCollectionEnabled(bool enabled)
        {
            if(!IsReady)
            {
                return;
            }
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
        }

        #region 스테이지

        /// <summary>스테이지 도전(시작). 재시도/부활 후 재시작도 각각 1회로 집계된다.</summary>
        public static void LogStageStart(int stage)
        {
            if(IsSkipped(FirebaseAnalyticsEvents.STAGE_START))
            {
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.STAGE_START, new Parameter(PARAM_STAGE, stage));
        }

        /// <summary>스테이지 클리어.</summary>
        public static void LogStageClear(int stage, int star, float playTime)
        {
            if(IsSkipped(FirebaseAnalyticsEvents.STAGE_CLEAR))
            {
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.STAGE_CLEAR,
                new Parameter(PARAM_STAGE, stage),
                new Parameter(PARAM_STAR, star),
                new Parameter(PARAM_PLAY_TIME, playTime));
        }

        /// <summary>스테이지 사망. reason은 "time_out" 또는 "slot_full".</summary>
        public static void LogStageFail(int stage, string reason)
        {
            if(IsSkipped(FirebaseAnalyticsEvents.STAGE_FAIL))
            {
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.STAGE_FAIL,
                new Parameter(PARAM_STAGE, stage),
                new Parameter(PARAM_REASON, reason));
        }

        /// <summary>광고 시청으로 부활.</summary>
        public static void LogStageReviveAd(int stage)
        {
            if(IsSkipped(FirebaseAnalyticsEvents.STAGE_REVIVE_AD))
            {
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.STAGE_REVIVE_AD, new Parameter(PARAM_STAGE, stage));
        }

        /// <summary>골드 소모로 부활.</summary>
        public static void LogStageReviveGold(int stage, int cost)
        {
            if(IsSkipped(FirebaseAnalyticsEvents.STAGE_REVIVE_GOLD))
            {
                return;
            }

            LogEvent(FirebaseAnalyticsEvents.STAGE_REVIVE_GOLD,
                new Parameter(PARAM_STAGE, stage),
                new Parameter(PARAM_COST, cost));
        }

        #endregion

        #region 컨텐츠

        /// <summary>파라미터 없는 단순 카운트 이벤트.</summary>
        public static void LogContentEvent(string eventName)
        {
            if(IsSkipped(eventName))
            {
                return;
            }

            LogEvent(eventName);
        }

        /// <summary>구매 성공/실패처럼 상품 식별이 필요한 이벤트.</summary>
        public static void LogContentEvent(string eventName, string productId)
        {
            if(IsSkipped(eventName))
            {
                return;
            }

            LogEvent(eventName, new Parameter(PARAM_PRODUCT, productId));
        }

        #endregion

        /// <summary>
        /// 전송하지 않아야 하는 상황이면 true.
        /// Parameter는 네이티브(P/Invoke) 래퍼라 생성 자체가 에디터에서 위험하므로,
        /// 반드시 Parameter를 만들기 전에 이 검사를 먼저 통과해야 한다.
        /// </summary>
        private static bool IsSkipped(string eventName)
        {
            if(string.IsNullOrEmpty(eventName))
            {
                return true;
            }

            if(IsReady)
            {
                return false;
            }

            //어떤 이벤트가 발생했는지는 에디터/디버그에서도 확인할 수 있게 로그만 남긴다.
            if(!bIsSendEnabled)
            {
                Debug.Log($"[Analytics] (전송 생략) {eventName}");
            }

            return true;
        }

        private static void LogEvent(string eventName)
        {
            FirebaseAnalytics.LogEvent(eventName);
            Debug.Log($"[Analytics] {eventName}");
        }

        private static void LogEvent(string eventName, params Parameter[] parameters)
        {
            FirebaseAnalytics.LogEvent(eventName, parameters);
            Debug.Log($"[Analytics] {eventName} ({parameters.Length} params)");
        }
    }
}
