using System.Collections;
using System.Collections.Generic;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class TextLocalizeSetter : MonoBehaviour
    {
        [SerializeField] private int mKey;
        
        public void Initialize()
        {
            if(mKey == 0) return;

            GetComponent<TMP_Text>().text = LocalizeManager.Inst.GetString(mKey);

            if (SettingsManager.Inst == null)
            {
                return;
            }

            bool bIsRTL = SettingsManager.Inst.Language == ELanguage.Arabic;
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<IgnoreRTL>() != null)
                {
                    continue;
                }
                text.isRightToLeftText = bIsRTL;
            }
        }
    }
}
