using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using System.Threading.Tasks;
using UnityEngine;

namespace TrumpTile.FirebaseLibrary
{
    /// <summary>
    /// 파이어베이스 서비스를 초기화하고, 각 서비스에 인스턴스를 제공하는 스태틱 클래스입니다.
    /// </summary>
    public static class FirebaseService
    {
        private static FirebaseAuth mAuth;
        private static FirebaseFunctions mFunctions;
        private static bool mbInitialized;

        public static bool IsInitialized => mbInitialized;

        //로컬 Functions 에뮬레이터 주소와 사용 여부.
        //기본은 false(에디터에서도 실제 배포된 Functions를 호출한다).
        //에뮬레이터를 직접 띄워놓고 테스트할 때만 true로 바꾼다.
        private const bool USE_FUNCTIONS_EMULATOR = false;
        private const string FUNCTIONS_EMULATOR_URL = "http://localhost:5001";

        public static FirebaseAuth Auth { get => mAuth; }
        public static FirebaseFunctions Functions { get => mFunctions; }

        /// <summary>
        /// 파이어베이스 서비스들을 초기화하는 함수입니다. 게임이 시작될 때 한 번만 실행하면 됩니다.
        /// 비동기로 실행하기 위해 Task를 반환형으로 가집니다.
        /// </summary>
        /// <returns></returns>
        public static async Task Initialize()
        {
            //중복 초기화 방지 (스테이지 클리어/구매 등에서 재호출될 수 있음)
            if(mbInitialized)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            //네이티브(C++) SDK가 내부에서 무엇을 하는지 콘솔로 뽑는다.
            //응답이 오지 않는 요청을 추적할 때 이 로그가 유일한 단서다.
            //반드시 첫 FirebaseApp 접근보다 먼저 설정해야 적용된다.
            FirebaseApp.LogLevel = LogLevel.Verbose;
#endif

            //파이어베이스 기본 설정들을 초기화해줍니다.
            //Available이 아니면 Auth/Functions가 정상 동작하지 않으므로 상태를 남기고 중단한다.
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if(status != DependencyStatus.Available)
            {
                Debug.LogError($"[FirebaseService] 초기화 실패 - 의존성 상태: {status}");
                return;
            }

            mAuth = FirebaseAuth.DefaultInstance;

            //firestore와 로컬 위치를 맞추기 위해 서울 서버를 읽어옵니다.
            mFunctions = FirebaseFunctions.GetInstance("asia-northeast3");

#if UNITY_EDITOR
            //에뮬레이터는 스위치를 켰을 때만 사용하고, 기본은 실제 Functions를 호출합니다.
            //실기기는 개발빌드여도 항상 실제 Functions를 호출합니다.
            //(기기에서 localhost는 기기 자신이라, 개발빌드에 에뮬레이터를 걸면 모든 서버 호출이 조용히 실패한다)
            if(USE_FUNCTIONS_EMULATOR)
            {
                mFunctions.UseFunctionsEmulator(FUNCTIONS_EMULATOR_URL);
                Debug.Log($"[FirebaseService] 에디터 - Functions 에뮬레이터 사용: {FUNCTIONS_EMULATOR_URL}");
            }
#endif

            mbInitialized = true;

            //Analytics는 로그인이 필요 없고, 초기화 완료 후에만 호출해야 예외가 나지 않는다.
            FirebaseAnalyticsService.SetCollectionEnabled(true);

            Debug.Log("[FirebaseService] 초기화 완료 (region: asia-northeast3)");
        }
    }
}
