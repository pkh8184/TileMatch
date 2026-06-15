using System.Collections;
using System.Collections.Generic;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [System.Serializable]
    public class PiggyBankContent : TemporaryContent
    {
        [Header("보상 목록")]
        [SerializeReference, SubclassSelector] private ProductReward[] mRewardArray;
    }   
}
