using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using TrumpTile.GameMain.Item;
using DG.Tweening;
namespace TrumpTile.GameMain.Core
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
		[SerializeField] private ItemButtonConfig[] mItemButtonConfigs;
		[SerializeField] private Button mPauseButton;

		[Serializable]
		private class ItemButtonConfig
		{
			public int itemId;
			public Button button;
			public TextMeshProUGUI countText;
		}

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
		[SerializeField] private GameObject mComboPrefab;

		[Header("Animation")]
		[SerializeField] private Animator mComboAnimator;

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
			if (mItemButtonConfigs != null)
			{
				foreach (ItemButtonConfig config in mItemButtonConfigs)
				{
					if (config.button == null)
					{
						continue;
					}
					int capturedId = config.itemId;
					config.button.onClick.AddListener(() => OnItemButtonClick(capturedId));
				}
			}

			if (mPauseButton != null)
			{
				mPauseButton.onClick.AddListener(OnPauseClick);
			}

			if (mNextLevelButton != null)
			{
				mNextLevelButton.onClick.AddListener(OnNextLevelClick);
			}

			if (mClearRestartButton != null)
			{
				mClearRestartButton.onClick.AddListener(OnRestartClick);
			}

			if (mClearMainMenuButton != null)
			{
				mClearMainMenuButton.onClick.AddListener(OnMainMenuClick);
			}

			if (mResumeButton != null)
			{
				mResumeButton.onClick.AddListener(OnResumeClick);
			}

			if (mPauseRestartButton != null)
			{
				mPauseRestartButton.onClick.AddListener(OnRestartClick);
			}

			if (mPauseMainMenuButton != null)
			{
				mPauseMainMenuButton.onClick.AddListener(OnMainMenuClick);
			}
		}

		private void SubscribeEvents()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.OnScoreChanged += UpdateScore;
				GameManager.Instance.OnComboChanged += UpdateCombo;
				GameManager.Instance.OnProgressChanged += UpdateProgress;
			}
			ItemManager.Inst.OnItemCountChanged += UpdateItemCount;
		}

		private void OnDestroy()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.OnScoreChanged -= UpdateScore;
				GameManager.Instance.OnComboChanged -= UpdateCombo;
				GameManager.Instance.OnProgressChanged -= UpdateProgress;
			}
			// if (ItemManager.Inst != null)
			// {
			// 	//ItemManager.Inst.OnItemCountChanged -= UpdateItemCount;
			// }
		}

		#region Button Callbacks

		private void OnItemButtonClick(int itemId)
		{
			if (GameManager.Instance == null || !GameManager.Instance.CanUseItem())
			{
				return;
			}
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
			ItemManager.Inst.UseItem(itemId);
		}

		private void OnPauseClick()
		{
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
			GameManager.Instance?.PauseGame();
			ShowPausePanel();
		}

		private void OnResumeClick()
		{
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
			HideAllPanels();
			GameManager.Instance?.ResumeGame();
		}

		private void OnRestartClick()
		{
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
			HideAllPanels();
			Time.timeScale = 1F;
			GameManager.Instance?.RestartLevel();
		}

		private void OnNextLevelClick()
		{
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
			HideAllPanels();
			GameManager.Instance?.NextLevel();
		}

		private void OnMainMenuClick()
		{
			AudioEvent.Play(EAudioKey.SFX_BtnClick);
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
			if (mScoreText == null)
			{
				yield break;
			}

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
			{
				mLevelText.text = $"Level {level}";
			}
		}

		public void UpdateGold(int gold)
		{
			if (mGoldText != null)
			{
				mGoldText.text = $"{gold:N0}";
			}
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
					{
						mComboAnimator.SetTrigger("Pulse");
					}

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
			if (mComboText == null)
			{
				yield break;
			}

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

		// 특정 아이템 개수 변경 시 호출 (ItemManager.OnItemCountChanged)
		public void UpdateItemCount(int itemId, int count)
		{
			if (mItemButtonConfigs == null)
			{
				return;
			}
			foreach (ItemButtonConfig config in mItemButtonConfigs)
			{
				if (config.itemId != itemId)
				{
					continue;
				}
				if (config.countText != null)
				{
					config.countText.text = count.ToString();
				}
				if (config.button != null)
				{
					bool bCanUse = GameManager.Instance != null &&
						GameManager.Instance.CurrentState == GameManager.EGameState.Playing;
					config.button.image.color = count > 0? Color.white : new Color(200f / 255f,200f / 255f,200f / 255f,128f / 255f);
				}
				break;
			}
		}

		// 레벨 시작 시 전체 아이템 UI 동기화
		public void RefreshAllItemButtons()
		{
			if (mItemButtonConfigs == null)
			{
				return;
			}
			bool bCanUse = GameManager.Instance != null &&
				GameManager.Instance.CurrentState == GameManager.EGameState.Playing;

			foreach (ItemButtonConfig config in mItemButtonConfigs)
			{
				int count = ItemManager.Inst.GetItemCount(config.itemId);
				if (config.countText != null)
				{
					config.countText.text = count.ToString();
				}
				if (config.button != null)
				{
					config.button.image.color = count > 0? Color.white : new Color(200f / 255f,200f / 255f,200f / 255f,128f / 255f);
				}
			}
		}

		public void UpdateItemButtonStates()
		{
			RefreshAllItemButtons();
		}

		public void DisableItemButtons()
		{
			if (mItemButtonConfigs == null)
			{
				return;
			}
			foreach (ItemButtonConfig config in mItemButtonConfigs)
			{
				if (config.button != null)
				{
					config.button.interactable = false;
				}
			}
		}

		public void UpdateProgress(int matched, int total)
		{
			// 진행률 표시 (필요시 구현)
		}

		#endregion

		#region Panels

		public void HideAllPanels()
		{
			if (mLevelClearPanel != null)
			{
				mLevelClearPanel.SetActive(false);
			}
			if (mPausePanel != null)
			{
				mPausePanel.SetActive(false);
			}
		}

		public void ShowLevelClearPanel(int stars = 3)
		{
			if (mLevelClearPanel != null)
			{
				mLevelClearPanel.SetActive(true);

				if (mClearScoreText != null && GameManager.Instance != null)
				{
					mClearScoreText.text = $"{GameManager.Instance.GetScore():N0}";
				}

				if (mClearLevelText != null && GameManager.Instance != null)
				{
					mClearLevelText.text = $"Level {GameManager.Instance.CurrentLevel}";
				}
			}
		}

		public void ShowPausePanel()
		{
			if (mPausePanel != null)
			{
				mPausePanel.SetActive(true);
			}
		}

		#endregion

		#region Floating Text
		public void ShowFloatingText(Vector3 position, string text, Color textColor, Color backgroundColor, Sprite sprite)
		{
			if (mComboPrefab == null)
			{
				return;
			}

			Transform parent = mFloatingTextParent != null ? mFloatingTextParent : transform;
			GameObject floatingObj = Instantiate(mComboPrefab, position, Quaternion.identity, parent);

			Image image = floatingObj.GetComponent<Image>();
			image.sprite = sprite;

			PlayComboSpriteAnim(image);
		}
		private void PlayComboSpriteAnim(Image image)
		{
			Sequence seq = DOTween.Sequence();

			seq.Append(image.DOFade(1, 0.3f));
			seq.Join(image.transform.DOScale(1.2f, 0.3f));
			seq.Append(image.transform.DOScale(1, 0.2f));
			seq.Append(image.DOFade(0, 0.2f));

			seq.OnComplete(() => Destroy(image.gameObject));
		}
		public void ShowFloatingText(Vector3 position, string text, Color textColor, Color backgroundColor)
		{
			if (mFloatingTextPrefab == null)
			{
				return;
			}

			Transform parent = mFloatingTextParent != null ? mFloatingTextParent : transform;
			GameObject floatingObj = Instantiate(mFloatingTextPrefab, position, Quaternion.identity, parent);

			TextMeshProUGUI tmp = floatingObj.GetComponentInChildren<TextMeshProUGUI>();
			if (tmp != null)
			{
				tmp.text = text;
				tmp.color = textColor;
			}
			if (floatingObj.TryGetComponent<RawImage>(out var rawImage))
			{
				rawImage.color = backgroundColor;
			}
			int rand = UnityEngine.Random.Range(0, 3);
			if(rand != 0)
			{
				tmp.color = new Color(textColor.r,textColor.g,textColor.b, 1);
				if(rand == 1)
				{
					PlayComboAnim_A(floatingObj);
				}
				else if(rand == 2)
				{
					PlayComboAnim_B(floatingObj);
				}
				else
				{
					PlayComboAnim_C(floatingObj);
				}
			}
			else
			{
				StartCoroutine(AnimateFloatingText(floatingObj));
			}
		}
		private void PlayComboAnim_A(GameObject obj)
		{
			Sequence seq = DOTween.Sequence();
			obj.transform.localScale = new Vector2(1,0);
			
			seq.Append(obj.transform.DOScaleY(1, 0.5f).SetEase(Ease.OutExpo));
			seq.Append(obj.GetComponent<CanvasGroup>().DOFade(0, 0.5f));
			seq.OnComplete(() => Destroy(obj));
		}
		private void PlayComboAnim_B(GameObject obj)
		{
			Sequence seq = DOTween.Sequence();
			obj.transform.localScale = new Vector2(0,0);
			
			seq.Append(obj.transform.DOScale(1.1f, 0.25f).SetEase(Ease.OutExpo));
			seq.Append(obj.transform.DOScale(1, 0.25f).SetEase(Ease.OutExpo));
			seq.Append(obj.GetComponent<CanvasGroup>().DOFade(0, 0.5f));
			seq.OnComplete(() => Destroy(obj));
		}
		private void PlayComboAnim_C(GameObject obj)
		{
			Sequence seq = DOTween.Sequence();
			TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
			string value = tmp.text;
			seq.Append(DOTween.To(() => 0, x =>
			{
				tmp.text = value.Substring(0, x);
			}, value.Length, 0.5f).SetEase(Ease.Linear));
			seq.Append(obj.GetComponent<CanvasGroup>().DOFade(0, 0.5f));
			seq.OnComplete(() => Destroy(obj));
		}
		private IEnumerator AnimateFloatingText(GameObject obj)
		{
			float duration = 1F;
			float elapsed = 0F;

			TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
			CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
			RawImage rawImage = obj.GetComponent<RawImage>();

			Color startColor = tmp != null ? tmp.color : Color.white;
			Color backgroundStartColor = rawImage != null ? rawImage.color : Color.white;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;

				if (canvasGroup != null)
				{
					Color newColor = startColor;
					Color newBackgroundColor = backgroundStartColor;

					newColor.a = 1f - t;
					newBackgroundColor.a = 1f - t;
					
					tmp.color = newColor;
					rawImage.color = newBackgroundColor;
                    canvasGroup.alpha = newColor.a;
				}

				yield return null;
			}

			Destroy(obj);
		}

		// public void ShowScorePopup(Vector3 position, int score)
		// {
		// 	ShowFloatingText(position, $"+{score}", Color.yellow);
		// }

		// public void ShowComboPopup(Vector3 position, int combo)
		// {
		// 	ShowFloatingText(position, $"x{combo} Combo!", Color.cyan);
		// }

		#endregion
	}
}
