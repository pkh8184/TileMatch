using DG.Tweening;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
	/// <summary>
	/// 팝업 등장 스케일 연출 공통 처리.
	/// 0% → 130% → 100% 형태의 역동적인 팝(pop) 애니메이션을 시퀀스에 추가한다.
	/// </summary>
	public static class PopupScaleAnimator
	{
		public const float DEFAULT_PEAK = 1.3F;   // 최대 130%
		public const float RISE_RATIO = 0.6F;     // 상승 60% / 안착 40%

		/// <summary>
		/// 대상 트랜스폼을 0 스케일로 초기화한 뒤, 시퀀스에 0 → peak → 1 스케일 트윈을 추가한다.
		/// </summary>
		public static void AppendPopIn(Sequence sequence, Transform target, float duration, float peak = DEFAULT_PEAK)
		{
			if (sequence == null || target == null)
			{
				return;
			}

			target.localScale = Vector3.zero;
			sequence.Append(target.DOScale(peak, duration * RISE_RATIO).SetEase(Ease.OutQuad));
			sequence.Append(target.DOScale(1F, duration * (1F - RISE_RATIO)).SetEase(Ease.InOutQuad));
		}
	}
}
