using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TrumpTile.Core;
using TrumpTile.Audio;

namespace TileMatch
{
	public class UIManager : MonoBehaviour
	{
		public static UIManager Instance { get; private set; }

		[Header("HUD")]
		[SerializeField] private TextMeshProUGUI scoreText;
		[SerializeField] private TextMeshProUGUI levelText;
		[SerializeField] private TextMeshProUGUI goldText;
		[SerializeField] private TextMeshProUGUI comboText;
		[SerializeField] private TextMeshProUGUI timerText;
		[SerializeField] private Slider timerSlider;

		[Header("Items")]
		[SerializeField] private TextMeshProUGUI strikeCountText;
		[SerializeField] private TextMeshProUGUI blackHoleCountText;
		[SerializeField] private TextMeshProUGUI boomCountText;
		[SerializeField] private Button strikeButton;
		[SerializeField] private Button blackHoleButton;
		[SerializeField] private Button boomButton;
		[SerializeField] private Button pauseButton;

		[Header("Panels")]
		[SerializeField] private GameObject levelClearPanel;
		[SerializeField] private GameObject pausePanel;

		[Header("Level Clear Panel")]
		[SerializeField] private TextMeshProUGUI clearLevelText;
		[SerializeField] private TextMeshProUGUI clearScoreText;
		[SerializeField] private Button nextLevelButton;
		[SerializeField] private Button clearRestartButton;
		[SerializeField] private Button clearMainMenuButton;

		[Header("Pause Panel")]
		[SerializeField] private Button resumeButton;
		[SerializeField] private Button pauseRestartButton;
		[SerializeField] private Button pauseMainMenuButton;

		[Header("Floating Text")]
		[SerializeField] private GameObject floatingTextPrefab;
		[SerializeField] private Transform floatingTextParent;

		[Header("Animation")]
		[SerializeField] private Animator comboAnimator;

		// 아이템 카운트 캐시
		private int strikeCount;
		private int blackHoleCount;
		private int boomCount;

		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			SetupButtons();
			SubscribeEvents();
			HideAllPanels();
		}

		private void SetupButtons()
		{
			// 아이템 버튼
			if (strikeButton != null)
				strikeButton.onClick.AddListener(OnStrikeClick);

			if (blackHoleButton != null)
				blackHoleButton.onClick.AddListener(OnBlackHoleClick);

			if (boomButton != null)
				boomButton.onClick.AddListener(OnBoomClick);

			if (pauseButton != null)
				pauseButton.onClick.AddListener(OnPauseClick);

			// Level Clear 패널 버튼
			if (nextLevelButton != null)
				nextLevelButton.onClick.AddListener(OnNextLevelClick);

			if (clearRestartButton != null)
				clearRestartButton.onClick.AddListener(OnRestartClick);

			if (clearMainMenuButton != null)
				clearMainMenuButton.onClick.AddListener(OnMainMenuClick);

			// Pause 패널 버튼
			if (resumeButton != null)
				resumeButton.onClick.AddListener(OnResumeClick);

			if (pauseRestartButton != null)
				pauseRestartButton.onClick.AddListener(OnRestartClick);

			if (pauseMainMenuButton != null)
				pauseMainMenuButton.onClick.AddListener(OnMainMenuClick);
		}

		private void SubscribeEvents()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.OnScoreChanged += UpdateScore;
				GameManager.Instance.OnComboChanged += UpdateCombo;
				GameManager.Instance.OnItemCountChanged += UpdateItems;
				GameManager.Instance.OnProgressChanged += UpdateProgress;
			}
		}

		private void OnDestroy()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.OnScoreChanged -= UpdateScore;
				GameManager.Instance.OnComboChanged -= UpdateCombo;
				GameManager.Instance.OnItemCountChanged -= UpdateItems;
				GameManager.Instance.OnProgressChanged -= UpdateProgress;
			}
		}

		#region Button Callbacks

		private void OnStrikeClick()
		{
			if (GameManager.Instance == null || !GameManager.Instance.CanUseItem()) return;
			AudioManager.Instance?.PlayButtonClick();
			GameManager.Instance.UseStrike();
		}

		private void OnBlackHoleClick()
		{
			if (GameManager.Instance == null || !GameManager.Instance.CanUseItem()) return;
			AudioManager.Instance?.PlayButtonClick();
			GameManager.Instance.UseBlackHole();
		}

		private void OnBoomClick()
		{
			if (GameManager.Instance == null || !GameManager.Instance.CanUseItem()) return;
			AudioManager.Instance?.PlayButtonClick();
			GameManager.Instance.UseBoom();
		}

		private void OnPauseClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			GameManager.Instance?.PauseGame();
			ShowPausePanel();
		}

		private void OnResumeClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			HideAllPanels();
			GameManager.Instance?.ResumeGame();
		}

		private void OnRestartClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			HideAllPanels();
			Time.timeScale = 1f;
			GameManager.Instance?.RestartLevel();
		}

		private void OnNextLevelClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			HideAllPanels();
			GameManager.Instance?.NextLevel();
		}

		private void OnMainMenuClick()
		{
			AudioManager.Instance?.PlayButtonClick();
			HideAllPanels();
			Time.timeScale = 1f;
			GameManager.Instance?.GoToMainMenu();
		}

		#endregion

		#region Update UI

		public void UpdateScore(int score)
		{
			if (scoreText != null)
			{
				scoreText.text = $"{score:N0}";
				StartCoroutine(ScorePunchAnimation());
			}
		}

		private IEnumerator ScorePunchAnimation()
		{
			if (scoreText == null) yield break;

			Vector3 originalScale = Vector3.one;
			Vector3 punchScale = Vector3.one * 1.2f;

			scoreText.transform.localScale = punchScale;

			float duration = 0.15f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				scoreText.transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
				yield return null;
			}

			scoreText.transform.localScale = originalScale;
		}

		public void UpdateLevel(int level)
		{
			if (levelText != null)
				levelText.text = $"Level {level}";
		}

		public void UpdateGold(int gold)
		{
			if (goldText != null)
				goldText.text = $"{gold:N0}";
		}

		public void UpdateCombo(int combo)
		{
			if (comboText != null)
			{
				if (combo > 1)
				{
					comboText.gameObject.SetActive(true);
					comboText.text = $"x{combo}";

					if (comboAnimator != null)
						comboAnimator.SetTrigger("Pulse");

					StartCoroutine(ComboPunchAnimation());
				}
				else
				{
					comboText.gameObject.SetActive(false);
				}
			}
		}

		private IEnumerator ComboPunchAnimation()
		{
			if (comboText == null) yield break;

			Vector3 punchScale = Vector3.one * 1.5f;
			comboText.transform.localScale = punchScale;

			float duration = 0.2f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				comboText.transform.localScale = Vector3.Lerp(punchScale, Vector3.one, elapsed / duration);
				yield return null;
			}

			comboText.transform.localScale = Vector3.one;
		}

		public void UpdateTimer(float timeRemaining)
		{
			if (timerText != null)
			{
				int minutes = Mathf.FloorToInt(timeRemaining / 60);
				int seconds = Mathf.FloorToInt(timeRemaining % 60);
				timerText.text = $"{minutes:D2}:{seconds:D2}";
				timerText.color = timeRemaining < 10 ? Color.red : Color.white;
			}
		}

		public void UpdateItems(int strike, int blackHole, int boom)
		{
			strikeCount = strike;
			blackHoleCount = blackHole;
			boomCount = boom;

			if (strikeCountText != null)
				strikeCountText.text = strike.ToString();

			if (blackHoleCountText != null)
				blackHoleCountText.text = blackHole.ToString();

			if (boomCountText != null)
				boomCountText.text = boom.ToString();

			UpdateItemButtonStates();
		}

		/// <summary>
		/// 아이템 버튼 활성화/비활성화 상태 업데이트
		/// </summary>
		public void UpdateItemButtonStates()
		{
			// 게임 상태 체크 (Playing 또는 Loading 상태에서 사용 가능)
			bool canUse = true;

			if (GameManager.Instance != null)
			{
				var state = GameManager.Instance.CurrentState;
				canUse = (state == GameManager.GameState.Playing || state == GameManager.GameState.Loading);
			}

			if (strikeButton != null)
				strikeButton.interactable = canUse && strikeCount > 0;

			if (blackHoleButton != null)
				blackHoleButton.interactable = canUse && blackHoleCount > 0;

			if (boomButton != null)
				boomButton.interactable = canUse && boomCount > 0;
		}

		/// <summary>
		/// 게임 종료 시 아이템 버튼 비활성화
		/// </summary>
		public void DisableItemButtons()
		{
			if (strikeButton != null)
				strikeButton.interactable = false;

			if (blackHoleButton != null)
				blackHoleButton.interactable = false;

			if (boomButton != null)
				boomButton.interactable = false;
		}

		public void UpdateProgress(int matched, int total)
		{
			// 진행률 표시 (필요시 구현)
		}

		#endregion

		#region Panels

		public void HideAllPanels()
		{
			if (levelClearPanel != null) levelClearPanel.SetActive(false);
			if (pausePanel != null) pausePanel.SetActive(false);
		}

		public void ShowLevelClearPanel(int stars = 3)
		{
			if (levelClearPanel != null)
			{
				levelClearPanel.SetActive(true);

				if (clearScoreText != null && GameManager.Instance != null)
					clearScoreText.text = $"{GameManager.Instance.GetScore():N0}";

				if (clearLevelText != null && GameManager.Instance != null)
					clearLevelText.text = $"Level {GameManager.Instance.CurrentLevel}";
			}
		}

		public void ShowPausePanel()
		{
			if (pausePanel != null)
				pausePanel.SetActive(true);
		}

		#endregion

		#region Floating Text

		public void ShowFloatingText(Vector3 position, string text, Color color)
		{
			if (floatingTextPrefab == null) return;

			Transform parent = floatingTextParent != null ? floatingTextParent : transform;
			GameObject floatingObj = Instantiate(floatingTextPrefab, position, Quaternion.identity, parent);

			var tmp = floatingObj.GetComponent<TextMeshProUGUI>();
			if (tmp != null)
			{
				tmp.text = text;
				tmp.color = color;
			}

			StartCoroutine(AnimateFloatingText(floatingObj));
		}

		private IEnumerator AnimateFloatingText(GameObject obj)
		{
			float duration = 1f;
			float elapsed = 0f;
			Vector3 startPos = obj.transform.position;

			var tmp = obj.GetComponent<TextMeshProUGUI>();
			Color startColor = tmp != null ? tmp.color : Color.white;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				obj.transform.position = startPos + Vector3.up * t * 1f;

				if (tmp != null)
				{
					Color newColor = startColor;
					newColor.a = 1f - t;
					tmp.color = newColor;
				}

				yield return null;
			}

			Destroy(obj);
		}

		public void ShowScorePopup(Vector3 position, int score)
		{
			ShowFloatingText(position, $"+{score}", Color.yellow);
		}

		public void ShowComboPopup(Vector3 position, int combo)
		{
			ShowFloatingText(position, $"x{combo} Combo!", Color.cyan);
		}

		#endregion
	}
}