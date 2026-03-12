using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TrumpTile.FrameLibrary;

namespace TrumpTile.GameMain.Data
{
    public class PlayerDataManager : Singleton_GameObject<PlayerDataManager>
    {
        private UserData mUserData;
        public UserData UserData { get => mUserData; set => mUserData = value;}
    }
}
