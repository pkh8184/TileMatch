using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TrumpTile.Audio;
using TrumpTile.Core;  // GameManager

namespace TrumpTile.UI
{
	/// <summary>
	/// 게임 클리어/오버 팝업 관리
	/// </summary>
	public class PopupManager : MonoBehaviour
	{
		public static PopupManager Instance { get; private set; }

		[Header("Game Clear Popup")]
		[SerializeField] private GameObject clearPopup;
		[SerializeField] private TextMeshProUGUI clearLevelText;
		[SerializeField] private TextMeshProUGUI clearScoreText;
		[SerializeField] private GameObject[] stars; // 별 3개
		[SerializeField] private Button clearNextButton;
		[SerializeField] private Button clearRetryButton;
		[SerializeField] private Button clearHomeButton;

		[Header("Game Over Popup")]
		[SerializeField] private GameObject gameOverPopup;
		[SerializeField] private TextMeshProUGUI gameOverLevelText;
		[SerializeField] private Button gameOverRetryButton;
		[SerializeField] private Button gameOverHomeButton;

		[Header("Pause Popup")]
		[SerializeField] private GameObject pausePopup;
		[SerializeField] private Button pauseResumeButton;
		[SerializeField] private Button pauseRetryButton;
		[SerializeField] private Button pauseHomeButton;

		[Header("Animation Settings")]
		[SerializeField] private float popupAnimDuration = 0.3f;
		[SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField] private float starAnimDelay = 0.2f;

		[Header("Audio")]
		[SerializeField] private AudioClip clearSound;
		[SerializeField] private AudioClip gameOverSound;
		[SerializeField] private AudioClip starSound;
		[SerializeField] private AudioClip buttonSound;

		private CanvasGroup clearCanvasGroup;
		private CanvasGroup gameOverCanvasGroup;
		private CanvasGroup pauseCanvasGroup;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			SetupCanvasGroups();
			SetupButtons();
			HideAllPopups();
		}

		private void SetupCanvasGroups()
		{
			if (clearPopup != null)
			{
				clearCanvasGroup = clearPopup.GetComponent<CanvasGroup>();
				if (clearCanvasGroup == null)
					clearCanvasGroup = clearPopup.AddComponent<CanvasGroup>();
			}

			if (gameOverPopup != null)
			{
				gameOverCanvasGroup = gameOverPopup.GetComponent<CanvasGroup>();
				if (gameOverCanvasGroup == null)
					gameOverCanvasGroup = gameOverPopup.AddComponent<CanvasGroup>();
			}

			if (pausePopup != null)
			{
				pauseCanvasGroup = pausePopup.GetComponent<CanvasGroup>();
				if (pauseCanvasGroup == null)
					pauseCanvasGroup = pausePopup.AddComponent<CanvasGroup>();
			}
		}

		private void SetupButtons()
		{
			// Clear Popup Buttons
			if (clearNextButton != null)
				clearNextButton.onClick.AddListener(OnNextLevel);
			if (clearRetryButton != null)
				clearRetryButton.onClick.AddListener(OnRetry);
			if (clearHomeButton != null)
				clearHomeButton.onClick.AddListener(OnHome);

			// Game Over Popup Buttons
			if (gameOverRetryButton != null)
				gameOverRetryButton.onClick.AddListener(OnRetry);
			if (gameOverHomeButton != null)
				gameOverHomeButton.onClick.AddListener(OnHome);

			// Pause Popup Buttons
			if (pauseResumeButton != null)
				pauseResumeButton.onClick.AddListener(OnResume);
			if (pauseRetryButton != null)
				pauseRetryButton.onClick.AddListener(OnRetry);
			if (pauseHomeButton != null)
				pauseHomeButton.onClick.AddListener(OnHome);
		}

		private void HideAllPopups()
		{
			if (clearPopup != null) clearPopup.SetActive(false);
			if (gameOverPopup != null) gameOverPopup.SetActive(false);
			if (pausePopup != null) pausePopup.SetActive(false);
		}

		#region Show Popups

		/// <summary>
		/// 게임 클리어 팝업 표시
		/// </summary>
		public void ShowClearPopup(int level, int score, int starCount)
		{
			if (clearPopup == null) return;

			// 텍스트 설정
			if (clearLevelText != null)
				clearLevelText.text = $"Level {level}";
			if (clearScoreText != null)
				clearScoreText.text = $"Score: {score:N0}";

			// 별 초기화 (모두 숨김)
			if (stars != null)
			{
				foreach (var star in stars)
				{
					if (star != null)
					{
						star.transform.localScale = Vector3.zero;
						star.SetActive(true);
					}
				}
			}

			StartCoroutine(ShowClearPopupCoroutine(starCount));
		}

		private IEnumerator ShowClearPopupCoroutine(int starCount)
		{
			// 사운드 재생
			AudioManager.Instance?.PlaySFX(clearSound);

			// 팝업 표시 애니메이션
			clearPopup.SetActive(true);
			yield return StartCoroutine(AnimatePopupIn(clearPopup.transform, clearCanvasGroup));

			// 별 애니메이션
			if (stars != null)
			{
				for (int i = 0; i < Mathf.Min(starCount, stars.Length); i++)
				{
					yield return new WaitForSeconds(starAnimDelay);
					if (stars[i] != null)
					{
						AudioManager.Instance?.PlaySFX(starSound);
						StartCoroutine(AnimateStarIn(stars[i].transform));
					}
				}
			}
		}

		/// <summary>
		/// 게임 오버 팝업 표시
		/// </summary>
		public void ShowGameOverPopup(int level)
		{
			if (gameOverPopup == null) return;

			if (gameOverLevelText != null)
				gameOverLevelText.text = $"Level {level}";

			AudioManager.Instance?.PlaySFX(gameOverSound);
			StartCoroutine(ShowPopupCoroutine(gameOverPopup, gameOverCanvasGroup));
		}

		/// <summary>
		/// 일시정지 팝업 표시
		/// </summary>
		public void ShowPausePopup()
		{
			if (pausePopup == null) return;

			Time.timeScale = 0f;
			StartCoroutine(ShowPopupCoroutine(pausePopup, pauseCanvasGroup));
		}

		private IEnumerator ShowPopupCoroutine(GameObject popup, CanvasGroup canvasGroup)
		{
			popup.SetActive(true);
			yield return StartCoroutine(AnimatePopupIn(popup.transform, canvasGroup));
		}

		#endregion

		#region Animations

		private IEnumerator AnimatePopupIn(Transform popup, CanvasGroup canvasGroup)
		{
			float elapsed = 0f;
			popup.localScale = Vector3.zero;
			if (canvasGroup != null) canvasGroup.alpha = 0f;

			while (elapsed < popupAnimDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = popupCurve.Evaluate(elapsed / popupAnimDuration);

				popup.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
				if (canvasGroup != null)
					canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

				yield return null;
			}

			popup.localScale = Vector3.one;
			if (canvasGroup != null) canvasGroup.alpha = 1f;
		}

		private IEnumerator AnimatePopupOut(Transform popup, CanvasGroup canvasGroup)
		{
			float elapsed = 0f;

			while (elapsed < popupAnimDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = 1f - popupCurve.Evaluate(elapsed / popupAnimDuration);

				popup.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
				if (canvasGroup != null)
					canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

				yield return null;
			}

			popup.localScale = Vector3.zero;
			popup.gameObject.SetActive(false);
		}

		private IEnumerator AnimateStarIn(Transform star)
		{
			float duration = 0.3f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / duration;

				// Bounce effect
				float scale = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.2f;
				if (t > 0.7f)
					scale = Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);

				star.localScale = Vector3.one * scale;
				yield return null;
			}

			star.localScale = Vector3.one;
		}

		#endregion

		#region Button Callbacks

		private void OnNextLevel()
		{
			AudioManager.Instance?.PlaySFX(buttonSound);
			StartCoroutine(HideAndAction(clearPopup, clearCanvasGroup, () =>
			{
				GameManager.Instance?.NextLevel();
			}));
		}

		private void OnRetry()
		{
			AudioManager.Instance?.PlaySFX(buttonSound);

			GameObject activePopup = null;
			CanvasGroup activeCanvasGroup = null;

			if (clearPopup != null && clearPopup.activeSelf)
			{
				activePopup = clearPopup;
				activeCanvasGroup = clearCanvasGroup;
			}
			else if (gameOverPopup != null && gameOverPopup.activeSelf)
			{
				activePopup = gameOverPopup;
				activeCanvasGroup = gameOverCanvasGroup;
			}
			else if (pausePopup != null && pausePopup.activeSelf)
			{
				activePopup = pausePopup;
				activeCanvasGroup = pauseCanvasGroup;
			}

			if (activePopup != null)
			{
				StartCoroutine(HideAndAction(activePopup, activeCanvasGroup, () =>
				{
					Time.timeScale = 1f;
					GameManager.Instance?.RestartLevel();
				}));
			}
		}

		private void OnHome()
		{
			AudioManager.Instance?.PlaySFX(buttonSound);

			GameObject activePopup = null;
			CanvasGroup activeCanvasGroup = null;

			if (clearPopup != null && clearPopup.activeSelf)
			{
				activePopup = clearPopup;
				activeCanvasGroup = clearCanvasGroup;
			}
			else if (gameOverPopup != null && gameOverPopup.activeSelf)
			{
				activePopup = gameOverPopup;
				activeCanvasGroup = gameOverCanvasGroup;
			}
			else if (pausePopup != null && pausePopup.activeSelf)
			{
				activePopup = pausePopup;
				activeCanvasGroup = pauseCanvasGroup;
			}

			if (activePopup != null)
			{
				StartCoroutine(HideAndAction(activePopup, activeCanvasGroup, () =>
				{
					Time.timeScale = 1f;
					GameManager.Instance?.GoToMainMenu();
				}));
			}
		}

		private void OnResume()
		{
			AudioManager.Instance?.PlaySFX(buttonSound);
			StartCoroutine(HideAndAction(pausePopup, pauseCanvasGroup, () =>
			{
				Time.timeScale = 1f;
			}));
		}

		private IEnumerator HideAndAction(GameObject popup, CanvasGroup canvasGroup, System.Action action)
		{
			yield return StartCoroutine(AnimatePopupOut(popup.transform, canvasGroup));
			action?.Invoke();
		}

		#endregion
	}
}