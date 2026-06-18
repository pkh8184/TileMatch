using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
    [System.Serializable]
    public class GemCollectionRewardConfig
    {
        public Image Image;
        public TMP_Text Text;
    }
    public class GemCollectionRewardUI : MonoBehaviour
    {
        [Header("단계 텍스트")]
        [SerializeField] private TMP_Text mLevelText;
        [Header("보상 UI")]
        [SerializeField] private GemCollectionRewardConfig[] mRewardConfigArray;
        [Header("지급 완료 표시 / 잠김 표시")]
        [SerializeField] private GameObject mCheckObejct;
        [SerializeField] private GameObject mLockObejct;
        [Header("보상 스프라이트")]
        [SerializeField] private Sprite[] mRewardSpriteArray;
        public void Ininitialize(int level, ProductReward[] rewards, bool isLock, bool isCollect)
        {
            mLevelText.text = level.ToString();
            for(int i = 0; i < rewards.Length; i++)
            {
                RewardDisplayInfo info = rewards[i].GetRewardDisplayInfo();

                mRewardConfigArray[i].Image.sprite = info.Type == ERewardType.Gold? mRewardSpriteArray[0] : mRewardSpriteArray[info.ItemId - 1004];
                mRewardConfigArray[i].Text.text = "x" + info.Amount.ToString();
                mRewardConfigArray[i].Image.gameObject.SetActive(true);
            }
            mLockObejct.SetActive(isLock);
            mCheckObejct.SetActive(isCollect);
        }
        public void SetUnlock()
        {
            mLockObejct.SetActive(false);
        }
        public void SetCollect()
        {
            mCheckObejct.SetActive(true);
        }
        public GemCollectionRewardConfig[] GetConfigArray()
        {
            return mRewardConfigArray;
        }
    }
}
