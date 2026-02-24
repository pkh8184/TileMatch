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
		[SerializeField] private CanvasGroup mTransitionCanvasGroup;
		[SerializeField] private Image mFadeImage;

		[Header("Settings")]
		[SerializeField] private float mFadeDuration = 0.5F;
		[SerializeField] private Color mFadeColor = Color.white;

		private bool mIsTransitioning = false;

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
			SetAlpha(0F);
			Debug.Log("[TransitionManager] Initialized - Alpha set to 0");
		}

		/// <summary>
		/// Alpha 값 설정
		/// </summary>
		private void SetAlpha(float alpha)
		{
			if (mFadeImage != null)
			{
				Color c = mFadeColor;
				c.a = alpha;
				mFadeImage.color = c;
			}

			if (mTransitionCanvasGroup != null)
			{
				mTransitionCanvasGroup.blocksRaycasts = alpha > 0.5F;
			}
		}

		/// <summary>
		/// 씬 로드 (Fade 효과)
		/// </summary>
		public void LoadScene(string sceneName)
		{
			if (mIsTransitioning)
			{
				Debug.Log("[TransitionManager] Already transitioning, ignored");
				return;
			}

			Debug.Log($"[TransitionManager] LoadScene called: {sceneName}");
			StartCoroutine(LoadSceneWithFade(sceneName));
		}

		private IEnumerator LoadSceneWithFade(string sceneName)
		{
			mIsTransitioning = true;
			Debug.Log("[TransitionManager] Fade Out started");

			// 1. Fade Out (투명 → 불투명)
			yield return StartCoroutine(FadeOut());
			Debug.Log("[TransitionManager] Fade Out completed");

			// 2. 씬 로드
			Debug.Log($"[TransitionManager] Loading scene: {sceneName}");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
			asyncLoad.allowSceneActivation = false;

			while (asyncLoad.progress < 0.9F)
			{
				yield return null;
			}

			asyncLoad.allowSceneActivation = true;
			yield return new WaitUntil(() => asyncLoad.isDone);
			Debug.Log("[TransitionManager] Scene loaded");

			// 씬 초기화 대기
			yield return new WaitForSeconds(0.1F);

			// 3. Fade In (불투명 → 투명)
			Debug.Log("[TransitionManager] Fade In started");
			yield return StartCoroutine(FadeIn());
			Debug.Log("[TransitionManager] Fade In completed");

			mIsTransitioning = false;
		}

		/// <summary>
		/// Fade Out (화면이 흰색으로)
		/// </summary>
		private IEnumerator FadeOut()
		{
			float elapsed = 0F;

			while (elapsed < mFadeDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / mFadeDuration;
				float alpha = Mathf.Lerp(0F, 1F, EaseInOutQuad(t));
				SetAlpha(alpha);
				yield return null;
			}

			SetAlpha(1F);
		}

		/// <summary>
		/// Fade In (흰색에서 투명으로)
		/// </summary>
		private IEnumerator FadeIn()
		{
			float elapsed = 0F;

			while (elapsed < mFadeDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				float t = elapsed / mFadeDuration;
				float alpha = Mathf.Lerp(1F, 0F, EaseInOutQuad(t));
				SetAlpha(alpha);
				yield return null;
			}

			SetAlpha(0F);
		}

		private float EaseInOutQuad(float t)
		{
			return t < 0.5F ? 2F * t * t : 1F - Mathf.Pow(-2F * t + 2F, 2F) / 2F;
		}

		public bool IsTransitioning => mIsTransitioning;
	}
}
