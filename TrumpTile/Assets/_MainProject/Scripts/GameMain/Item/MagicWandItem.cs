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

		public MagicWandItem(ITimerControllable timerControllable, EffectManager effectManager)
		{
			mTimerControllable = timerControllable;
			mEffectManager = effectManager;
		}

		public bool CanExecute()
		{
			return mTimerControllable != null;
		}

		public IEnumerator Execute(Action onComplete)
		{
			if (mEffectManager != null)
			{
				mEffectManager.PlayMagicWandSpineEffect();
			}
			AudioEvent.Play(EAudioKey.SFX_ItemUse);
			mTimerControllable.FreezeTimer(FREEZE_DURATION);

			yield return new WaitForSeconds(0.3f);

			onComplete?.Invoke();
		}
	}
}
