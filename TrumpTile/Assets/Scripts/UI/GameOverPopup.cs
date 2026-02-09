using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using DG.Tweening;

namespace TrumpTile.UI
{
	/// <summary>
	/// 패배 팝업 UI (재구성)
	/// 
	/// [동작]
	/// - 배경: 처음부터 어둡게 (애니메이션 없음)
	/// - UI: 바운스 애니메이션으로 등장
	/// - 2초간 터치 무시
	/// - Free = 부활, Tap to Restart = 완전 재시작
	/// </summary>
	public class GameOverPopup : MonoBehaviour
	{
		[Header("UI References")]
		[SerializeField] private GameObject popupPanel;
		[SerializeField] private Image dimBackground;
		[SerializeField] private RectTransform gaugeArea;
		[SerializeField] private Image gaugeFill;
		[SerializeField] private TextMeshProUGUI countText;
		[SerializeField] private TextMeshProUGUI messageText;
		[SerializeField] private Button continueButton;
		[SerializeField] private RectTransform continueButtonRect;
		[SerializeField] private Button restartButton;
		[SerializeField] private RectTransform restartButtonRect;

		[Header("Animation Settings")]
		[SerializeField] private float dimAlpha = 0.95f;
		[SerializeField] private float gaugePopDuration = 0.4f;
		[SerializeField] private float gaugePopScale = 1.2f;
		[SerializeField] private float buttonPopDuration = 0.3f;
		[SerializeField] private float buttonPopScale = 1.3f;
		[SerializeField] private float buttonDelay = 0.1f;

		[Header("Countdown Settings")]
		[SerializeField] private float countdownTime = 10f;
		[SerializeField] private float inputBlockDuration = 2f;
		[SerializeField] private int maxReviveCount = 3;

		[Header("Messages")]
		[SerializeField]
		private string[] messages = new string[]
		{
			"Don't give Up!",
			"Almost there!",
			"Keep going!"
		};

		[Header("Colors")]
		[SerializeField] private Color gaugeFullColor = new Color(0.3f, 0.85f, 0.3f);
		[SerializeField] private Color gaugeLowColor = new Color(0.85f, 0.3f, 0.3f);

		// Events
		public event Action OnContinue;
		public event Action OnRestart;

		// State
		private bool isPopupActive = false;
		private bool isInputBlocked = true;
		private int currentReviveCount = 0;
		private Coroutine countdownCoroutine;

		// 원본 스케일
		private Vector3 gaugeOriginalScale;
		private Vector3 continueOriginalScale;
		private Vector3 restartOriginalScale;

		public bool IsActive => isPopupActive;

		private void Awake()
		{
			SaveOriginalValues();
			SetupButtons();

			if (popupPanel != null)
				popupPanel.SetActive(false);
		}

		private void SaveOriginalValues()
		{
			gaugeOriginalScale = gaugeArea != null ? gaugeArea.localScale : Vector3.one;
			continueOriginalScale = continueButtonRect != null ? continueButtonRect.localScale : Vector3.one;
			restartOriginalScale = restartButtonRect != null ? restartButtonRect.localScale : Vector3.one;
		}

		private void SetupButtons()
		{
			if (continueButton != null)
				continueButton.onClick.AddListener(OnContinueClick);

			if (restartButton != null)
				restartButton.onClick.AddListener(OnRestartClick);
		}

		private void OnDestroy()
		{
			DOTween.Kill(this);

			if (continueButton != null)
				continueButton.onClick.RemoveListener(OnContinueClick);

			if (restartButton != null)
				restartButton.onClick.RemoveListener(OnRestartClick);
		}

		public void ResetForNewGame()
		{
			currentReviveCount = 0;
		}

		/// <summary>
		/// 팝업 표시
		/// </summary>
		public void Show()
		{
			if (isPopupActive) return;

			Debug.Log("[GameOverPopup] Show");

			isPopupActive = true;
			isInputBlocked = true;

			// 1. 배경 즉시 어둡게 (애니메이션 없음)
			SetBackgroundDark();

			// 2. UI 초기 상태 (스케일 0)
			SetUIInitialState();

			// 3. 팝업 활성화
			if (popupPanel != null)
				popupPanel.SetActive(true);

			// 4. UI 애니메이션 시작
			PlayUIAnimation();

			// 5. 입력 차단 해제 타이머
			StartCoroutine(UnblockInputAfterDelay());

			// 6. 카운트다운 시작
			StartCountdown();
		}

		/// <summary>
		/// 배경 즉시 어둡게
		/// </summary>
		private void SetBackgroundDark()
		{
			if (dimBackground != null)
			{
				Color c = dimBackground.color;
				c.a = dimAlpha;
				dimBackground.color = c;
			}
		}

		/// <summary>
		/// UI 초기 상태 설정
		/// </summary>
		private void SetUIInitialState()
		{
			// 게이지 스케일 0
			if (gaugeArea != null)
				gaugeArea.localScale = Vector3.zero;

			// 버튼들 스케일 0
			if (continueButtonRect != null)
				continueButtonRect.localScale = Vector3.zero;

			if (restartButtonRect != null)
				restartButtonRect.localScale = Vector3.zero;

			// 게이지 채움 초기화
			if (gaugeFill != null)
			{
				gaugeFill.fillAmount = 1f;
				gaugeFill.color = gaugeFullColor;
			}

			// 카운트 텍스트
			if (countText != null)
				countText.text = Mathf.CeilToInt(countdownTime).ToString();

			// 메시지 설정
			if (messageText != null && messages.Length > 0)
				messageText.text = messages[UnityEngine.Random.Range(0, messages.Length)];

			// 버튼 상태
			bool canRevive = currentReviveCount < maxReviveCount;
			if (continueButton != null)
			{
				continueButton.gameObject.SetActive(canRevive);
				continueButton.interactable = false;
			}

			if (restartButton != null)
				restartButton.interactable = false;
		}

		/// <summary>
		/// UI 애니메이션
		/// </summary>
		private void PlayUIAnimation()
		{
			Sequence seq = DOTween.Sequence();

			// 게이지 바운스
			if (gaugeArea != null)
			{
				seq.Append(
					gaugeArea.DOScale(gaugeOriginalScale * gaugePopScale, gaugePopDuration * 0.6f)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					gaugeArea.DOScale(gaugeOriginalScale, gaugePopDuration * 0.4f)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Continue 버튼 바운스
			if (continueButtonRect != null && continueButton.gameObject.activeSelf)
			{
				seq.Append(
					continueButtonRect.DOScale(continueOriginalScale * buttonPopScale, buttonPopDuration * 0.5f)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					continueButtonRect.DOScale(continueOriginalScale, buttonPopDuration * 0.5f)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Restart 버튼 바운스
			if (restartButtonRect != null)
			{
				seq.AppendInterval(buttonDelay);
				seq.Append(
					restartButtonRect.DOScale(restartOriginalScale * buttonPopScale, buttonPopDuration * 0.5f)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					restartButtonRect.DOScale(restartOriginalScale, buttonPopDuration * 0.5f)
						.SetEase(Ease.InOutQuad)
				);
			}
		}

		/// <summary>
		/// 카운트다운 시작
		/// </summary>
		private void StartCountdown()
		{
			if (countdownCoroutine != null)
				StopCoroutine(countdownCoroutine);

			countdownCoroutine = StartCoroutine(CountdownCoroutine());
		}

		/// <summary>
		/// 팝업 숨기기
		/// </summary>
		public void Hide()
		{
			if (!isPopupActive) return;

			Debug.Log("[GameOverPopup] Hide");

			isPopupActive = false;
			isInputBlocked = true;

			DOTween.Kill(this);

			if (countdownCoroutine != null)
			{
				StopCoroutine(countdownCoroutine);
				countdownCoroutine = null;
			}

			if (popupPanel != null)
				popupPanel.SetActive(false);

			ResetUI();
		}

		/// <summary>
		/// UI 원본 상태 복원
		/// </summary>
		private void ResetUI()
		{
			if (gaugeArea != null)
				gaugeArea.localScale = gaugeOriginalScale;

			if (continueButtonRect != null)
				continueButtonRect.localScale = continueOriginalScale;

			if (restartButtonRect != null)
				restartButtonRect.localScale = restartOriginalScale;
		}

		/// <summary>
		/// 입력 차단 해제
		/// </summary>
		private IEnumerator UnblockInputAfterDelay()
		{
			yield return new WaitForSeconds(inputBlockDuration);

			isInputBlocked = false;

			// 버튼 활성화 + 펄스 효과
			if (continueButton != null && continueButton.gameObject.activeSelf)
			{
				continueButton.interactable = true;
				continueButtonRect?.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
			}

			if (restartButton != null)
			{
				restartButton.interactable = true;
				restartButtonRect?.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
			}

			Debug.Log("[GameOverPopup] Input unblocked");
		}

		/// <summary>
		/// 카운트다운
		/// </summary>
		private IEnumerator CountdownCoroutine()
		{
			float remainingTime = countdownTime;
			int lastSecond = -1;

			while (remainingTime > 0f)
			{
				remainingTime -= Time.deltaTime;
				float progress = remainingTime / countdownTime;

				// 게이지 업데이트
				if (gaugeFill != null)
				{
					gaugeFill.fillAmount = progress;
					gaugeFill.color = Color.Lerp(gaugeLowColor, gaugeFullColor, progress);
				}

				// 카운트 텍스트
				int currentSecond = Mathf.CeilToInt(remainingTime);
				if (countText != null && currentSecond != lastSecond)
				{
					countText.text = currentSecond.ToString();
					lastSecond = currentSecond;

					// 마지막 3초 펄스
					if (currentSecond <= 3 && currentSecond > 0)
					{
						countText.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
					}
				}

				yield return null;
			}

			// 타이머 종료 - 자동 재시작
			Debug.Log("[GameOverPopup] Countdown finished");
			ExecuteRestart();
		}

		private void OnContinueClick()
		{
			if (isInputBlocked) return;

			Debug.Log("[GameOverPopup] Continue clicked");
			currentReviveCount++;

			Hide();
			OnContinue?.Invoke();
		}

		private void OnRestartClick()
		{
			if (isInputBlocked) return;

			Debug.Log("[GameOverPopup] Restart clicked");
			ExecuteRestart();
		}

		private void ExecuteRestart()
		{
			Hide();
			OnRestart?.Invoke();
		}

		public int GetRemainingRevives()
		{
			return maxReviveCount - currentReviveCount;
		}
	}
}