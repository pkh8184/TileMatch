using System.Collections;
using System.Linq;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// COMBO_TRIGGERED 이벤트 페이로드
	/// </summary>
	public struct ComboTriggeredPayload
	{
		public string Label;
		public int ConsecutiveCount;
		public bool HasSound;
		public AudioClip AudioClip;
		public Color LabelColor;
	}

	/// <summary>
	/// 일반 콤보 시스템
	///
	/// [동작 방식]
	/// - EventManager를 통해 MATCH_OCCURRED 이벤트를 구독
	/// - 연속 매치 횟수에 해당하는 ComboEntry를 탐색하여 발동
	/// - isRandom=TRUE 항목이 여러 개이면 그 중 하나를 랜덤 선택
	/// - comboEndTime 내에 다음 매치가 없으면 연속 카운트를 초기화
	/// - 콤보 결과를 EventManager를 통해 COMBO_TRIGGERED / COMBO_RESET으로 발행
	///
	/// [인스펙터 설정]
	/// - mComboDatabase: 사용할 ComboDatabase 에셋
	/// - mComboDisplayPoint: 콤보 레이블이 표시될 월드 위치 Transform (미설정 시 슬롯 위치 사용)
	/// - mDefaultComboEndTime: 매칭 항목이 없을 때 사용할 기본 타이머
	///
	/// </summary>
	public class ComboSystem : MonoBehaviour
	{
		[Header("Combo Database")]
		[Tooltip("사용할 ComboDatabase 에셋")]
		[SerializeField] private ComboDatabase mComboDatabase;

		[Header("Display Settings")]
		[Tooltip("콤보 레이블이 표시될 월드 위치. 미설정 시 슬롯 마지막 타일 위치를 사용")]
		[SerializeField] private Transform mComboDisplayPoint;

		[Header("Timer Settings")]
		[Tooltip("매칭 항목이 없을 때 사용할 기본 콤보 종료 시간 (초)")]
		[SerializeField] private float mDefaultComboEndTime = 4f;

		private int mConsecutiveMatchCount;
		private Coroutine mResetTimerCoroutine;

		public int ConsecutiveMatchCount => mConsecutiveMatchCount;

		#region Unity Lifecycle

		private void Start()
		{
			if (mComboDatabase == null)
			{
				Debug.LogWarning("[ComboSystem] ComboDatabase가 설정되지 않았습니다.");
			}

			EventManager.Inst.AddEvent(EventKeys.MATCH_OCCURRED, OnMatchOccurred);
		}

		private void OnDestroy()
		{
			EventManager em = EventManager.Inst;
			if (em != null)
			{
				em.RemoveEvent(EventKeys.MATCH_OCCURRED);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// 콤보 카운트 및 타이머를 초기화합니다.
		/// 레벨 시작/재시작 시 호출하세요.
		/// </summary>
		public void ResetCombo()
		{
			mConsecutiveMatchCount = 0;

			if (mResetTimerCoroutine != null)
			{
				StopCoroutine(mResetTimerCoroutine);
				mResetTimerCoroutine = null;
			}

			EventManager.Inst.ActiveEvent(EventKeys.COMBO_RESET, (object)null);
			Debug.Log("[ComboSystem] 콤보 초기화");
		}

		#endregion

		#region Match Handler

		private void OnMatchOccurred(object param)
		{
			mConsecutiveMatchCount++;

			ComboEntry entry = SelectComboEntry(mConsecutiveMatchCount);
			float timerDuration = entry != null ? entry.comboEndTime : GetFallbackEndTime();

			if (entry != null)
			{
				TriggerCombo(entry);
			}

			if (mResetTimerCoroutine != null)
			{
				StopCoroutine(mResetTimerCoroutine);
			}

			mResetTimerCoroutine = StartCoroutine(ResetTimerCoroutine(timerDuration));
		}

		/// <summary>
		/// 현재 연속 횟수에 맞는 ComboEntry를 선택합니다.
		/// isRandom=TRUE 항목이 여러 개면 그 중 랜덤 선택,
		/// isRandom=FALSE만 있으면 첫 번째 항목 반환.
		/// 정확히 일치하는 항목이 없고 카운트가 DB 최대치를 초과하면 마지막 항목을 반환.
		/// </summary>
		private ComboEntry SelectComboEntry(int count)
		{
			ComboEntry[] entries = mComboDatabase?.Entries;
			if (entries == null || entries.Length == 0)
			{
				return null;
			}

			ComboEntry[] candidates = entries
				.Where(e => e.consecutiveCount == count)
				.ToArray();

			if (candidates.Length == 0)
			{
				// 카운트가 DB 최대치를 초과한 경우 가장 높은 항목을 반환
				ComboEntry lastEntry = entries
					.OrderByDescending(e => e.consecutiveCount)
					.FirstOrDefault();

				if (lastEntry != null && count > lastEntry.consecutiveCount)
				{
					return lastEntry;
				}

				return null;
			}

			if (candidates.Length == 1)
			{
				return candidates[0];
			}

			ComboEntry[] randomPool = candidates.Where(e => e.isRandom).ToArray();
			if (randomPool.Length > 0)
			{
				return randomPool[Random.Range(0, randomPool.Length)];
			}

			return candidates[0];
		}

		/// <summary>
		/// 현재 카운트보다 낮거나 같은 항목 중 가장 높은 것의 comboEndTime을 반환합니다.
		/// </summary>
		private float GetFallbackEndTime()
		{
			ComboEntry[] entries = mComboDatabase?.Entries;
			if (entries == null || entries.Length == 0)
			{
				return mDefaultComboEndTime;
			}

			ComboEntry last = entries
				.Where(e => e.consecutiveCount <= mConsecutiveMatchCount)
				.OrderByDescending(e => e.consecutiveCount)
				.FirstOrDefault();

			return last != null ? last.comboEndTime : mDefaultComboEndTime;
		}

		#endregion

		#region Combo Trigger

		private void TriggerCombo(ComboEntry entry)
		{
			if (entry.hasSound && entry.audioClip != null)
			{
				AudioManager.Inst?.PlaySFX(entry.audioClip);
			}

			UIManager.Instance.ShowFloatingText(GetDisplayPosition(), entry.label + "!", entry.labelColor);

			ComboTriggeredPayload payload = new ComboTriggeredPayload()
			{
				Label = entry.label,
				ConsecutiveCount = mConsecutiveMatchCount,
				HasSound = entry.hasSound,
				AudioClip = entry.audioClip,
				LabelColor = entry.labelColor
			};
			EventManager.Inst.ActiveEvent(EventKeys.COMBO_TRIGGERED, payload);

			Debug.Log($"[ComboSystem] {entry.label}! (연속 {mConsecutiveMatchCount}회 매치)");
		}

		private Vector3 GetDisplayPosition()
		{
			if (mComboDisplayPoint != null)
			{
				return mComboDisplayPoint.position;
			}

			if (SlotManager.Instance != null)
			{
				return SlotManager.Instance.GetLastTilePosition() + Vector3.up * 1.5f;
			}

			return Vector3.zero;
		}

		#endregion

		#region Timer

		private IEnumerator ResetTimerCoroutine(float duration)
		{
			yield return new WaitForSeconds(duration);

			mConsecutiveMatchCount = 0;
			mResetTimerCoroutine = null;

			EventManager.Inst.ActiveEvent(EventKeys.COMBO_RESET, (object)null);
			Debug.Log("[ComboSystem] 콤보 타이머 만료 - 연속 카운트 초기화");
		}

		#endregion
	}
}
