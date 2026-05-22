using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TrumpTile.GameMain.UI
{
    public class RoulettePopup : PopupBase
    {
        [Header("애니메이션을 위한 참조")]
        [SerializeField] private RectTransform mRouletteRect;
        protected override void PlayShowAnim()
        {
            base.PlayShowAnim();

            Sequence seq = DOTween.Sequence();
            mRouletteRect.localRotation = Quaternion.Euler(0,0,-207);

            seq.Append(mRouletteRect.DORotate(new Vector3(0,0,-242), 1f).SetEase(Ease.OutQuart));
        }
    }    
}

