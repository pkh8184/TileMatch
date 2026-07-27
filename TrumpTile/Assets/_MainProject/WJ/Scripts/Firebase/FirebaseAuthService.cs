using System;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
#if !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace TrumpTile.FirebaseLibrary
{
    public static class FirebaseAuthService
    {
        //Firebase 로그인으로 확보한 UID. (서버 Callable 호출은 Firebase SDK가 세션 인증을 자동 첨부하므로,
        // 이 값은 참조/상태 확인용이며 로그인 성공 여부 판단에 사용한다.)
        public static string CachedFirebaseUid { get; private set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(CachedFirebaseUid);

        /// <summary>
        /// Firebase 로그인 후 UID를 캐싱한다.
        /// - 릴리즈 빌드: 구글 플레이 로그인 → Firebase Auth 로그인.
        /// - 에디터 / 디버그(개발) 빌드: GPGS에 의존하지 않도록 고정 테스트 계정(이메일/비번)으로 대체 → UID 고정.
        /// 오프라인/실패 시 네트워크 팝업 없이 조용히 넘어간다. (호출 전 FirebaseService.Initialize() 필요)
        /// </summary>
        public static async Task Login()
        {
#if UNITY_EDITOR
            await LoginWithTestEmail();
#else
            //디버그(개발) 빌드는 실기기에서도 GPGS 없이 바로 테스트할 수 있도록 에디터와 같은 계정을 쓴다.
            if(Debug.isDebugBuild)
            {
                await LoginWithTestEmail();
                return;
            }

            await LoginWithGooglePlay();
#endif
        }

        //에디터/디버그 빌드 공용 고정 테스트 계정. 같은 계정 = 같은 UID라 재시작해도 저장/불러오기가 이어진다.
        //(Firebase 콘솔에서 '이메일/비밀번호' 로그인 제공업체를 켜야 함)
        private const string TEST_ACCOUNT_EMAIL = "editor-test@tilematch.local";
        private const string TEST_ACCOUNT_PASSWORD = "editorTest1234!";

        //Firebase Auth Task가 끝내 완료되지 않는 경우가 있어 단계별 응답 상한을 둔다.
        private const int AUTH_TIMEOUT_MILLISECOND = 10000;

        //GPGS/익명 대신 고정 테스트 계정으로 로그인해 UID를 고정한다.
        //(익명 로그인은 세션이 유지되지 않아 매 실행마다 UID가 바뀌므로 사용하지 않는다)
        private static async Task LoginWithTestEmail()
        {
            AuthResult result = await RequestAuth(
                () => FirebaseService.Auth.SignInWithEmailAndPasswordAsync(TEST_ACCOUNT_EMAIL, TEST_ACCOUNT_PASSWORD), "로그인");

            //계정이 아직 없으면 최초 1회 생성하고 그대로 로그인 상태가 된다.
            if(result == null)
            {
                result = await RequestAuth(
                    () => FirebaseService.Auth.CreateUserWithEmailAndPasswordAsync(TEST_ACCOUNT_EMAIL, TEST_ACCOUNT_PASSWORD), "계정 생성");
            }

            if(result == null)
            {
                CachedFirebaseUid = null;
                return;
            }

            CachedFirebaseUid = result.User.UserId;
            Debug.Log($"[FirebaseAuthService] (테스트 계정) 로그인 성공, UID: {CachedFirebaseUid}");
        }

        //인증 요청 1건을 수행한다. 실패/무응답을 모두 null로 돌려주어 호출부가 멈추지 않게 한다.
        private static async Task<AuthResult> RequestAuth(Func<Task<AuthResult>> authFunc, string stepName)
        {
            try
            {
                Task<AuthResult> authTask = authFunc();

                if(await Task.WhenAny(authTask, Task.Delay(AUTH_TIMEOUT_MILLISECOND)) != authTask)
                {
                    //Status로 원인이 갈린다.
                    //WaitingForActivation = 네이티브 결과가 C#으로 전달되지 않은 것(Firebase 콜백 펌프 문제).
                    //Faulted/Canceled = 요청 자체는 돌아온 것이므로 서버/설정 문제.
                    Debug.LogWarning($"[FirebaseAuthService] (테스트 계정) {stepName} 응답 없음 - "
                        + $"{AUTH_TIMEOUT_MILLISECOND / 1000}초 초과 (Task.Status: {authTask.Status}, "
                        + $"콜백 펌프 오브젝트: {(GameObject.Find("Firebase Services") != null ? "있음" : "없음")})");
                    return null;
                }

                return authTask.Result;
            }
            catch(Exception e)
            {
                Debug.LogWarning($"[FirebaseAuthService] (테스트 계정) {stepName} 실패: {e.Message}");
                return null;
            }
        }

#if !UNITY_EDITOR
        //실기기: 구글 플레이 인증 → 서버 인증 코드 → Firebase Auth 로그인 → UID 캐싱.
        private static async Task LoginWithGooglePlay()
        {
            //1. 구글 플레이 인증 → 서버 인증 코드 (콜백 지연 대비 15초 타임아웃)
            string authCode = null;
            Task<string> codeTask = RequestGoogleServerAuthCode();
            if(await Task.WhenAny(codeTask, Task.Delay(15000)) == codeTask)
            {
                authCode = codeTask.Result;
            }
            else
            {
                Debug.LogWarning("[FirebaseAuthService] 서버 인증 코드 요청 타임아웃(15초)");
            }

            if(string.IsNullOrEmpty(authCode))
            {
                //여기서 막히면 대부분 GPGS의 WebClientId가 '웹' 유형이 아니거나,
                //Play Console에 '게임 서버' 사용자 인증 정보가 없는 경우다.
                Debug.LogWarning("[FirebaseAuthService] 서버 인증 코드를 받지 못함 - Firebase 로그인 중단 "
                    + $"(WebClientId: {GooglePlayGames.GameInfo.WebClientId})");
                CachedFirebaseUid = null;
                return;
            }

            Debug.Log("[FirebaseAuthService] 서버 인증 코드 획득 성공");

            //2. 서버 인증 코드로 Firebase Auth 로그인 → UID 캐싱
            try
            {
                Credential credential = PlayGamesAuthProvider.GetCredential(authCode);
                AuthResult result = await FirebaseService.Auth.SignInAndRetrieveDataWithCredentialAsync(credential);
                CachedFirebaseUid = result.User.UserId;
                Debug.Log($"[FirebaseAuthService] Firebase 로그인 성공, UID 캐싱: {CachedFirebaseUid}");
            }
            catch(Exception e)
            {
                CachedFirebaseUid = null;
                //Firebase 콘솔에서 'Play 게임즈' 제공업체가 꺼져 있거나,
                //거기 등록한 웹 클라이언트 ID/보안 비밀이 GPGS 것과 다르면 여기로 떨어진다.
                Debug.LogWarning($"[FirebaseAuthService] Firebase 로그인 실패: {e.GetType().Name} / {e.Message}");
            }
        }

        //구글 플레이 인증 후 서버 인증 코드를 반환한다(실패 시 null). 콜백 기반 API를 Task로 감싼다.
        private static Task<string> RequestGoogleServerAuthCode()
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if(status == SignInStatus.Success)
                {
                    Debug.Log("[FirebaseAuthService] 구글 플레이 로그인 성공");
                    PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
                    {
                        if(string.IsNullOrEmpty(code))
                        {
                            Debug.LogWarning("[FirebaseAuthService] RequestServerSideAccess가 빈 코드를 반환함");
                        }
                        tcs.TrySetResult(code);
                    });
                }
                else
                {
                    //오프라인/취소 등 → 서버 인증 코드 없음
                    Debug.Log($"[FirebaseAuthService] 구글 플레이 로그인 실패(오프라인 등): {status}");
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }
#endif

        /// <summary>
        /// 로그인을 보장한다. 이미 캐싱된 UID가 있으면 로그인을 생략하고,
        /// 없으면 Firebase 초기화 + 로그인을 수행한다. 반환값은 로그인 성공 여부.
        /// (스테이지 클리어/인앱결제 후 서버 저장 직전에 호출한다.)
        /// </summary>
        public static async Task<bool> EnsureLoggedIn()
        {
            if(IsLoggedIn)
            {
                return true;
            }

            await FirebaseService.Initialize();
            await Login();

            return IsLoggedIn;
        }
    }
}
