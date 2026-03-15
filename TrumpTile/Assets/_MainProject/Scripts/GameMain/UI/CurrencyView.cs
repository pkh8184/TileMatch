using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    public class CurrencyView : PlayerDataView
    {
        [SerializeField] private Sprite mIcon;
        private Image mImage;

        protected override void Awake()
        {
            base.Awake();
            mImage = transform.Find("Icon").GetComponent<Image>();
            mImage.sprite = mIcon;
        }
    }
}

