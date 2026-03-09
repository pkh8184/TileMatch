using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 게임 클리어/오버 팝업 관리
	/// </summary>
	public class PopupManager : MonoBehaviour
	{
		public static PopupManager Instance { get; private set; }

		[Header("Game Clear Popup")]
		[SerializeField] private GameObject mClearPopup;
		[SerializeField] private TextMeshProUGUI mClearLevelText;
		[SerializeField] private TextMeshProUGUI mClearScoreText;
		[SerializeField] private GameObject[] mStars; // 별 3개
		[SerializeField] private Button mClearNextButton;
		[SerializeField] private Button mClearRetryButton;
		[SerializeField] private Button mClearHomeButton;

		[Header("Game Over Popup")]
		[SerializeField] private GameObject mGameOverPopup;
		[SerializeField] private TextMeshProUGUI mGameOverLevelText;
		[SerializeField] private Button mGameOverRetryButton;
		[SerializeField] private Button mGameOverHomeButton;

		[Header("Pause Popup")]
		[SerializeField] private GameObject mPausePopup;
		[SerializeField] private Button mPauseResumeButton;
		[SerializeField] private Button mPauseRetryButton;
		[SerializeField] private Button mPauseHomeButton;

		[Header("Animation Settings")]
		[SerializeField] private float mPopupAnimDuration = 0.3F;
		[SerializeField] private AnimationCurve mPopupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
		[SerializeField] private float mStarAnimDelay = 0.2F;

		[Header("Audio")]
		[SerializeField] private AudioClip mClearSound;
		[SerializeField] private AudioClip mGameOverSound;
		[SerializeField] private AudioClip mStarSound;
		[SerializeField] private AudioClip mButtonSound;

		private CanvasGroup mClearCanvasGroup;
		private CanvasGroup mGameOverCanvasGroup;
		private CanvasGroup mPauseCanvasGroup;

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
			if (mClearPopup != null)
			{
				mClearCanvasGroup = mClearPopup.GetComponent<CanvasGroup>();
				if (mClearCanvasGroup == null)
				{
					mClearCanvasGroup = mClearPopup.AddComponent<CanvasGroup>();
				}
			}

			if (mGameOverPopup != null)
			{
				mGameOverCanvasGroup = mGameOverPopup.GetComponent<CanvasGroup>();
				if (mGameOverCanvasGroup == null)
				{
					mGameOverCanvasGroup = mGameOverPopup.AddComponent<CanvasGroup>();
				}
			}

			if (mPausePopup != null)
			{
				mPauseCanvasGroup = mPausePopup.GetComponent<CanvasGroup>();
				if (mPauseCanvasGroup == null)
				{
					mPauseCanvasGroup = mPausePopup.AddComponent<CanvasGroup>();
				}
			}
		}

		private void SetupButtons()
		{
			// Clear Popup Buttons
			if (mClearNextButton != null)
			{
				mClearNextButton.onClick.AddListener(OnNextLevel);
			}
			if (mClearRetryButton != null)
			{
				mClearRetryButton.onClick.AddListener(OnRetry);
			}
			if (mClearHomeButton != null)
			{
				mClearHomeButton.onClick.AddListener(OnHome);
			}

			// Game Over Popup Buttons
			if (mGameOverRetryButton != null)
			{
				mGameOverRetryButton.onClick.AddListener(OnRetry);
			}
			if (mGameOverHomeButton != null)
			{
				mGameOverHomeButton.onClick.AddListener(OnHome);
			}

			// Pause Popup Buttons
			if (mPauseResumeButton != null)
			{
				mPauseResumeButton.onClick.AddListener(OnResume);
			}
			if (mPauseRetryButton != null)
			{
				mPauseRetryButton.onClick.AddListener(OnRetry);
			}
			if (mPauseHomeButton != null)
			{
				mPauseHomeButton.onClick.AddListener(OnHome);
			}
		}

		private void HideAllPopups()
		{
			if (mClearPopup != null)
			{
				mClearPopup.SetActive(false);
			}
			if (mGameOverPopup != null)
			{
				mGameOverPopup.SetActive(false);
			}
			if (mPausePopup != null)
			{
				mPausePopup.SetActive(false);
			}
		}

		#region Show Popups

		/// <summary>
		/// 게임 클리어 팝업 표시
		/// </summary>
		public void ShowClearPopup(int level, int score, int starCount)
		{
			if (mClearPopup == null) return;

			// 텍스트 설정
			if (mClearLevelText != null)
			{
				mClearLevelText.text = $"Level {level}";
			}
			if (mClearScoreText != null)
			{
				mClearScoreText.text = $"Score: {score:N0}";
			}

			// 별 초기화 (모두 숨김)
			if (mStars != null)
			{
				foreach (GameObject star in mStars)
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
			AudioManager.Instance?.PlaySFX(mClearSound);

			// 팝업 표시 애니메이션
			mClearPopup.SetActive(true);
			yield return StartCoroutine(AnimatePopupIn(mClearPopup.transform, mClearCanvasGroup));

			// 별 애니메이션
			if (mStars != null)
			{
				for (int i = 0; i < Mathf.Min(starCount, mStars.Length); i++)
				{
					yield return new WaitForSeconds(mStarAnimDelay);
					if (mStars[i] != null)
					{
						AudioManager.Instance?.PlaySFX(mStarSound);
						StartCoroutine(AnimateStarIn(mStars[i].transform));
					}
				}
			}
		}

		/// <summary>
		/// 게임 오버 팝업 표시
		/// </summary>
		public void ShowGameOverPopup(int level)
		{
			if (mGameOverPopup == null) return;

			if (mGameOverLevelText != null)
			{
				mGameOverLevelText.text = $"Level {level}";
			}

			AudioManager.Instance?.PlaySFX(mGameOverSound);
			StartCoroutine(ShowPopupCoroutine(mGameOverPopup, mGameOverCanvasGroup));
		}

		/// <summary>
		/// 일시정지 팝업 표시
		/// </summary>
		public void ShowPausePopup()
		{
			if (mPausePopup == null) return;

			Time.timeScale = 0F;
			StartCoroutine(ShowPopupCoroutine(mPausePopup, mPauseCanvasGroup));
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
			float elapsed = 0F;
			popup.localScale = Vector3.zero;
			if (canvasGroup != null)
			{
				canvasGroup.alpha = 0F;
			}

			while (elapsed < mPopupAnimDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = mPopupCurve.Evaluate(elapsed / mPopupAnimDuration);

				popup.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
				if (canvasGroup != null)
				{
					canvasGroup.alpha = Mathf.Lerp(0F, 1F, t);
				}

				yield return null;
			}

			popup.localScale = Vector3.one;
			if (canvasGroup != null)
			{
				canvasGroup.alpha = 1F;
			}
		}

		private IEnumerator AnimatePopupOut(Transform popup, CanvasGroup canvasGroup)
		{
			float elapsed = 0F;

			while (elapsed < mPopupAnimDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = 1F - mPopupCurve.Evaluate(elapsed / mPopupAnimDuration);

				popup.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
				if (canvasGroup != null)
				{
					canvasGroup.alpha = Mathf.Lerp(0F, 1F, t);
				}

				yield return null;
			}

			popup.localScale = Vector3.zero;
			popup.gameObject.SetActive(false);
		}

		private IEnumerator AnimateStarIn(Transform star)
		{
			float duration = 0.3F;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / duration;

				// Bounce effect
				float scale = Mathf.Sin(t * Mathf.PI * 0.5F) * 1.2F;
				if (t > 0.7F)
				{
					scale = Mathf.Lerp(1.2F, 1F, (t - 0.7F) / 0.3F);
				}

				star.localScale = Vector3.one * scale;
				yield return null;
			}

			star.localScale = Vector3.one;
		}

		#endregion

		#region Button Callbacks

		private void OnNextLevel()
		{
			AudioManager.Instance?.PlaySFX(mButtonSound);
			StartCoroutine(HideAndAction(mClearPopup, mClearCanvasGroup, () =>
			{
				GameManager.Instance?.NextLevel();
			}));
		}

		private void OnRetry()
		{
			AudioManager.Instance?.PlaySFX(mButtonSound);

			GameObject activePopup = null;
			CanvasGroup activeCanvasGroup = null;

			if (mClearPopup != null && mClearPopup.activeSelf)
			{
				activePopup = mClearPopup;
				activeCanvasGroup = mClearCanvasGroup;
			}
			else if (mGameOverPopup != null && mGameOverPopup.activeSelf)
			{
				activePopup = mGameOverPopup;
				activeCanvasGroup = mGameOverCanvasGroup;
			}
			else if (mPausePopup != null && mPausePopup.activeSelf)
			{
				activePopup = mPausePopup;
				activeCanvasGroup = mPauseCanvasGroup;
			}

			if (activePopup != null)
			{
				StartCoroutine(HideAndAction(activePopup, activeCanvasGroup, () =>
				{
					Time.timeScale = 1F;
					GameManager.Instance?.RestartLevel();
				}));
			}
		}

		private void OnHome()
		{
			AudioManager.Instance?.PlaySFX(mButtonSound);

			GameObject activePopup = null;
			CanvasGroup activeCanvasGroup = null;

			if (mClearPopup != null && mClearPopup.activeSelf)
			{
				activePopup = mClearPopup;
				activeCanvasGroup = mClearCanvasGroup;
			}
			else if (mGameOverPopup != null && mGameOverPopup.activeSelf)
			{
				activePopup = mGameOverPopup;
				activeCanvasGroup = mGameOverCanvasGroup;
			}
			else if (mPausePopup != null && mPausePopup.activeSelf)
			{
				activePopup = mPausePopup;
				activeCanvasGroup = mPauseCanvasGroup;
			}

			if (activePopup != null)
			{
				StartCoroutine(HideAndAction(activePopup, activeCanvasGroup, () =>
				{
					Time.timeScale = 1F;
					GameManager.Instance?.GoToMainMenu();
				}));
			}
		}

		private void OnResume()
		{
			AudioManager.Instance?.PlaySFX(mButtonSound);
			StartCoroutine(HideAndAction(mPausePopup, mPauseCanvasGroup, () =>
			{
				Time.timeScale = 1F;
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
