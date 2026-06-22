using System.Collections;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
	public class SpaceTravelGatherView : UIBase
	{
		[Header("인원 카운터 텍스트")]
		[SerializeField] private TMP_Text mPlayerCountText;
		[Header("집결 완료 후 대기 시간 (초)")]
		[SerializeField] private float mCompletionDelay = 1.5F;

		private const int GATHER_TARGET = 100;
		private const float GATHER_DURATION = 2.5F;

		public override void Initialize()
		{
			base.Initialize();

			EventManager.Inst.AddEvent("SpaceTravel_ShowGather", PlayGatherAnim);
		}

		private void OnDestroy()
		{
			EventManager.Inst?.RemoveEvent("SpaceTravel_ShowGather", PlayGatherAnim);
		}

		private void PlayGatherAnim()
		{
			gameObject.SetActive(true);
			mPlayerCountText.text = "0";
			StartCoroutine(Co_GatherAnim());
		}

		private IEnumerator Co_GatherAnim()
		{
			int currentCount = 0;
			Tween tween = DOTween.To(
				() => currentCount,
				x =>
				{
					currentCount = x;
					mPlayerCountText.text = x.ToString();
				},
				GATHER_TARGET,
				GATHER_DURATION
			).SetEase(Ease.OutQuad);

			yield return tween.WaitForCompletion();
			yield return new WaitForSeconds(mCompletionDelay);

			gameObject.SetActive(false);
			EventManager.Inst.ActiveEvent("SpaceTravel_ShowProgress");
		}
	}
}
