using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            if(mbWhitoutFirebase)
            {
                Debug.Log("[TitleManager] 파이어베이스 없이 테스트, 로딩 시작");

                yield return new WaitUntil(() => mLoadingProgress >= 100);

                Debug.Log("씬 전환 이벤트 호출");
                SceneTransister.Inst.TransistScene("MainScene");

                yield break;
            }

            Task task = InitFirebaseService();
            yield return new WaitUntil(() => task.IsCompleted);

            Debug.Log("데이터 로딩 완료, 씬 로딩 실행");
            AsyncOperation op = SceneManager.LoadSceneAsync("MainScene");
            op.allowSceneActivation = false;

            while(!op.isDone)
            {
                if (op.progress >= 0.9f)
                {
                    break;
                }
                yield return null;
            }
            Debug.Log("로딩 성공");

            yield return new WaitUntil(() => mLoadingProgress >= 100);

            EventManager.Inst.ActiveEvent(RequestEventKeys.LOADING_COMPLETE, (Action)(() => op.allowSceneActivation = true));
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
            }
            catch (Exception e)
            {
                //오프라인/서버 실패 시에도 부팅이 멈추지 않도록 로컬 데이터로 진행한다.
                Debug.LogWarning($"[TitleManager] 온라인 초기화 실패(오프라인 진행): {e.Message}");
            }
        }
    }
}
