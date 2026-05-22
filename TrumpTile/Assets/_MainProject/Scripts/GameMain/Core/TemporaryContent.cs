using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    [CreateAssetMenu(fileName = "Content", menuName = "TrumpTile/TemporaryContent")]
    //[Serializable]
    public class TemporaryContent : ContentBase
    {
        [SerializeField] protected float mLimitTime;
        [SerializeField] protected float mCoolTime;
    }   
}
