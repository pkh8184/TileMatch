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
        }
        private IEnumerator Start()
        {
            WaitUntil wait = new WaitUntil(() => !GameObject.Find("StudioLogo"));
            yield return wait;
            Debug.Log("[TitleManager] 로고 송출 완료, 파이어베이스 참조 초기화 및 로그인 실행");

            StartCoroutine(Co_IncreaseLoadingProgress());
            
            yield return FirebaseAuthService.Login();

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

            EventManager.Inst.ActiveEvent("LoadingComplete", (Action)(() => op.allowSceneActivation = true));
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
            //파이어베이스 기능 초기화
            await FirebaseService.Initialize();
            Debug.Log("Firebase 초기화");

            //로그인
            await FirebaseAuthService.Login();
            Debug.Log("로그인 성공");

            object result = await FirebaseFunctionsService.RequestLogin(Application.version);
            if (result is string)
            {           
                EventManager.Inst.ActiveEvent(RequestEventKeys.REQUIRED_VERSION_UPDATE, (object)null);
                Debug.Log("Required Version Update : 앱이 최신 버전이 아닙니다.");
                return;
            }
            //유저 데이터 생성 및 읽어오기
            PlayerDataManager.Inst.Initialize(result as Dictionary<object, object>);

            Debug.Log("데이터 로딩 성공");
        }
    }
}
