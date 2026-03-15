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

        public static FirebaseAuth Auth { get => mAuth; }
        public static FirebaseFunctions Functions { get => mFunctions; }

        /// <summary>
        /// 파이어베이스 서비스들을 초기화하는 함수입니다. 게임이 시작될 때 한 번만 실행하면 됩니다.
        /// 비동기로 실행하기 위해 Task를 반환형으로 가집니다.
        /// </summary>
        /// <returns></returns>
        public static async Task Initialize()
        {
            //파이어베이스 기본 설정들을 초기화해줍니다.
            await FirebaseApp.CheckAndFixDependenciesAsync();

            mAuth = FirebaseAuth.DefaultInstance;

            //firestore와 로컬 위치를 맞추기 위해 서울 서버를 읽어옵니다.
            mFunctions = FirebaseFunctions.GetInstance("asia-northeast3");

            //테스트 단계에서는 로컬에서 실행되는 에뮬레이터를 사용합니다.
            mFunctions.UseFunctionsEmulator("http://localhost:5001");
        }
    }
}
