using System;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
    public enum ECardSuit
	{
        Spade = 0,    // ♠
        Heart = 1,    // ♥
        Diamond = 2,  // ♦
        Club = 3      // ♣
    }
    public enum ECardRank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }
    public enum ETileCartegory
	{
		Fruit = 0,
		Desert = 1,
		Interior = 2,
		Tools = 3,
		Length
	}
	/// <summary>
	/// 타일(카드) 데이터
	/// </summary>
	[CreateAssetMenu(fileName = "TileData", menuName = "TrumpTile/Tile Data")]
	public class TileData : ScriptableObject
	{
		[Header("Identity")]
		public string tileTypeId;  // 예: "Spade_Ace", "Heart_King"
		public string displayName;

		public string TileID => tileTypeId;

		/// <summary>
		/// 같은 타일인지 확인 (SlotManager에서 사용)
		/// </summary>
		public bool Matches(TileData other)
		{
			if (other == null)
			{
				return false;
			}
			return tileTypeId == other.tileTypeId;
		}

		[Header("Card Info")]
		public ETileCartegory tileCartegory;
		public ECardSuit suit;
		public ECardRank rank;

		[Header("Visual")]
		public Sprite sprite;
		public Color tintColor = Color.white;

		[Header("Audio")]
		public AudioClip selectSound;
		public AudioClip matchSound;


#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(tileTypeId))
			{
				//tileTypeId = $"{suit}_{rank}";
			}

			if (string.IsNullOrEmpty(displayName))
			{
				//displayName = $"{suit} {rank}";
			}
		}
#endif
	}
}
