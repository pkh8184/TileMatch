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
		[SerializeField] private TextMeshProUGUI mScoreText;
		[SerializeField] private TextMeshProUGUI mLevelText;
		[SerializeField] private TextMeshProUGUI mGoldText;
		[SerializeField] private TextMeshProUGUI mComboText;
		[SerializeField] private TextMeshProUGUI mTimerText;
		[SerializeField] private Slider mTimerSlider;

		[Header("Items")]
		[SerializeField] private TextMeshProUGUI mStrikeCountText;
		[SerializeField] private TextMeshProUGUI mBlackHoleCountText;
		[SerializeField] private TextMeshProUGUI mBoomCountText;
		[SerializeField] private Button mStrikeButton;
		[SerializeField] private Button mBlackHoleButton;
		[SerializeField] private Button mBoomButton;
		[SerializeField] private Button mPauseButton;

		[Header("Panels")]
		[SerializeField] private GameObject mLevelClearPanel;
		[SerializeField] private GameObject mPausePanel;

		[Header("Level Clear Panel")]
		[SerializeField] private TextMeshProUGUI mClearLevelText;
		[SerializeField] private TextMeshProUGUI mClearScoreText;
		[SerializeField] private Button mNextLevelButton;
		[SerializeField] private Button mClearRestartButton;
		[SerializeField] private Button mClearMainMenuButton;

		[Header("Pause Panel")]
		[SerializeField] private Button mResumeButton;
		[SerializeField] private Button mPauseRestartButton;
		[SerializeField] private Button mPauseMainMenuButton;

		[Header("Floating Text")]
		[SerializeField] private GameObject mFloatingTextPrefab;
		[SerializeField] private Transform mFloatingTextParent;

		[Header("Animation")]
		[SerializeField] private Animator mComboAnimator;

		private int mStrikeCount;
		private int mBlackHoleCount;
		private int mBoomCount;

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
			if (mStrikeButton != null)
				mStrikeButton.onClick.AddListener(OnStrikeClick);

			if (mBlackHoleButton != null)
				mBlackHoleButton.onClick.AddListener(OnBlackHoleClick);

			if (mBoomButton != null)
				mBoomButton.onClick.AddListener(OnBoomClick);

			if (mPauseButton != null)
				mPauseButton.onClick.AddListener(OnPauseClick);

			if (mNextLevelButton != null)
				mNextLevelButton.onClick.AddListener(OnNextLevelClick);

			if (mClearRestartButton != null)
				mClearRestartButton.onClick.AddListener(OnRestartClick);

			if (mClearMainMenuButton != null)
				mClearMainMenuButton.onClick.AddListener(OnMainMenuClick);

			if (mResumeButton != null)
				mResumeButton.onClick.AddListener(OnResumeClick);

			if (mPauseRestartButton != null)
				mPauseRestartButton.onClick.AddListener(OnRestartClick);

			if (mPauseMainMenuButton != null)
				mPauseMainMenuButton.onClick.AddListener(OnMainMenuClick);
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
			Time.timeScale = 1F;
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
			Time.timeScale = 1F;
			GameManager.Instance?.GoToMainMenu();
		}

		#endregion

		#region Update UI

		public void UpdateScore(int score)
		{
			if (mScoreText != null)
			{
				mScoreText.text = $"{score:N0}";
				StartCoroutine(ScorePunchAnimation());
			}
		}

		private IEnumerator ScorePunchAnimation()
		{
			if (mScoreText == null) yield break;

			Vector3 originalScale = Vector3.one;
			Vector3 punchScale = Vector3.one * 1.2F;

			mScoreText.transform.localScale = punchScale;

			float duration = 0.15F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				mScoreText.transform.localScale = Vector3.Lerp(punchScale, originalScale, elapsed / duration);
				yield return null;
			}

			mScoreText.transform.localScale = originalScale;
		}

		public void UpdateLevel(int level)
		{
			if (mLevelText != null)
				mLevelText.text = $"Level {level}";
		}

		public void UpdateGold(int gold)
		{
			if (mGoldText != null)
				mGoldText.text = $"{gold:N0}";
		}

		public void UpdateCombo(int combo)
		{
			if (mComboText != null)
			{
				if (combo > 1)
				{
					mComboText.gameObject.SetActive(true);
					mComboText.text = $"x{combo}";

					if (mComboAnimator != null)
						mComboAnimator.SetTrigger("Pulse");

					StartCoroutine(ComboPunchAnimation());
				}
				else
				{
					mComboText.gameObject.SetActive(false);
				}
			}
		}

		private IEnumerator ComboPunchAnimation()
		{
			if (mComboText == null) yield break;

			Vector3 punchScale = Vector3.one * 1.5F;
			mComboText.transform.localScale = punchScale;

			float duration = 0.2F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				mComboText.transform.localScale = Vector3.Lerp(punchScale, Vector3.one, elapsed / duration);
				yield return null;
			}

			mComboText.transform.localScale = Vector3.one;
		}

		public void UpdateTimer(float timeRemaining)
		{
			if (mTimerText != null)
			{
				int minutes = Mathf.FloorToInt(timeRemaining / 60);
				int seconds = Mathf.FloorToInt(timeRemaining % 60);
				mTimerText.text = $"{minutes:D2}:{seconds:D2}";
				mTimerText.color = timeRemaining < 10 ? Color.red : Color.white;
			}
		}

		public void UpdateItems(int strike, int blackHole, int boom)
		{
			mStrikeCount = strike;
			mBlackHoleCount = blackHole;
			mBoomCount = boom;

			if (mStrikeCountText != null)
				mStrikeCountText.text = strike.ToString();

			if (mBlackHoleCountText != null)
				mBlackHoleCountText.text = blackHole.ToString();

			if (mBoomCountText != null)
				mBoomCountText.text = boom.ToString();

			UpdateItemButtonStates();
		}

		public void UpdateItemButtonStates()
		{
			bool bCanUse = true;

			if (GameManager.Instance != null)
			{
				GameManager.EGameState state = GameManager.Instance.CurrentState;
				bCanUse = (state == GameManager.EGameState.Playing || state == GameManager.EGameState.Loading);
			}

			if (mStrikeButton != null)
				mStrikeButton.interactable = bCanUse && mStrikeCount > 0;

			if (mBlackHoleButton != null)
				mBlackHoleButton.interactable = bCanUse && mBlackHoleCount > 0;

			if (mBoomButton != null)
				mBoomButton.interactable = bCanUse && mBoomCount > 0;
		}

		public void DisableItemButtons()
		{
			if (mStrikeButton != null)
				mStrikeButton.interactable = false;

			if (mBlackHoleButton != null)
				mBlackHoleButton.interactable = false;

			if (mBoomButton != null)
				mBoomButton.interactable = false;
		}

		public void UpdateProgress(int matched, int total)
		{
			// 진행률 표시 (필요시 구현)
		}

		#endregion

		#region Panels

		public void HideAllPanels()
		{
			if (mLevelClearPanel != null) mLevelClearPanel.SetActive(false);
			if (mPausePanel != null) mPausePanel.SetActive(false);
		}

		public void ShowLevelClearPanel(int stars = 3)
		{
			if (mLevelClearPanel != null)
			{
				mLevelClearPanel.SetActive(true);

				if (mClearScoreText != null && GameManager.Instance != null)
					mClearScoreText.text = $"{GameManager.Instance.GetScore():N0}";

				if (mClearLevelText != null && GameManager.Instance != null)
					mClearLevelText.text = $"Level {GameManager.Instance.CurrentLevel}";
			}
		}

		public void ShowPausePanel()
		{
			if (mPausePanel != null)
				mPausePanel.SetActive(true);
		}

		#endregion

		#region Floating Text

		public void ShowFloatingText(Vector3 position, string text, Color color)
		{
			if (mFloatingTextPrefab == null) return;

			Transform parent = mFloatingTextParent != null ? mFloatingTextParent : transform;
			GameObject floatingObj = Instantiate(mFloatingTextPrefab, position, Quaternion.identity, parent);

			TextMeshProUGUI tmp = floatingObj.GetComponent<TextMeshProUGUI>();
			if (tmp != null)
			{
				tmp.text = text;
				tmp.color = color;
			}

			StartCoroutine(AnimateFloatingText(floatingObj));
		}

		private IEnumerator AnimateFloatingText(GameObject obj)
		{
			float duration = 1F;
			float elapsed = 0F;
			Vector3 startPos = obj.transform.position;

			TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
			Color startColor = tmp != null ? tmp.color : Color.white;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				obj.transform.position = startPos + Vector3.up * t * 1F;

				if (tmp != null)
				{
					Color newColor = startColor;
					newColor.a = 1F - t;
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
