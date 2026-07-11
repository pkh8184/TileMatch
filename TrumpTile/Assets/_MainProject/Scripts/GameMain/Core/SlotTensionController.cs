using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 슬롯 위험 경고 + 클러치 세이브 연출을 스스로 처리한다.
	/// 씬 와이어링 불필요: 오버레이 UI를 런타임에 직접 생성하고, SlotManager.Instance 를 자동 구독한다.
	/// 컴포넌트를 인게임 씬의 아무 오브젝트에 하나만 붙이면 된다.
	/// </summary>
	public class SlotTensionController : MonoBehaviour
	{
		[Header("SFX (선택 — mbPlaySfx 를 켜야 재생)")]
		[SerializeField] private bool mbPlaySfx = false;
		[SerializeField] private EAudioKey mHeartbeatSfx;
		[SerializeField] private EAudioKey mClutchSfx;

		[Header("연출 값")]
		[SerializeField] private int mPulseCount = 2;       // 한 번 뜰 때 깜빡이는 횟수
		[SerializeField] private int mMaxDangerShows = 2;   // 스테이지당 위험 경고가 뜨는 최대 횟수
		[SerializeField] private float mPulsePeriod = 0.6F;
		[SerializeField] private float mDangerMaxAlpha = 0.4F;
		[SerializeField] private float mFlashMaxAlpha = 0.7F;
		[SerializeField] private float mSlowScale = 0.2F;
		[SerializeField] private float mSlowRealDuration = 0.35F;

		private readonly SlotTensionState mState = new SlotTensionState();
		private SlotManager mSlot;
		private Image mDangerOverlay;
		private Image mFlashOverlay;
		private Sequence mPulseSeq;
		private int mDangerShownCount;

		private void Awake()
		{
			CreateOverlay();
		}

		private void Start()
		{
			mSlot = SlotManager.Instance;
			if (mSlot != null)
			{
				mSlot.OnSlotCountChanged += HandleSlotCountChanged;
			}
		}

		private void OnDestroy()
		{
			if (mSlot != null)
			{
				mSlot.OnSlotCountChanged -= HandleSlotCountChanged;
			}
			if (mPulseSeq != null)
			{
				mPulseSeq.Kill();
			}
		}

		/// <summary>스테이지 시작 시 호출(위험/클러치 플래그 초기화 + 경고 정지).</summary>
		public void ResetForNewStage()
		{
			mState.Reset();
			mDangerShownCount = 0;
			StopPulse();
		}

		private void HandleSlotCountChanged(int count, int max, ESlotDecreaseReason reason)
		{
			ETensionEvent ev = mState.Evaluate(count, max, reason);
			switch (ev)
			{
				case ETensionEvent.EnterDanger:
				{
					StartPulse();
					break;
				}
				case ETensionEvent.ExitDanger:
				{
					StopPulse();
					break;
				}
				case ETensionEvent.ClutchSave:
				{
					StopPulse();
					PlayClutch();
					break;
				}
			}
		}

		#region 위험 경고

		private void StartPulse()
		{
			if (mDangerOverlay == null)
			{
				return;
			}

			if (mDangerShownCount >= mMaxDangerShows)
			{
				return;
			}
			mDangerShownCount++;

			if (mPulseSeq != null)
			{
				mPulseSeq.Kill();
			}

			mPulseSeq = DOTween.Sequence();
			mPulseSeq.SetUpdate(true);
			mPulseSeq.AppendCallback(() => PlaySfx(mHeartbeatSfx));
			mPulseSeq.Append(FadeOverlay(mDangerOverlay, mDangerMaxAlpha, mPulsePeriod * 0.5F));
			mPulseSeq.Append(FadeOverlay(mDangerOverlay, 0F, mPulsePeriod * 0.5F));
			mPulseSeq.SetLoops(mPulseCount);
		}

		private void StopPulse()
		{
			if (mPulseSeq != null)
			{
				mPulseSeq.Kill();
				mPulseSeq = null;
			}
			SetAlpha(mDangerOverlay, 0F);
		}

		#endregion

		#region 클러치 세이브

		private void PlayClutch()
		{
			PlaySfx(mClutchSfx);
			PlayFlash();
			StartCoroutine(Co_SlowMotion());
		}

		private void PlayFlash()
		{
			if (mFlashOverlay == null)
			{
				return;
			}

			Sequence seq = DOTween.Sequence();
			seq.SetUpdate(true);
			seq.Append(FadeOverlay(mFlashOverlay, mFlashMaxAlpha, 0.05F));
			seq.Append(FadeOverlay(mFlashOverlay, 0F, 0.25F));
		}

		private IEnumerator Co_SlowMotion()
		{
			float original = Time.timeScale;
			try
			{
				Time.timeScale = mSlowScale;
				yield return new WaitForSecondsRealtime(mSlowRealDuration);
			}
			finally
			{
				Time.timeScale = original <= 0F ? 1F : original;
			}
		}

		#endregion

		#region UI 생성 / 유틸

		private void CreateOverlay()
		{
			GameObject canvasGo = new GameObject("TensionOverlayCanvas");
			canvasGo.hideFlags = HideFlags.HideInHierarchy;   // Hierarchy에 안 보이게(실수 클릭 방지)
			canvasGo.transform.SetParent(transform, false);

			Canvas canvas = canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 32000;

			mDangerOverlay = CreateFullScreenImage(canvasGo.transform, "DangerOverlay", new Color(1F, 0F, 0F, 0F));
			mFlashOverlay = CreateFullScreenImage(canvasGo.transform, "FlashOverlay", new Color(1F, 1F, 1F, 0F));
		}

		private Image CreateFullScreenImage(Transform parent, string objName, Color color)
		{
			GameObject go = new GameObject(objName);
			go.transform.SetParent(parent, false);

			Image img = go.AddComponent<Image>();
			img.color = color;
			img.raycastTarget = false;

			RectTransform rt = img.rectTransform;
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;

			return img;
		}

		private Tween FadeOverlay(Image img, float targetAlpha, float duration)
		{
			return DOTween.To(() => img.color.a, alpha => SetAlpha(img, alpha), targetAlpha, duration);
		}

		private void SetAlpha(Image img, float alpha)
		{
			if (img == null)
			{
				return;
			}
			Color c = img.color;
			c.a = alpha;
			img.color = c;
		}

		private void PlaySfx(EAudioKey key)
		{
			if (mbPlaySfx)
			{
				AudioEvent.Play(key);
			}
		}

		#endregion
	}
}
