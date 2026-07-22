using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    [System.Serializable]
    public class ProfileResourceEntry
    {
        public int Id;
        public Sprite Sprite;
    }
    [System.Serializable]
    [CreateAssetMenu(fileName = "ProfileResourceDatabase", menuName = "TileMatch/ProfileResourceDatabase")]
    public class ProfileResourceDatabase : ScriptableObject
    {
        [SerializeField] private ProfileResourceEntry[] mProfileEntries;
        [SerializeField] private ProfileResourceEntry[] mFrameEntries;
        private Dictionary<int, Sprite> mResourceDict;

        public void Initialize()
        {
            if(mProfileEntries.Length == 0 || mFrameEntries.Length == 0)
            {
                Debug.LogError("[ProfileResourceDatabase] 엔트리가 비어있습니다.");
                return;
            }
            mResourceDict = new Dictionary<int, Sprite>();
            
            foreach(var item in mProfileEntries)
            {
                mResourceDict[item.Id] = item.Sprite;
            }
            foreach(var item in mFrameEntries)
            {
                mResourceDict[item.Id] = item.Sprite;
            }
        }
        public Sprite GetProfileSprite(int id)
        {
            return mResourceDict[id];
        }
        public Sprite GetFrameSprite(int id)
        {
            return mResourceDict[id];
        }
    }
}
