using System;
using System.Collections.Generic;
using System.Collections;
using TrumpTile.GameMain.Core;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class IngameView : ViewBase
    {
        public override void Initialize()
        {
            base.Initialize();

            //임시
            EventManager.Inst.AddEvent("IngameLoadingComplete", PlayFadeInAfterLoadLevel);
        }
        private void PlayFadeInAfterLoadLevel(object obj)
        {
            Action onComplete = obj as Action;

            StartCoroutine(Co_PlayFadeInAnimAfterLoadLevel(onComplete));
        }
        private IEnumerator Co_PlayFadeInAnimAfterLoadLevel(Action onComplete)
        {
            yield return StartCoroutine(Co_FadeInAnim());

            onComplete?.Invoke();
        }
    }
    
}

