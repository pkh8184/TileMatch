using DG.Tweening;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>타일 착지 손맛: 살짝 눌렸다(squash) 원래로 안착(살짝 오버슈트).</summary>
	public static class TileJuice
	{
		// 착지 젤리 연출 사용 여부. 레벨 로드 시 GameManager가 설정한다.
		// (일일 퍼즐 + Water/Dark 테마이고 토글이 켜져 있을 때만 true)
		public static bool IsEnabled = false;

		public static void PlayLanding(Transform target, Vector3 settledScale)
		{
			if (!IsEnabled || target == null)
			{
				return;
			}

			Vector3 squash = new Vector3(settledScale.x * 1.18F, settledScale.y * 0.82F, settledScale.z);

			Sequence seq = DOTween.Sequence();
			seq.SetUpdate(true);
			seq.Append(target.DOScale(squash, 0.06F).SetEase(Ease.OutQuad));
			seq.Append(target.DOScale(settledScale, 0.12F).SetEase(Ease.OutBack));
		}
	}
}
