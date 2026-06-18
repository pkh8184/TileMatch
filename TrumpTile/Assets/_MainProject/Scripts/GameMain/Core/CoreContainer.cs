using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public static class CoreContainer 
    {
        public static readonly RewardContainer RewardContainer = new RewardContainer();
        public static int GetGemCount = 0;
        public static int GetGoldCount = 0;
        public static void ClearAll()
        {
            RewardContainer.Clear();
        }
    }    
}

