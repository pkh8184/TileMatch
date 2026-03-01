using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using DG.Tweening;

namespace TrumpTile.GameMain.UI
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
		[SerializeField] private float dimAlpha = 0.95F;
		[SerializeField] private float gaugePopDuration = 0.4F;
		[SerializeField] private float gaugePopScale = 1.2F;
		[SerializeField] private float buttonPopDuration = 0.3F;
		[SerializeField] private float buttonPopScale = 1.3F;
		[SerializeField] private float buttonDelay = 0.1F;

		[Header("Countdown Settings")]
		[SerializeField] private float countdownTime = 10F;
		[SerializeField] private float inputBlockDuration = 2F;
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
		[SerializeField] private Color gaugeFullColor = new Color(0.3F, 0.85F, 0.3F);
		[SerializeField] private Color gaugeLowColor = new Color(0.85F, 0.3F, 0.3F);

		// Events
		public event Action OnContinue;
		public event Action OnRestart;

		// State
		private bool mIsPopupActive = false;
		private bool mIsInputBlocked = true;
		private int mCurrentReviveCount = 0;
		private Coroutine mCountdownCoroutine;

		// 원본 스케일
		private Vector3 mGaugeOriginalScale;
		private Vector3 mContinueOriginalScale;
		private Vector3 mRestartOriginalScale;

		public bool IsActive => mIsPopupActive;

		private void Awake()
		{
			SaveOriginalValues();
			SetupButtons();

			if (popupPanel != null)
				popupPanel.SetActive(false);
		}

		private void SaveOriginalValues()
		{
			mGaugeOriginalScale = gaugeArea != null ? gaugeArea.localScale : Vector3.one;
			mContinueOriginalScale = continueButtonRect != null ? continueButtonRect.localScale : Vector3.one;
			mRestartOriginalScale = restartButtonRect != null ? restartButtonRect.localScale : Vector3.one;
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
			mCurrentReviveCount = 0;
		}

		/// <summary>
		/// 팝업 표시
		/// </summary>
		public void Show()
		{
			if (mIsPopupActive) return;

			Debug.Log("[GameOverPopup] Show");

			mIsPopupActive = true;
			mIsInputBlocked = true;

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
				gaugeFill.fillAmount = 1F;
				gaugeFill.color = gaugeFullColor;
			}

			// 카운트 텍스트
			if (countText != null)
				countText.text = Mathf.CeilToInt(countdownTime).ToString();

			// 메시지 설정
			if (messageText != null && messages.Length > 0)
				messageText.text = messages[UnityEngine.Random.Range(0, messages.Length)];

			// 버튼 상태
			bool bCanRevive = mCurrentReviveCount < maxReviveCount;
			if (continueButton != null)
			{
				continueButton.gameObject.SetActive(bCanRevive);
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
					gaugeArea.DOScale(mGaugeOriginalScale * gaugePopScale, gaugePopDuration * 0.6F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					gaugeArea.DOScale(mGaugeOriginalScale, gaugePopDuration * 0.4F)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Continue 버튼 바운스
			if (continueButtonRect != null && continueButton.gameObject.activeSelf)
			{
				seq.Append(
					continueButtonRect.DOScale(mContinueOriginalScale * buttonPopScale, buttonPopDuration * 0.5F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					continueButtonRect.DOScale(mContinueOriginalScale, buttonPopDuration * 0.5F)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Restart 버튼 바운스
			if (restartButtonRect != null)
			{
				seq.AppendInterval(buttonDelay);
				seq.Append(
					restartButtonRect.DOScale(mRestartOriginalScale * buttonPopScale, buttonPopDuration * 0.5F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					restartButtonRect.DOScale(mRestartOriginalScale, buttonPopDuration * 0.5F)
						.SetEase(Ease.InOutQuad)
				);
			}
		}

		/// <summary>
		/// 카운트다운 시작
		/// </summary>
		private void StartCountdown()
		{
			if (mCountdownCoroutine != null)
				StopCoroutine(mCountdownCoroutine);

			mCountdownCoroutine = StartCoroutine(CountdownCoroutine());
		}

		/// <summary>
		/// 팝업 숨기기
		/// </summary>
		public void Hide()
		{
			if (!mIsPopupActive) return;

			Debug.Log("[GameOverPopup] Hide");

			mIsPopupActive = false;
			mIsInputBlocked = true;

			DOTween.Kill(this);

			if (mCountdownCoroutine != null)
			{
				StopCoroutine(mCountdownCoroutine);
				mCountdownCoroutine = null;
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
				gaugeArea.localScale = mGaugeOriginalScale;

			if (continueButtonRect != null)
				continueButtonRect.localScale = mContinueOriginalScale;

			if (restartButtonRect != null)
				restartButtonRect.localScale = mRestartOriginalScale;
		}

		/// <summary>
		/// 입력 차단 해제
		/// </summary>
		private IEnumerator UnblockInputAfterDelay()
		{
			yield return new WaitForSeconds(inputBlockDuration);

			mIsInputBlocked = false;

			// 버튼 활성화 + 펄스 효과
			if (continueButton != null && continueButton.gameObject.activeSelf)
			{
				continueButton.interactable = true;
				continueButtonRect?.DOPunchScale(Vector3.one * 0.1F, 0.2F, 5, 0.5F);
			}

			if (restartButton != null)
			{
				restartButton.interactable = true;
				restartButtonRect?.DOPunchScale(Vector3.one * 0.1F, 0.2F, 5, 0.5F);
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

			while (remainingTime > 0F)
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
						countText.rectTransform.DOPunchScale(Vector3.one * 0.2F, 0.3F, 5, 0.5F);
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
			if (mIsInputBlocked) return;

			Debug.Log("[GameOverPopup] Continue clicked");
			mCurrentReviveCount++;

			Hide();
			OnContinue?.Invoke();
		}

		private void OnRestartClick()
		{
			if (mIsInputBlocked) return;

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
			return maxReviveCount - mCurrentReviveCount;
		}
	}
}
