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
        [Header("젬 2배 버프 아이콘")]
        [SerializeField] private Sprite mGemDoubleSprite;
        public void Ininitialize(int level, ProductReward[] rewards, bool isLock, bool isCollect)
        {
            mLevelText.text = level.ToString();
            for(int i = 0; i < rewards.Length; i++)
            {
                RewardDisplayInfo info = rewards[i].GetRewardDisplayInfo();

                //젬 2배는 ItemId가 없으므로 아이템 인덱싱(ItemId-1004)을 타면 안 된다.
                if(info.Type == ERewardType.GemDouble)
                {
                    mRewardConfigArray[i].Image.sprite = mGemDoubleSprite;
                    //Amount는 지속 시간(분)이라 개수 표기("x")가 아니라 분으로 보여준다.
                    mRewardConfigArray[i].Text.text = info.Amount.ToString() + "m";
                }
                else
                {
                    mRewardConfigArray[i].Image.sprite = info.Type == ERewardType.Gold? mRewardSpriteArray[0] : mRewardSpriteArray[info.ItemId - 1004];
                    mRewardConfigArray[i].Text.text = "x" + info.Amount.ToString();
                }
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
