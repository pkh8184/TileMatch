using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 승리 화면 팝업
	///
	/// [주의사항]
	/// - Inspector에서 Next Button, Main Button 연결 필수
	/// - 버튼의 OnClick 이벤트는 코드에서 연결 (Inspector에서 연결하지 말 것)
	/// </summary>
	public class VictoryPopup : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private GameObject mPopupPanel;
		[SerializeField] private TextMeshProUGUI mTitleText;
		[SerializeField] private Button mNextButton;
		[SerializeField] private Button mMainButton;

		[Header("Optional")]
		[SerializeField] private TextMeshProUGUI mLevelText;
		[SerializeField] private TextMeshProUGUI mScoreText;
		[SerializeField] private GameObject[] mStarObjects;

		[Header("Animation")]
		[SerializeField] private float mShowDelay = 0.3F;
		[SerializeField] private float mAnimationDuration = 0.4F;
		[SerializeField] private Ease mShowEase = Ease.OutBack;

		[Header("Audio")]
		[SerializeField] private AudioClip mVictorySound;
		[SerializeField] private AudioClip mButtonSound;

		private CanvasGroup mCanvasGroup;
		private RectTransform mPanelRect;
		private bool mHasNextLevel = true;
		private bool mIsButtonClicked = false;

		private void Awake()
		{
			Debug.Log("[VictoryPopup] Awake START");

			// mPopupPanel이 없으면 자기 자신 사용
			if (mPopupPanel == null)
			{
				mPopupPanel = gameObject;
			}

			// CanvasGroup 설정
			mCanvasGroup = mPopupPanel.GetComponent<CanvasGroup>();
			if (mCanvasGroup == null)
			{
				mCanvasGroup = mPopupPanel.AddComponent<CanvasGroup>();
			}

			mPanelRect = mPopupPanel.GetComponent<RectTransform>();

			SetupButtonListeners();

			Debug.Log($"[VictoryPopup] Awake DONE - NextBtn: {mNextButton != null}, MainBtn: {mMainButton != null}");
		}

		/// <summary>
		/// 버튼 리스너 설정
		/// </summary>
		private void SetupButtonListeners()
		{
			// 기존 리스너 제거 후 새로 연결
			if (mNextButton != null)
			{
				mNextButton.onClick.RemoveAllListeners();
				mNextButton.onClick.AddListener(OnNextButtonClicked);
				Debug.Log("[VictoryPopup] Next button listener added");
			}
			else
			{
				Debug.LogWarning("[VictoryPopup] Next button is NULL!");
			}

			if (mMainButton != null)
			{
				mMainButton.onClick.RemoveAllListeners();
				mMainButton.onClick.AddListener(OnMainButtonClicked);
				Debug.Log("[VictoryPopup] Main button listener added");
			}
			else
			{
				Debug.LogWarning("[VictoryPopup] Main button is NULL!");
			}
		}

		/// <summary>
		/// 승리 팝업 표시
		/// </summary>
		public void Show(int level = 0, int score = 0, int stars = 3, bool hasNext = true)
		{
			Debug.Log($"[VictoryPopup] Show - Level: {level}, Score: {score}, Stars: {stars}, HasNext: {hasNext}");

			mHasNextLevel = hasNext;
			mIsButtonClicked = false;

			// 게임 오브젝트 활성화
			gameObject.SetActive(true);

			// 버튼 활성화
			if (mNextButton != null)
			{
				mNextButton.interactable = true;
				mNextButton.gameObject.SetActive(mHasNextLevel);
			}
			if (mMainButton != null)
			{
				mMainButton.interactable = true;
			}

			StartCoroutine(ShowCoroutine(level, score, stars));
		}

		private IEnumerator ShowCoroutine(int level, int score, int stars)
		{
			// 패널 활성화
			if (mPopupPanel != null)
			{
				mPopupPanel.SetActive(true);

				foreach (Transform child in mPopupPanel.transform)
				{
					child.gameObject.SetActive(true);
				}
			}

			// NEXT 버튼 표시 여부
			if (mNextButton != null)
			{
				mNextButton.gameObject.SetActive(mHasNextLevel);
			}

			yield return new WaitForSeconds(mShowDelay);

			// 사운드
			if (mVictorySound != null)
			{
				AudioManager.Instance?.PlaySFX(mVictorySound);
			}
			else
			{
				AudioManager.Instance?.PlayGameClear();
			}

			// 텍스트 설정
			if (mTitleText != null)
			{
				mTitleText.text = "YOU WIN!";
			}

			if (mLevelText != null && level > 0)
			{
				mLevelText.text = $"Level {level}";
			}

			if (mScoreText != null)
			{
				mScoreText.text = $"{score:N0}";
			}

			// 별 표시
			if (mStarObjects != null)
			{
				for (int i = 0; i < mStarObjects.Length; i++)
				{
					if (mStarObjects[i] != null)
					{
						mStarObjects[i].SetActive(i < stars);
					}
				}
			}

			// 애니메이션
			if (mCanvasGroup != null && mPanelRect != null)
			{
				mCanvasGroup.alpha = 0F;
				mPanelRect.localScale = Vector3.one * 0.5F;

				mCanvasGroup.DOFade(1F, mAnimationDuration);
				mPanelRect.DOScale(1F, mAnimationDuration).SetEase(mShowEase);
			}
			else
			{
				if (mPopupPanel != null)
				{
					mPopupPanel.transform.localScale = Vector3.one;
				}
				if (mCanvasGroup != null)
				{
					mCanvasGroup.alpha = 1F;
				}
			}
		}

		/// <summary>
		/// 팝업 숨기기
		/// </summary>
		public void Hide()
		{
			Debug.Log("[VictoryPopup] Hide");

			if (mPopupPanel != null)
			{
				mPopupPanel.SetActive(false);
			}
			gameObject.SetActive(false);
		}

		/// <summary>
		/// NEXT 버튼 클릭 핸들러
		/// </summary>
		private void OnNextButtonClicked()
		{
			Debug.Log("[VictoryPopup] === NEXT BUTTON CLICKED ===");

			if (mIsButtonClicked)
			{
				Debug.Log("[VictoryPopup] Button already clicked, ignoring");
				return;
			}
			mIsButtonClicked = true;

			PlayButtonSound();

			// 버튼 비활성화
			if (mNextButton != null)
			{
				mNextButton.interactable = false;
			}
			if (mMainButton != null)
			{
				mMainButton.interactable = false;
			}

			// 팝업 숨기기
			Hide();

			// 다음 레벨로 이동
			if (GameManager.Instance != null)
			{
				Debug.Log("[VictoryPopup] Calling GameManager.NextLevel()");
				GameManager.Instance.NextLevel();
			}
			else
			{
				Debug.LogError("[VictoryPopup] GameManager.Instance is NULL!");
			}
		}

		/// <summary>
		/// MAIN 버튼 클릭 핸들러
		/// </summary>
		private void OnMainButtonClicked()
		{
			Debug.Log("[VictoryPopup] === MAIN BUTTON CLICKED ===");

			if (mIsButtonClicked)
			{
				Debug.Log("[VictoryPopup] Button already clicked, ignoring");
				return;
			}
			mIsButtonClicked = true;

			PlayButtonSound();

			// 버튼 비활성화
			if (mNextButton != null)
			{
				mNextButton.interactable = false;
			}
			if (mMainButton != null)
			{
				mMainButton.interactable = false;
			}

			// 팝업 숨기기
			Hide();

			// 메인 메뉴로 이동
			if (GameManager.Instance != null)
			{
				Debug.Log("[VictoryPopup] Calling GameManager.GoToMainMenu()");
				GameManager.Instance.GoToMainMenu();
			}
			else
			{
				Debug.LogError("[VictoryPopup] GameManager.Instance is NULL!");
			}
		}

		private void PlayButtonSound()
		{
			if (mButtonSound != null)
			{
				AudioManager.Instance?.PlaySFX(mButtonSound);
			}
			else
			{
				AudioManager.Instance?.PlayButtonClick();
			}
		}

		/// <summary>
		/// OnEnable에서 버튼 리스너 재설정 (혹시 모를 경우 대비)
		/// </summary>
		private void OnEnable()
		{
			// Awake에서 이미 설정했지만, 혹시 모르니 다시 확인
			if (mNextButton != null && mNextButton.onClick.GetPersistentEventCount() == 0)
			{
				SetupButtonListeners();
			}
		}
	}
}
