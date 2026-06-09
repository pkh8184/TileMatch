using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [System.Serializable]
    public class ContentUIController
    {
        [Header("레드닷 게임오브젝트")]
        [SerializeField] private GameObject mRedDotObject;
        public virtual void PlayShowButtonAnim(Button button)
        {
            if(button == null)
            {
                return;
            }
        }
        public void ActiveRedDot(bool isActive)
        {
            mRedDotObject.SetActive(isActive);
        }
    }    
}

