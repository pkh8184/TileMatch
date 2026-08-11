using System;
using System.Collections;
using System.Threading.Tasks;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public class TitleManager : MonoBehaviour
    {
        [Header("로딩 홀드 구간(마지막 인덱스는 무조건 100)")]
        [SerializeField, Range(0, 100)] private float[] mLoadingProgressHoldArray;
        [Header("구간 ~ 구간까지의 로딩 속도(초)")]
        [SerializeField] private float mLoadingDuration;
        [Header("구간 도착 후 다음 구간까지 지연시간")]
        [SerializeField] private float mHoldSecond;
        [Header("테스트용 플래그(Firebase 에뮬레이터 없이 타이틀씬 사용 시 체크)")]
        [SerializeField] private bool mbWhitoutFirebase;

        //온라인 초기화(Firebase 초기화 + 로그인 + 서버 데이터 자동 복원) 최대 대기 시간.
        //Firebase Task가 끝내 완료되지 않는 경우가 있어, 부팅이 여기서 멈추지 않도록 상한을 둔다.
        //자동 복원의 서버 왕복이 포함되므로 로그인만 하던 때(10초)보다 여유를 뒀다.
        private const float ONLINE_INIT_TIMEOUT_SECOND = 15f;

        private float mLoadingProgress = 0;
        public float LoadingProgress { get => mLoadingProgress; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.SetResolution(1080, 1920, true);
            
            PlayerDataManager.Inst.Initialize();
            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);

            foreach (var item in uiBaseArray)
            {
                item.Initialize();
            }
            //임시
            GooglePlayGames.PlayGamesPlatform.Activate();
        }
        private IEnumerator Start()
        {
            WaitUntil wait = new WaitUntil(() => !GameObject.Find("StudioLogo"));
            yield return wait;
            Debug.Log("[TitleManager] 로고 송출 완료, 파이어베이스 참조 초기화 및 로그인 실행");

            StartCoroutine(Co_IncreaseLoadingProgress());

            //테스트 플래그와 실제 부팅이 서로 다른 씬 전환 경로를 타면 한쪽만 깨져도 알아채기 어렵다.
            //초기화 여부만 분기하고, 씬 전환은 아래 한 경로로 합친다.
            if(mbWhitoutFirebase)
            {
                Debug.Log("[TitleManager] 파이어베이스 없이 테스트, 로딩 시작");
            }
            else
            {
                Task task = InitFirebaseService();

                //응답이 오지 않는 Task를 무한정 기다리면 타이틀에서 부팅이 멈춘다.
                //상한을 넘기면 로그만 남기고 로컬 데이터로 진행한다(로그인은 이후 저장 시점에 재시도된다).
                float elapsed = 0f;
                yield return new WaitUntil(() =>
                {
                    elapsed += Time.unscaledDeltaTime;
                    return task.IsCompleted || elapsed >= ONLINE_INIT_TIMEOUT_SECOND;
                });

                if(!task.IsCompleted)
                {
                    Debug.LogWarning($"[TitleManager] 온라인 초기화 {ONLINE_INIT_TIMEOUT_SECOND}초 초과 - 로컬 데이터로 진행");
                }

                Debug.Log("데이터 로딩 완료, 씬 로딩 실행");
            }

            yield return new WaitUntil(() => mLoadingProgress >= 100);

            Debug.Log("씬 전환 이벤트 호출");
            SceneTransister.Inst.TransistScene("MainScene");
        }
        private IEnumerator Co_IncreaseLoadingProgress()
        {
            int holdIndex = 0;
            WaitForSeconds wait = new WaitForSeconds(mHoldSecond);
            while (mLoadingProgress < 100)
            {
                float startProgress = mLoadingProgress;
                float elapsed = 0f;    
                while (elapsed < mLoadingDuration)
                {
                    elapsed += Time.deltaTime;
                    mLoadingProgress = Mathf.Lerp(startProgress, mLoadingProgressHoldArray[holdIndex], elapsed / mLoadingDuration);
                    yield return null;
                }
                holdIndex++;
                yield return wait;
            }
        }
        private async Task InitFirebaseService()
        {
            //타이틀 로그인과 동일하게, 오프라인이면 팝업 없이 조용히 서버 초기화를 스킵한다(로컬 데이터로 진행).
            if(!NetworkUtil.IsConnected())
            {
                return;
            }

            try
            {
                //파이어베이스 기능 초기화
                await FirebaseService.Initialize();
                Debug.Log("Firebase 초기화");

                //구글 플레이 로그인 → Firebase Auth 로그인 → Firebase UID 캐싱
                await FirebaseAuthService.Login();

                //주의: 여기서 progressLogin으로 서버 유저 문서를 미리 만들지 않는다.
                //서버 문서는 최초 saveData(스테이지 클리어/구매) 때 생성되고, 데이터 불러오기(loadData)로 읽는다.
                //(부팅 때 문서를 만들면 '저장된 데이터 없음(NoData)' 판정이 불가능해짐)
                Debug.Log(FirebaseAuthService.IsLoggedIn
                    ? "[TitleManager] 로그인 성공, UID 캐싱 완료"
                    : "[TitleManager] 로그인 실패 - 로컬 데이터로 진행");

                //로그인된 계정 기준으로, 이 설치가 아직 서버 데이터를 받은 적 없으면 여기서 자동 복원한다.
                //메인씬 진입 전에 끝내야 재설치 유저가 복원 전에 플레이해서 서버를 덮어쓰는 사고를 막을 수 있다.
                //(게임 시작 전에 데이터가 확정되므로 예전처럼 "재시작 필요" 안내가 필요 없다)
                if(FirebaseAuthService.IsLoggedIn)
                {
                    ELoadResult restoreResult = await ServerSyncService.TryAutoRestoreOnBoot();
                    Debug.Log($"[TitleManager] 부팅 자동 복원 결과: {restoreResult}");
                }
            }
            catch (Exception e)
            {
                //오프라인/서버 실패 시에도 부팅이 멈추지 않도록 로컬 데이터로 진행한다.
                Debug.LogWarning($"[TitleManager] 온라인 초기화 실패(오프라인 진행): {e.Message}");
            }
        }
    }
}
