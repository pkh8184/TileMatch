using Firebase;
using Firebase.Auth;
using Firebase.Functions;
using System.Threading.Tasks;

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

        //에디터/개발(디버그) 빌드에서 사용할 로컬 Functions 에뮬레이터 주소.
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

            //파이어베이스 기본 설정들을 초기화해줍니다.
            await FirebaseApp.CheckAndFixDependenciesAsync();

            mAuth = FirebaseAuth.DefaultInstance;

            //firestore와 로컬 위치를 맞추기 위해 서울 서버를 읽어옵니다.
            mFunctions = FirebaseFunctions.GetInstance("asia-northeast3");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            //에디터/개발(디버그) 빌드에서는 로컬 에뮬레이터를 사용합니다. 실기기 릴리즈 빌드는 실제 Functions를 호출합니다.
            //주의: 실기기 개발빌드에서 localhost는 기기 자신이므로, PC 에뮬레이터를 쓰려면 adb reverse(tcp:5001) 또는 PC IP로 교체하세요.
            mFunctions.UseFunctionsEmulator(FUNCTIONS_EMULATOR_URL);
#endif

            mbInitialized = true;
        }
    }
}
