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
        /// - 실기기: 구글 플레이 로그인 → Firebase Auth 로그인.
        /// - 에디터: GPGS가 동작하지 않으므로 고정 테스트 계정(이메일/비번)으로 대체 → UID 고정.
        /// 오프라인/실패 시 네트워크 팝업 없이 조용히 넘어간다. (호출 전 FirebaseService.Initialize() 필요)
        /// </summary>
        public static async Task Login()
        {
#if UNITY_EDITOR
            await LoginForEditor();
#else
            await LoginWithGooglePlay();
#endif
        }

#if UNITY_EDITOR
        //에디터 테스트용 고정 계정. 같은 계정 = 같은 UID라 재시작해도 저장/불러오기가 이어진다.
        //(Firebase 콘솔에서 '이메일/비밀번호' 로그인 제공업체를 켜야 함)
        private const string EDITOR_TEST_EMAIL = "editor-test@tilematch.local";
        private const string EDITOR_TEST_PASSWORD = "editorTest1234!";

        //에디터에서는 GPGS/익명 대신 고정 테스트 계정으로 로그인해 UID를 고정한다.
        //(익명 로그인은 에디터에서 세션이 유지되지 않아 매 실행마다 UID가 바뀌므로 사용하지 않는다)
        private static async Task LoginForEditor()
        {
            try
            {
                AuthResult result;
                try
                {
                    result = await FirebaseService.Auth.SignInWithEmailAndPasswordAsync(EDITOR_TEST_EMAIL, EDITOR_TEST_PASSWORD);
                }
                catch
                {
                    //계정이 아직 없으면 최초 1회 생성하고 그대로 로그인 상태가 된다.
                    result = await FirebaseService.Auth.CreateUserWithEmailAndPasswordAsync(EDITOR_TEST_EMAIL, EDITOR_TEST_PASSWORD);
                }

                CachedFirebaseUid = result.User.UserId;
                Debug.Log($"[FirebaseAuthService] (에디터) 테스트 계정 로그인, UID: {CachedFirebaseUid}");
            }
            catch(Exception e)
            {
                CachedFirebaseUid = null;
                Debug.LogWarning($"[FirebaseAuthService] (에디터) 테스트 계정 로그인 실패: {e.Message}");
            }
        }
#else
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
