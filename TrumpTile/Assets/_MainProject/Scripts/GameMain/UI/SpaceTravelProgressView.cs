using System.Collections;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class SpaceTravelProgressView : UIBase
	{
		[Header("현재 가상 인원 수 텍스트")]
		[SerializeField] private TMP_Text mFakePlayerCountText;
		[Header("현재 연승 수 텍스트")]
		[SerializeField] private TMP_Text mStreakCountText;
		[Header("남은 제한 시간 텍스트")]
		[SerializeField] private TMP_Text mRemainingTimeText;
		[Header("쿨타임 텍스트")]
		[SerializeField] private TMP_Text mCooldownText;
		[Header("진행 단계 이미지 배열 (7개)")]
		[SerializeField] private Image[] mStageImages;
		[Header("레드닷 오브젝트")]
		[SerializeField] private GameObject mRedDotObject;
		[Header("탈락 메시지 텍스트 (연출용)")]
		[SerializeField] private TMP_Text mEliminationMessageText;
		[Header("쿨타임 패널")]
		[SerializeField] private GameObject mCooldownPanel;
		[Header("진행 패널")]
		[SerializeField] private GameObject mProgressPanel;

		private SpaceTravelContent mContentData;
		private Coroutine mTimerCoroutine;
		private int mLastFakePlayerCount;

		public override void Initialize()
		{
			base.Initialize();

			mContentData = ContentManager.Inst.GetContentData<SpaceTravelContent>("SpaceTravel");

			if (mContentData == null || !mContentData.Unlock)
			{
				gameObject.SetActive(false);
				return;
			}

			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_SHOW_PROGRESS, RefreshView);
			EventManager.Inst.AddEvent(EventKeys.SPACE_TRAVEL_SET_RED_DOT, SetRedDot);

			mLastFakePlayerCount = mContentData.GetFakePlayerCount();
			RefreshView();
		}

		private void OnDestroy()
		{
			EventManager.Inst?.RemoveEvent(EventKeys.SPACE_TRAVEL_SHOW_PROGRESS, RefreshView);
			EventManager.Inst?.RemoveEvent(EventKeys.SPACE_TRAVEL_SET_RED_DOT, SetRedDot);
		}

		private void RefreshView()
		{
			ESpaceTravelState state = mContentData.GetState();

			mRedDotObject.SetActive(false);

			switch (state)
			{
				case ESpaceTravelState.Active:
					ShowProgressPanel();
					break;
				case ESpaceTravelState.FailCooldown:
				case ESpaceTravelState.SuccessCooldown:
					ShowCooldownPanel();
					break;
				default:
					mProgressPanel.SetActive(false);
					mCooldownPanel.SetActive(false);
					break;
			}
		}

		private void ShowProgressPanel()
		{
			mProgressPanel.SetActive(true);
			mCooldownPanel.SetActive(false);

			int streakCount     = mContentData.GetStreakCount();
			int fakeCount       = mContentData.GetFakePlayerCount();
			int eliminatedCount = mLastFakePlayerCount - fakeCount;

			mStreakCountText.text = $"{streakCount}/{mContentData.GetTargetStreakCount()}";

			if (eliminatedCount > 0)
			{
				PlayEliminationAnim(fakeCount, eliminatedCount);
			}
			else
			{
				mFakePlayerCountText.text = fakeCount.ToString();
			}

			mLastFakePlayerCount = fakeCount;

			UpdateStageImages(streakCount);

			if (mTimerCoroutine != null)
			{
				StopCoroutine(mTimerCoroutine);
			}
			mTimerCoroutine = StartCoroutine(Co_UpdateTimer());
		}

		private void ShowCooldownPanel()
		{
			mProgressPanel.SetActive(false);
			mCooldownPanel.SetActive(true);

			if (mTimerCoroutine != null)
			{
				StopCoroutine(mTimerCoroutine);
			}
			mTimerCoroutine = StartCoroutine(Co_UpdateCooldown());
		}

		private void PlayEliminationAnim(int newCount, int eliminatedCount)
		{
			int fromCount = newCount + eliminatedCount;
			DOTween.To(
				() => fromCount,
				x => mFakePlayerCountText.text = x.ToString(),
				newCount,
				1.2F
			).SetEase(Ease.OutQuad);

			mEliminationMessageText.text = $"{eliminatedCount}명이 탈락했습니다!";
			mEliminationMessageText.gameObject.SetActive(true);
			mEliminationMessageText.DOFade(0F, 1.5F)
				.SetDelay(1.5F)
				.OnComplete(() => mEliminationMessageText.gameObject.SetActive(false));
		}

		private void UpdateStageImages(int clearedCount)
		{
			for (int i = 0; i < mStageImages.Length; i++)
			{
				mStageImages[i].color = i < clearedCount ? Color.white : Color.gray;
			}
		}

		private void SetRedDot()
		{
			mRedDotObject.SetActive(true);
		}

		private IEnumerator Co_UpdateTimer()
		{
			while (mContentData.GetState() == ESpaceTravelState.Active)
			{
				float remaining  = mContentData.GetRemainingTime();
				int totalSeconds = (int)remaining;
				int minutes      = totalSeconds / 60;
				int seconds      = totalSeconds % 60;
				mRemainingTimeText.text = $"{minutes:D2}:{seconds:D2}";

				if (remaining <= 0F)
				{
					RefreshView();
					yield break;
				}
				yield return new WaitForSeconds(1F);
			}
		}

		private IEnumerator Co_UpdateCooldown()
		{
			while (
				mContentData.GetState() == ESpaceTravelState.FailCooldown ||
				mContentData.GetState() == ESpaceTravelState.SuccessCooldown)
			{
				float remaining  = mContentData.GetCooldownTime();
				int totalSeconds = (int)remaining;
				int hours        = totalSeconds / 3600;
				int minutes      = (totalSeconds % 3600) / 60;
				mCooldownText.text = $"{hours}시간 {minutes:D2}분 후 재도전 가능";

				if (remaining <= 0F)
				{
					RefreshView();
					yield break;
				}
				yield return new WaitForSeconds(30F);
			}
		}
	}
}
