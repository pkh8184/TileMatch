using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    public class ItemTutorialPopup : PopupBase
    {
        [SerializeField] private TMP_Text mItemName;
        [SerializeField] private TMP_Text mItemDescription;
        [SerializeField] private Image mItemImage;

        public void SetValid(IngameItemConfig config)
        {
            mItemName.text = config.itemName;
            mItemDescription.text = config.itemDescription;
            mItemImage.sprite = config.itemIcon;
        } 
        protected override void PlayHideAnim()
        {
            Sequence seq = DOTween.Sequence();
            seq.Append(mPopupObj.transform.DOScale(0, mHideDuration).SetEase(Ease.InBack));
            seq.OnComplete(() =>
            {
                mOpenPopupCount = Mathf.Max(0, mOpenPopupCount - 1);
                GameManager.Instance.tutorialComplete = true;
                gameObject.SetActive(false);
            });
        }
    }    
}

