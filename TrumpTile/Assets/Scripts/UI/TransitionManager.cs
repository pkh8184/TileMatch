using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace TrumpTile.UI
{
	/// <summary>
	/// 씬 전환 관리 (Fade 효과)
	/// DontDestroyOnLoad로 유지
	/// </summary>
	public class TransitionManager : MonoBehaviour
	{
		public static TransitionManager Instance { get; private set; }

		[Header("Transition UI")]
		[SerializeField] private CanvasGroup transitionCanvasGroup;
		[SerializeField] private Image fadeImage;

		[Header("Settings")]
		[SerializeField] private float fadeDuration = 0.5f;
		[SerializeField] private Color fadeColor = Color.white;

		private bool isTransitioning = false;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
				return;
			}
		}

		private void Start()
		{
			// 시작 시 투명하게 설정
			SetAlpha(0f);
			Debug.Log("[TransitionManager] Initialized - Alpha set to 0");
		}

		/// <summary>
		/// Alpha 값 설정
		/// </summary>
		private void SetAlpha(float alpha)
		{
			if (fadeImage != null)
			{
				Color c = fadeColor;
				c.a = alpha;
				fadeImage.color = c;
			}

			if (transitionCanvasGroup != null)
			{
				transitionCanvasGroup.blocksRaycasts = alpha > 0.5f;
			}
		}

		/// <summary>
		/// 씬 로드 (Fade 효과)
		/// </summary>
		public void LoadScene(string sceneName)
		{
			if (isTransitioning)
			{
				Debug.Log("[TransitionManager] Already transitioning, ignored");
				return;
			}

			Debug.Log($"[TransitionManager] LoadScene called: {sceneName}");
			StartCoroutine(LoadSceneWithFade(sceneName));
		}

		private IEnumerator LoadSceneWithFade(string sceneName)
		{
			isTransitioning = true;
			Debug.Log("[TransitionManager] Fade Out started");

			// 1. Fade Out (투명 → 불투명)
			yield return StartCoroutine(FadeOut());
			Debug.Log("[TransitionManager] Fade Out completed");

			// 2. 씬 로드
			Debug.Log($"[TransitionManager] Loading scene: {sceneName}");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
			asyncLoad.allowSceneActivation = false;

			while (asyncLoad.progress < 0.9f)
			{
				yield return null;
			}

			asyncLoad.allowSceneActivation = true;
			yield return new WaitUntil(() => asyncLoad.isDone);
			Debug.Log("[TransitionManager] Scene loaded");

			// 씬 초기화 대기
			yield return new WaitForSeconds(0.1f);

			// 3. Fade In (불투명 → 투명)
			Debug.Log("[TransitionManager] Fade In started");
			yield return StartCoroutine(FadeIn());
			Debug.Log("[TransitionManager] Fade In completed");

			isTransitioning = false;
		}

		/// <summary>
		/// Fade Out (화면이 흰색으로)
		/// </summary>
		private IEnumerator FadeOut()
		{
			float elapsed = 0f;

			while (elapsed < fadeDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / fadeDuration;
				float alpha = Mathf.Lerp(0f, 1f, EaseInOutQuad(t));
				SetAlpha(alpha);
				yield return null;
			}

			SetAlpha(1f);
		}

		/// <summary>
		/// Fade In (흰색에서 투명으로)
		/// </summary>
		private IEnumerator FadeIn()
		{
			float elapsed = 0f;

			while (elapsed < fadeDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / fadeDuration;
				float alpha = Mathf.Lerp(1f, 0f, EaseInOutQuad(t));
				SetAlpha(alpha);
				yield return null;
			}

			SetAlpha(0f);
		}

		private float EaseInOutQuad(float t)
		{
			return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
		}

		public bool IsTransitioning => isTransitioning;
	}
}