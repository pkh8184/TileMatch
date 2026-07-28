using Google.MiniJSON;
using System;
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

        [Header("구매 복원 완료 팝업 (확인 시 닫기만)")]
        [SerializeField] private PublicPopup mRestoreSuccessPopup;

        [Header("복원할 구매 없음 팝업 (확인 시 닫기만)")]
        [SerializeField] private PublicPopup mNothingToRestorePopup;

        [Header("데이터 불러오기 버튼")]
        [SerializeField] private Button mLoadDataButton;

        [Header("서버에 저장된 데이터 없음 팝업")]
        [SerializeField] private PublicPopup mNoDataPopup;

        [Header("데이터 불러오기 성공 팝업 (확인 시 앱 종료)")]
        [SerializeField] private PublicPopup mLoadSuccessPopup;

        [Header("이미 불러온 상태 팝업 (확인 시 닫기만)")]
        [SerializeField] private PublicPopup mAlreadyLoadedPopup;

        //서버 응답을 기다리는 중인지. 연속 클릭으로 서버 요청이 중복 나가는 것을 막는다.
        private bool mbLoadDataInProgress;

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

                //이미 불러온 상태여도 버튼은 살려둔다. 눌러야 "이미 불러왔음" 안내를 띄울 수 있고,
                //LoadFromServer가 플래그를 먼저 확인해 서버 호출 없이 AlreadyLoaded로 즉시 반환하므로 비용도 들지 않는다.
                mLoadDataButton.interactable = true;
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
                //확인 버튼에 별도 동작을 주입하지 않으면 PublicPopup의 기본 동작(Hide)만 수행한다.
                switch(result)
                {
                    case ERestoreResult.Restored:
                        Debug.Log("[_SettingPopup] 광고 제거 구매 복원됨");
                        if(mRestoreSuccessPopup != null)
                        {
                            mRestoreSuccessPopup.Show();
                        }
                        break;
                    case ERestoreResult.NothingToRestore:
                        Debug.Log("[_SettingPopup] 복원할 구매 없음");
                        if(mNothingToRestorePopup != null)
                        {
                            mNothingToRestorePopup.Show();
                        }
                        break;
                    case ERestoreResult.NetworkUnavailable:
                        //IAPManager가 EnsureConnected에서 이미 NETWORK_NOT_CONNECT를 발생시켜 팝업이 뜬다.
                        Debug.Log("[_SettingPopup] 네트워크 미연결");
                        break;
                    case ERestoreResult.Failed:
                        //스토어 미연결 / 타임아웃. 실질적으로 통신 문제라 네트워크 안내 팝업을 재사용한다.
                        Debug.LogWarning("[_SettingPopup] 구매 복원 실패 - 네트워크 안내 팝업 표시");
                        EventManager.Inst?.ActiveEvent(EventKeys.NETWORK_NOT_CONNECT);
                        break;
                }
            });
        }

        private async void OnLoadDataClick()
        {
            if(mbLoadDataInProgress)
            {
                return;
            }
            mbLoadDataInProgress = true;

            ELoadResult result;
            try
            {
                result = await ServerSyncService.LoadFromServer();
            }
            catch(Exception e)
            {
                //예외로 빠지면 아래 switch에 도달하지 못해 아무 안내도 없이 끝난다. Failed로 수렴시킨다.
                Debug.LogError($"[_SettingPopup] 데이터 불러오기 예외: {e}");
                result = ELoadResult.Failed;
            }
            finally
            {
                mbLoadDataInProgress = false;
            }

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
                    //로그인/서버 호출 실패 또는 예외. 실제 원인은 대부분 통신 문제(연결은 잡혔으나 서버 도달 실패)라
                    //전용 팝업을 따로 두지 않고 네트워크 안내 팝업을 재사용한다. (NetworkPopupHandler가 수신)
                    Debug.LogWarning("[_SettingPopup] 데이터 불러오기 실패 - 네트워크 안내 팝업 표시");
                    EventManager.Inst?.ActiveEvent(EventKeys.NETWORK_NOT_CONNECT);
                    break;
                case ELoadResult.AlreadyLoaded:
                    //이미 이 설치에서 불러온 상태 → 서버 호출 없이 안내만 띄운다.
                    //확인 버튼은 별도 주입 없이 기본 동작(Hide)만 수행한다.
                    Debug.Log("[_SettingPopup] 이미 서버 데이터를 불러왔음 (서버 호출 안 함)");
                    if(mAlreadyLoadedPopup != null)
                    {
                        mAlreadyLoadedPopup.Show();
                    }
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

