using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TrumpTile.Data;
using TrumpTile.Audio;

namespace TrumpTile.UI
{
    /// <summary>
    /// 메인 화면 UI 관리
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("상단 버튼")]
        [SerializeField] private Button settingButton;      // 설정
        [SerializeField] private Button mapButton;          // 지도 (하우징)
        [SerializeField] private Button shopButton;         // 상점

        [Header("재화")]
        [SerializeField] private TextMeshProUGUI goldText;  // 골드 표시

        [Header("스테이지")]
        [SerializeField] private Button stageButton;        // 스테이지 버튼
        [SerializeField] private TextMeshProUGUI stageLevelText; // "LEVEL 1" 표시

        [Header("프로필")]
        [SerializeField] private Button profileButton;      // 프로필 버튼
        [SerializeField] private Image profileImage;        // 프로필 이미지

        [Header("팝업")]
        [SerializeField] private GameObject settingPopup;
        [SerializeField] private GameObject mapPopup;
        [SerializeField] private GameObject shopPopup;
        [SerializeField] private GameObject profilePopup;
        [SerializeField] private GameObject stageSelectPopup;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SetupButtons();
            RefreshUI();
        }

        private void SetupButtons()
        {
            // 상단 버튼
            if (settingButton != null)
                settingButton.onClick.AddListener(OnSettingClick);

            if (mapButton != null)
                mapButton.onClick.AddListener(OnMapClick);

            if (shopButton != null)
                shopButton.onClick.AddListener(OnShopClick);

            // 스테이지 버튼
            if (stageButton != null)
                stageButton.onClick.AddListener(OnStageClick);

            // 프로필 버튼
            if (profileButton != null)
                profileButton.onClick.AddListener(OnProfileClick);
        }

        /// <summary>
        /// UI 새로고침
        /// </summary>
        public void RefreshUI()
        {
            UpdateGold();
            UpdateStageLevel();
            UpdateProfile();
        }

        #region Update UI

        /// <summary>
        /// 골드 표시 업데이트
        /// </summary>
        public void UpdateGold()
        {
            if (goldText != null && UserDataManager.Instance != null)
            {
                int gold = UserDataManager.Instance.Gold;
                goldText.text = FormatNumber(gold);
            }
        }

        /// <summary>
        /// 스테이지 레벨 표시 업데이트
        /// </summary>
        public void UpdateStageLevel()
        {
            if (stageLevelText != null && UserDataManager.Instance != null)
            {
                int currentStage = UserDataManager.Instance.CurrentStage;
                stageLevelText.text = $"LEVEL {currentStage}";
            }
        }

        /// <summary>
        /// 프로필 업데이트
        /// </summary>
        public void UpdateProfile()
        {
            if (profileImage != null && UserDataManager.Instance != null)
            {
                // 프로필 이미지 로드 (나중에 구현)
                // Sprite profileSprite = UserDataManager.Instance.GetProfileSprite();
                // profileImage.sprite = profileSprite;
            }
        }

        /// <summary>
        /// 숫자 포맷팅 (1000 -> 1,000)
        /// </summary>
        private string FormatNumber(int number)
        {
            return string.Format("{0:N0}", number);
        }

        #endregion

        #region Button Callbacks

        private void OnSettingClick()
        {
            Debug.Log("[MainMenuUI] Setting clicked");
            AudioManager.Instance?.PlayButtonClick();
            
            // 설정 팝업 열기
            if (settingPopup != null)
                settingPopup.SetActive(true);
        }

        private void OnMapClick()
        {
            Debug.Log("[MainMenuUI] Map clicked");
            AudioManager.Instance?.PlayButtonClick();
            
            // 지도(하우징) 팝업 열기
            if (mapPopup != null)
                mapPopup.SetActive(true);
        }

        private void OnShopClick()
        {
            Debug.Log("[MainMenuUI] Shop clicked");
            AudioManager.Instance?.PlayButtonClick();
            
            // 상점 팝업 열기
            if (shopPopup != null)
                shopPopup.SetActive(true);
        }

        private void OnStageClick()
        {
            Debug.Log("[MainMenuUI] Stage clicked");
            AudioManager.Instance?.PlayButtonClick();
            
            // 스테이지 선택 팝업 또는 바로 게임 시작
            if (stageSelectPopup != null)
            {
                stageSelectPopup.SetActive(true);
            }
            else
            {
                // 팝업 없으면 바로 현재 스테이지로 게임 시작
                StartGame();
            }
        }

        private void OnProfileClick()
        {
            Debug.Log("[MainMenuUI] Profile clicked");
            AudioManager.Instance?.PlayButtonClick();
            
            // 프로필 팝업 열기
            if (profilePopup != null)
                profilePopup.SetActive(true);
        }

        #endregion

        #region Game Start

        /// <summary>
        /// 게임 시작 (현재 스테이지)
        /// </summary>
        public void StartGame()
        {
            int currentStage = UserDataManager.Instance?.CurrentStage ?? 1;
            StartGame(currentStage);
        }

        /// <summary>
        /// 게임 시작 (특정 스테이지)
        /// </summary>
        public void StartGame(int stageLevel)
        {
            Debug.Log($"[MainMenuUI] Starting game - Stage {stageLevel}");
            
            // 스테이지 정보 저장
            if (UserDataManager.Instance != null)
                UserDataManager.Instance.SetSelectedStage(stageLevel);

            // 씬 전환 (Blur 효과)
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.LoadScene("GameScene");
            }
            else
            {
                // TransitionManager 없으면 직접 로드
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }
        }

        #endregion
    }
}
