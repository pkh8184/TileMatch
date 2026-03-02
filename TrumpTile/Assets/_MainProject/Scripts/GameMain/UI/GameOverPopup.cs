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
		[SerializeField] private GameObject mPopupPanel;
		[SerializeField] private Image mDimBackground;
		[SerializeField] private RectTransform mGaugeArea;
		[SerializeField] private Image mGaugeFill;
		[SerializeField] private TextMeshProUGUI mCountText;
		[SerializeField] private TextMeshProUGUI mMessageText;
		[SerializeField] private Button mContinueButton;
		[SerializeField] private RectTransform mContinueButtonRect;
		[SerializeField] private Button mRestartButton;
		[SerializeField] private RectTransform mRestartButtonRect;

		[Header("Animation Settings")]
		[SerializeField] private float mDimAlpha = 0.95F;
		[SerializeField] private float mGaugePopDuration = 0.4F;
		[SerializeField] private float mGaugePopScale = 1.2F;
		[SerializeField] private float mButtonPopDuration = 0.3F;
		[SerializeField] private float mButtonPopScale = 1.3F;
		[SerializeField] private float mButtonDelay = 0.1F;

		[Header("Countdown Settings")]
		[SerializeField] private float mCountdownTime = 10F;
		[SerializeField] private float mInputBlockDuration = 2F;
		[SerializeField] private int mMaxReviveCount = 3;

		[Header("Messages")]
		[SerializeField]
		private string[] mMessages = new string[]
		{
			"Don't give Up!",
			"Almost there!",
			"Keep going!"
		};

		[Header("Colors")]
		[SerializeField] private Color mGaugeFullColor = new Color(0.3F, 0.85F, 0.3F);
		[SerializeField] private Color mGaugeLowColor = new Color(0.85F, 0.3F, 0.3F);

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

			if (mPopupPanel != null)
			{
				mPopupPanel.SetActive(false);
			}
		}

		private void SaveOriginalValues()
		{
			mGaugeOriginalScale = mGaugeArea != null ? mGaugeArea.localScale : Vector3.one;
			mContinueOriginalScale = mContinueButtonRect != null ? mContinueButtonRect.localScale : Vector3.one;
			mRestartOriginalScale = mRestartButtonRect != null ? mRestartButtonRect.localScale : Vector3.one;
		}

		private void SetupButtons()
		{
			if (mContinueButton != null)
			{
				mContinueButton.onClick.AddListener(OnContinueClick);
			}

			if (mRestartButton != null)
			{
				mRestartButton.onClick.AddListener(OnRestartClick);
			}
		}

		private void OnDestroy()
		{
			DOTween.Kill(this);

			if (mContinueButton != null)
			{
				mContinueButton.onClick.RemoveListener(OnContinueClick);
			}

			if (mRestartButton != null)
			{
				mRestartButton.onClick.RemoveListener(OnRestartClick);
			}
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
			if (mIsPopupActive)
			{
				return;
			}

			Debug.Log("[GameOverPopup] Show");

			mIsPopupActive = true;
			mIsInputBlocked = true;

			// 1. 배경 즉시 어둡게 (애니메이션 없음)
			SetBackgroundDark();

			// 2. UI 초기 상태 (스케일 0)
			SetUIInitialState();

			// 3. 팝업 활성화
			if (mPopupPanel != null)
			{
				mPopupPanel.SetActive(true);
			}

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
			if (mDimBackground != null)
			{
				Color c = mDimBackground.color;
				c.a = mDimAlpha;
				mDimBackground.color = c;
			}
		}

		/// <summary>
		/// UI 초기 상태 설정
		/// </summary>
		private void SetUIInitialState()
		{
			// 게이지 스케일 0
			if (mGaugeArea != null)
			{
				mGaugeArea.localScale = Vector3.zero;
			}

			// 버튼들 스케일 0
			if (mContinueButtonRect != null)
			{
				mContinueButtonRect.localScale = Vector3.zero;
			}

			if (mRestartButtonRect != null)
			{
				mRestartButtonRect.localScale = Vector3.zero;
			}

			// 게이지 채움 초기화
			if (mGaugeFill != null)
			{
				mGaugeFill.fillAmount = 1F;
				mGaugeFill.color = mGaugeFullColor;
			}

			// 카운트 텍스트
			if (mCountText != null)
			{
				mCountText.text = Mathf.CeilToInt(mCountdownTime).ToString();
			}

			// 메시지 설정
			if (mMessageText != null && mMessages.Length > 0)
			{
				mMessageText.text = mMessages[UnityEngine.Random.Range(0, mMessages.Length)];
			}

			// 버튼 상태
			bool bCanRevive = mCurrentReviveCount < mMaxReviveCount;
			if (mContinueButton != null)
			{
				mContinueButton.gameObject.SetActive(bCanRevive);
				mContinueButton.interactable = false;
			}

			if (mRestartButton != null)
			{
				mRestartButton.interactable = false;
			}
		}

		/// <summary>
		/// UI 애니메이션
		/// </summary>
		private void PlayUIAnimation()
		{
			Sequence seq = DOTween.Sequence();

			// 게이지 바운스
			if (mGaugeArea != null)
			{
				seq.Append(
					mGaugeArea.DOScale(mGaugeOriginalScale * mGaugePopScale, mGaugePopDuration * 0.6F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					mGaugeArea.DOScale(mGaugeOriginalScale, mGaugePopDuration * 0.4F)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Continue 버튼 바운스
			if (mContinueButtonRect != null && mContinueButton.gameObject.activeSelf)
			{
				seq.Append(
					mContinueButtonRect.DOScale(mContinueOriginalScale * mButtonPopScale, mButtonPopDuration * 0.5F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					mContinueButtonRect.DOScale(mContinueOriginalScale, mButtonPopDuration * 0.5F)
						.SetEase(Ease.InOutQuad)
				);
			}

			// Restart 버튼 바운스
			if (mRestartButtonRect != null)
			{
				seq.AppendInterval(mButtonDelay);
				seq.Append(
					mRestartButtonRect.DOScale(mRestartOriginalScale * mButtonPopScale, mButtonPopDuration * 0.5F)
						.SetEase(Ease.OutBack)
				);
				seq.Append(
					mRestartButtonRect.DOScale(mRestartOriginalScale, mButtonPopDuration * 0.5F)
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
			{
				StopCoroutine(mCountdownCoroutine);
			}

			mCountdownCoroutine = StartCoroutine(CountdownCoroutine());
		}

		/// <summary>
		/// 팝업 숨기기
		/// </summary>
		public void Hide()
		{
			if (!mIsPopupActive)
			{
				return;
			}

			Debug.Log("[GameOverPopup] Hide");

			mIsPopupActive = false;
			mIsInputBlocked = true;

			DOTween.Kill(this);

			if (mCountdownCoroutine != null)
			{
				StopCoroutine(mCountdownCoroutine);
				mCountdownCoroutine = null;
			}

			if (mPopupPanel != null)
			{
				mPopupPanel.SetActive(false);
			}

			ResetUI();
		}

		/// <summary>
		/// UI 원본 상태 복원
		/// </summary>
		private void ResetUI()
		{
			if (mGaugeArea != null)
			{
				mGaugeArea.localScale = mGaugeOriginalScale;
			}

			if (mContinueButtonRect != null)
			{
				mContinueButtonRect.localScale = mContinueOriginalScale;
			}

			if (mRestartButtonRect != null)
			{
				mRestartButtonRect.localScale = mRestartOriginalScale;
			}
		}

		/// <summary>
		/// 입력 차단 해제
		/// </summary>
		private IEnumerator UnblockInputAfterDelay()
		{
			yield return new WaitForSeconds(mInputBlockDuration);

			mIsInputBlocked = false;

			// 버튼 활성화 + 펄스 효과
			if (mContinueButton != null && mContinueButton.gameObject.activeSelf)
			{
				mContinueButton.interactable = true;
				mContinueButtonRect?.DOPunchScale(Vector3.one * 0.1F, 0.2F, 5, 0.5F);
			}

			if (mRestartButton != null)
			{
				mRestartButton.interactable = true;
				mRestartButtonRect?.DOPunchScale(Vector3.one * 0.1F, 0.2F, 5, 0.5F);
			}

			Debug.Log("[GameOverPopup] Input unblocked");
		}

		/// <summary>
		/// 카운트다운
		/// </summary>
		private IEnumerator CountdownCoroutine()
		{
			float remainingTime = mCountdownTime;
			int lastSecond = -1;

			while (remainingTime > 0F)
			{
				remainingTime -= Time.deltaTime;
				float progress = remainingTime / mCountdownTime;

				// 게이지 업데이트
				if (mGaugeFill != null)
				{
					mGaugeFill.fillAmount = progress;
					mGaugeFill.color = Color.Lerp(mGaugeLowColor, mGaugeFullColor, progress);
				}

				// 카운트 텍스트
				int currentSecond = Mathf.CeilToInt(remainingTime);
				if (mCountText != null && currentSecond != lastSecond)
				{
					mCountText.text = currentSecond.ToString();
					lastSecond = currentSecond;

					// 마지막 3초 펄스
					if (currentSecond <= 3 && currentSecond > 0)
					{
						mCountText.rectTransform.DOPunchScale(Vector3.one * 0.2F, 0.3F, 5, 0.5F);
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
			if (mIsInputBlocked)
			{
				return;
			}

			Debug.Log("[GameOverPopup] Continue clicked");
			mCurrentReviveCount++;

			Hide();
			OnContinue?.Invoke();
		}

		private void OnRestartClick()
		{
			if (mIsInputBlocked)
			{
				return;
			}

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
			return mMaxReviveCount - mCurrentReviveCount;
		}
	}
}
