using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrumpTile.GameMain.UI
{
    public class ChallengeView : PlayerDataView
    {
        [SerializeField] private Sprite mIcon;
        [SerializeField] private string mChallengeName;
        private Image mImage;
        private TMP_Text mChallengeText;

        protected override void Awake()
        {
            base.Awake();
            mImage = transform.Find("Icon").GetComponent<Image>();
            mChallengeText = transform.Find("TMP_Info").GetComponent<TMP_Text>();
            //mImage.sprite = mIcon;
            mChallengeText.text = mChallengeName;
        }
    }
}
