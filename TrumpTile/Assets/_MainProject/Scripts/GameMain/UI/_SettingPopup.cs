using Google.MiniJSON;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class _SettingPopup : PopupBase
    {
        [Header("소리/진동 토글")]
        [SerializeField] private Toggle mBGMToggle;
        [SerializeField] private Toggle mSFXToggle;
        [SerializeField] private Toggle mHapticToggle;

        [Header("이용약관 버튼")]
        [SerializeField] private Button mTermsButton;
        [SerializeField] private Button mPolicyButton;

        [Header("공유 버튼")]
        [SerializeField] private Button mShareButton;

        [Header("앱 버전 텍스트")]
        [SerializeField] private TMP_Text mAppVersionText;

        [Header("구매 복원 버튼")]
        [SerializeField] private Button mRestorePurchaseButton;

        [Header("데이터 불러오기 버튼")]
        [SerializeField] private Button mLoadDataButton;

        [Header("서버에 저장된 데이터 없음 팝업")]
        [SerializeField] private PublicPopup mNoDataPopup;

        [Header("데이터 불러오기 성공 팝업 (확인 시 앱 종료)")]
        [SerializeField] private PublicPopup mLoadSuccessPopup;

        [Header("데이터 불러오기 실패 팝업 (미할당 시 로그만 남김)")]
        [SerializeField] private PublicPopup mLoadFailedPopup;

        [Header("공유 텍스트 및 URL")]
        [SerializeField] private string mShareText;
        [SerializeField] private string mShareURL = "https://";

        public override void Initialize()
        {
            base.Initialize();

            //사운드 관련 UI 초기화
            (bool BGMOn, bool SFXOn, bool HapticOn) soundSetting = PlayerDataManager.Inst.GetUserSoundSettingDatas();
            mBGMToggle.isOn = soundSetting.BGMOn;
            mSFXToggle.isOn = soundSetting.SFXOn;
            mHapticToggle.isOn = soundSetting.HapticOn;
          
            mBGMToggle.onValueChanged.AddListener((isOn) =>
            {
                SetBGMToggle(isOn);
            });
            mSFXToggle.onValueChanged.AddListener((isOn) =>
            {
                SetSFXToggle(isOn);
            });
            mHapticToggle.onValueChanged.AddListener((isOn) =>
            {
                PlayerDataManager.Inst?.SetHapticOn(isOn);
            });

            mTermsButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://pkh8184.github.io/AcidHomePage/#/project/housetilematch/terms");
            });

            mPolicyButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://pkh8184.github.io/AcidHomePage/#/project/housetilematch/privacy");
            });

            mShareButton.onClick.AddListener(() =>
            {
#if UNITY_ANDROID
                ShareURL();
#else
                Debug.Log($"{mShareText}\n{mShareURL}");
#endif
            });
            mAppVersionText.text = Application.version;

            //구매 복원 버튼 (프리팹에 할당된 경우에만 연결)
            if(mRestorePurchaseButton != null)
            {
                mRestorePurchaseButton.onClick.AddListener(OnRestorePurchaseClick);
            }

            //데이터 불러오기 버튼 (프리팹에 할당된 경우에만 연결)
            if(mLoadDataButton != null)
            {
                mLoadDataButton.onClick.AddListener(OnLoadDataClick);

                //이미 이 설치에서 서버 데이터를 불러왔으면 버튼 비활성 (컬러 틴트라 자동으로 어두워짐)
                mLoadDataButton.interactable = !ServerSyncService.IsServerDataLoaded;
            }
        }

        private void OnRestorePurchaseClick()
        {
            if(IAPManager.Instance == null)
            {
                return;
            }

            IAPManager.Instance.RestorePurchases(result =>
            {
                //결과 피드백 지점. 필요 시 결과별 토스트/팝업 연결.
                //(NetworkUnavailable은 NETWORK_NOT_CONNECT 이벤트도 별도로 발생함)
                switch(result)
                {
                    case ERestoreResult.Restored:
                        Debug.Log("[_SettingPopup] 광고 제거 구매 복원됨");
                        break;
                    case ERestoreResult.NothingToRestore:
                        Debug.Log("[_SettingPopup] 복원할 구매 없음");
                        break;
                    case ERestoreResult.NetworkUnavailable:
                        Debug.Log("[_SettingPopup] 네트워크 미연결");
                        break;
                    case ERestoreResult.Failed:
                        Debug.Log("[_SettingPopup] 구매 복원 실패");
                        break;
                }
            });
        }

        private async void OnLoadDataClick()
        {
            ELoadResult result = await ServerSyncService.LoadFromServer();
            switch(result)
            {
                case ELoadResult.Success:
                    Debug.Log("[_SettingPopup] 데이터 불러오기 완료");
                    //"불러오기 성공, 게임 재시작 필요" 팝업 → 확인 누르면 앱 종료.
                    //AddActionToConfirmButton은 동작을 덮어쓰는 방식이라 중복 호출해도 안전하다.
                    if(mLoadSuccessPopup != null)
                    {
                        mLoadSuccessPopup.AddActionToConfirmButton(QuitApplication);
                        mLoadSuccessPopup.Show();
                    }
                    break;
                case ELoadResult.NoData:
                    //서버에 저장된 데이터가 없음(한 번도 온라인 저장 안 한 계정) → 팝업
                    if(mNoDataPopup != null)
                    {
                        mNoDataPopup.Show();
                    }
                    break;
                case ELoadResult.NetworkUnavailable:
                    //네트워크 팝업은 NETWORK_NOT_CONNECT 이벤트 핸들러(NetworkPopupHandler)가 처리
                    break;
                case ELoadResult.Failed:
                    //로그인/서버 호출 실패. 아무 반응이 없으면 유저가 버튼 고장으로 인식하므로 안내를 띄운다.
                    Debug.LogWarning("[_SettingPopup] 데이터 불러오기 실패 - 로그인 또는 서버 호출 실패");
                    if(mLoadFailedPopup != null)
                    {
                        mLoadFailedPopup.Show();
                    }
                    break;
                case ELoadResult.AlreadyLoaded:
                    //이미 이 설치에서 불러온 상태 → 서버 호출 안 함.
                    //이 경우 버튼 자체가 비활성(Initialize에서 처리)이라 실제로는 도달하지 않는다.
                    Debug.Log("[_SettingPopup] 이미 서버 데이터를 불러왔음 (서버 호출 안 함)");
                    break;
            }
        }

        /// <summary>
        /// 서버 데이터를 로컬에 덮어썼기 때문에 재시작이 필요하다. 에디터에서는 플레이 모드를 종료한다.
        /// </summary>
        private void QuitApplication()
        {
            Debug.Log("[_SettingPopup] 불러오기 완료 확인 - 앱 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
#if UNITY_ANDROID
        private void ShareURL()
        {
            string shareContent = $"{mShareText}\n{mShareURL}";
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intentObject.Call<AndroidJavaObject>("setType", "text/plain");
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), shareContent);

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "공유하기");
            currentActivity.Call("startActivity", chooser);
        }
#endif
        private void SetBGMToggle(bool isOn)
        {
            AudioManager.Inst?.SetBGMMute(!isOn);
            PlayerDataManager.Inst?.SetBGMOn(isOn);
        }
        private void SetSFXToggle(bool isOn)
        {
            AudioManager.Inst?.SetSFXMute(!isOn);
            PlayerDataManager.Inst?.SetSFXOn(isOn);
        }
    }  
}

