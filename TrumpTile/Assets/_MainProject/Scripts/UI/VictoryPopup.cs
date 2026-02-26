using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TrumpTile.Audio;
using DG.Tweening;
using TrumpTile.Core;

namespace TrumpTile.UI
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
		[SerializeField] private GameObject popupPanel;
		[SerializeField] private TextMeshProUGUI titleText;
		[SerializeField] private Button nextButton;
		[SerializeField] private Button mainButton;

		[Header("Optional")]
		[SerializeField] private TextMeshProUGUI levelText;
		[SerializeField] private TextMeshProUGUI scoreText;
		[SerializeField] private GameObject[] starObjects;

		[Header("Animation")]
		[SerializeField] private float showDelay = 0.3F;
		[SerializeField] private float animationDuration = 0.4F;
		[SerializeField] private Ease showEase = Ease.OutBack;

		[Header("Audio")]
		[SerializeField] private AudioClip victorySound;
		[SerializeField] private AudioClip buttonSound;

		private CanvasGroup mCanvasGroup;
		private RectTransform mPanelRect;
		private bool mHasNextLevel = true;
		private bool mIsButtonClicked = false;

		private void Awake()
		{
			Debug.Log("[VictoryPopup] Awake START");

			// popupPanel이 없으면 자기 자신 사용
			if (popupPanel == null)
			{
				popupPanel = gameObject;
			}

			// CanvasGroup 설정
			mCanvasGroup = popupPanel.GetComponent<CanvasGroup>();
			if (mCanvasGroup == null)
			{
				mCanvasGroup = popupPanel.AddComponent<CanvasGroup>();
			}

			mPanelRect = popupPanel.GetComponent<RectTransform>();

			SetupButtonListeners();

			Debug.Log($"[VictoryPopup] Awake DONE - NextBtn: {nextButton != null}, MainBtn: {mainButton != null}");
		}

		/// <summary>
		/// 버튼 리스너 설정
		/// </summary>
		private void SetupButtonListeners()
		{
			// 기존 리스너 제거 후 새로 연결
			if (nextButton != null)
			{
				nextButton.onClick.RemoveAllListeners();
				nextButton.onClick.AddListener(OnNextButtonClicked);
				Debug.Log("[VictoryPopup] Next button listener added");
			}
			else
			{
				Debug.LogWarning("[VictoryPopup] Next button is NULL!");
			}

			if (mainButton != null)
			{
				mainButton.onClick.RemoveAllListeners();
				mainButton.onClick.AddListener(OnMainButtonClicked);
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
			if (nextButton != null)
			{
				nextButton.interactable = true;
				nextButton.gameObject.SetActive(mHasNextLevel);
			}
			if (mainButton != null)
			{
				mainButton.interactable = true;
			}

			StartCoroutine(ShowCoroutine(level, score, stars));
		}

		private IEnumerator ShowCoroutine(int level, int score, int stars)
		{
			// 패널 활성화
			if (popupPanel != null)
			{
				popupPanel.SetActive(true);

				foreach (Transform child in popupPanel.transform)
				{
					child.gameObject.SetActive(true);
				}
			}

			// NEXT 버튼 표시 여부
			if (nextButton != null)
			{
				nextButton.gameObject.SetActive(mHasNextLevel);
			}

			yield return new WaitForSeconds(showDelay);

			// 사운드
			if (victorySound != null)
			{
				AudioManager.Instance?.PlaySFX(victorySound);
			}
			else
			{
				AudioManager.Instance?.PlayGameClear();
			}

			// 텍스트 설정
			if (titleText != null)
			{
				titleText.text = "YOU WIN!";
			}

			if (levelText != null && level > 0)
			{
				levelText.text = $"Level {level}";
			}

			if (scoreText != null)
			{
				scoreText.text = $"{score:N0}";
			}

			// 별 표시
			if (starObjects != null)
			{
				for (int i = 0; i < starObjects.Length; i++)
				{
					if (starObjects[i] != null)
					{
						starObjects[i].SetActive(i < stars);
					}
				}
			}

			// 애니메이션
			if (mCanvasGroup != null && mPanelRect != null)
			{
				mCanvasGroup.alpha = 0F;
				mPanelRect.localScale = Vector3.one * 0.5F;

				mCanvasGroup.DOFade(1F, animationDuration);
				mPanelRect.DOScale(1F, animationDuration).SetEase(showEase);
			}
			else
			{
				if (popupPanel != null)
				{
					popupPanel.transform.localScale = Vector3.one;
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

			if (popupPanel != null)
			{
				popupPanel.SetActive(false);
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
			if (nextButton != null) nextButton.interactable = false;
			if (mainButton != null) mainButton.interactable = false;

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
			if (nextButton != null) nextButton.interactable = false;
			if (mainButton != null) mainButton.interactable = false;

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
			if (buttonSound != null)
			{
				AudioManager.Instance?.PlaySFX(buttonSound);
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
			if (nextButton != null && nextButton.onClick.GetPersistentEventCount() == 0)
			{
				SetupButtonListeners();
			}
		}
	}
}
