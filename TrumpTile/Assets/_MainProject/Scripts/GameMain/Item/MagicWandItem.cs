using System;
using System.Collections;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.Item
{
	// 타이머를 10초간 정지
	public class MagicWandItem : IItem
	{
		public int ItemId => 1006;

		private const float FREEZE_DURATION = 10f;

		private ITimerControllable mTimerControllable;
		private EffectManager mEffectManager;

		private bool mbActionDone;
		public MagicWandItem(ITimerControllable timerControllable, EffectManager effectManager)
		{
			mTimerControllable = timerControllable;
			mEffectManager = effectManager;
			mbActionDone = true;
		}

		public bool CanExecute()
		{
			return mTimerControllable != null && mbActionDone;
		}

		public IEnumerator Execute(Action onComplete)
		{
			AudioEvent.Play(EAudioKey.SFX_Clock);

			mbActionDone = false;

			if (mEffectManager != null)
			{
				mEffectManager.PlayMagicWandSpineEffect();
				mEffectManager.PlayClockItemEffect(FREEZE_DURATION, () => mTimerControllable.FreezeTimer(FREEZE_DURATION), () => mbActionDone = true);
			}
			else
			{
				mTimerControllable.FreezeTimer(FREEZE_DURATION);
				yield return new WaitForSeconds(FREEZE_DURATION);
				mbActionDone = true;
			}

			yield return new WaitUntil(() => mbActionDone);
			yield return new WaitForSeconds(0.3f);

			onComplete?.Invoke();
		}
	}
}
