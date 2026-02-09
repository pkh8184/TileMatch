using UnityEngine;

namespace TrumpTile.Core
{
	/// <summary>
	/// 카드 무늬
	/// </summary>
	public enum CardSuit
	{
		Spade = 0,    // ♠
		Heart = 1,    // ♥
		Diamond = 2,  // ♦
		Club = 3      // ♣
	}

	/// <summary>
	/// 카드 숫자
	/// </summary>
	public enum CardRank
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

	/// <summary>
	/// 타일(카드) 데이터
	/// </summary>
	[CreateAssetMenu(fileName = "TileData", menuName = "TrumpTile/Tile Data")]
	public class TileData : ScriptableObject
	{
		[Header("Identity")]
		public string tileTypeId;  // 예: "Spade_Ace", "Heart_King"
		public string displayName;

		// 프로퍼티 (BoardManager, SlotManager에서 사용)
		public string TileID => tileTypeId;

		/// <summary>
		/// 같은 타일인지 확인 (SlotManager에서 사용)
		/// </summary>
		public bool Matches(TileData other)
		{
			if (other == null) return false;
			return tileTypeId == other.tileTypeId;
		}

		[Header("Card Info")]
		public CardSuit suit;
		public CardRank rank;

		[Header("Visual")]
		public Sprite sprite;
		public Color tintColor = Color.white;

		[Header("Audio")]
		public AudioClip selectSound;
		public AudioClip matchSound;

		/// <summary>
		/// 카드 값 (점수 계산용)
		/// </summary>
		public int Value
		{
			get
			{
				switch (rank)
				{
					case CardRank.Ace: return 11;
					case CardRank.Jack:
					case CardRank.Queen:
					case CardRank.King:
						return 10;
					default:
						return (int)rank;
				}
			}
		}

		/// <summary>
		/// 무늬 색상 (빨강/검정)
		/// </summary>
		public Color SuitColor
		{
			get
			{
				switch (suit)
				{
					case CardSuit.Heart:
					case CardSuit.Diamond:
						return Color.red;
					default:
						return Color.black;
				}
			}
		}

		/// <summary>
		/// 같은 타입인지 확인 (매칭용)
		/// </summary>
		public bool IsSameType(TileData other)
		{
			if (other == null) return false;
			return tileTypeId == other.tileTypeId;
		}

		/// <summary>
		/// 같은 무늬인지 확인
		/// </summary>
		public bool IsSameSuit(TileData other)
		{
			if (other == null) return false;
			return suit == other.suit;
		}

		/// <summary>
		/// 같은 숫자인지 확인
		/// </summary>
		public bool IsSameRank(TileData other)
		{
			if (other == null) return false;
			return rank == other.rank;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			// 자동으로 ID 생성
			if (string.IsNullOrEmpty(tileTypeId))
			{
				tileTypeId = $"{suit}_{rank}";
			}

			// 자동으로 이름 생성
			if (string.IsNullOrEmpty(displayName))
			{
				displayName = $"{suit} {rank}";
			}
		}
#endif
	}
}