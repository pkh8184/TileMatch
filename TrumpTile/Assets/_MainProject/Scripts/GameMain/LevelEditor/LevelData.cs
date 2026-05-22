using UnityEngine;
using System;
using System.Collections.Generic;
using TrumpTile.GameMain.Core;

namespace TrumpTile.LevelEditor.Editor
{
	public enum EDifficultyType
	{
		Tutorial = 0,
		Easy_Normal,
		Easy_Hard,
		Easy_VeryHard,
		Normal,
		Hard,
		VeryHard,
		Length
	}
	
	[Serializable]
	public class LayerDataWrapper
	{
		public List<TilePlacement> tilePlacementList = new List<TilePlacement>();
    }
    /// <summary>
    /// 레벨 데이터를 저장하는 ScriptableObject
    /// </summary>
    [System.Serializable]
	[CreateAssetMenu(fileName = "Level_001", menuName = "TileMatch/Level Data")]
	public class LevelData : ScriptableObject
	{
		[Header("레벨 기본 정보")]
		public int levelNumber = 1;
		public string levelName = "New Level";
		public EDifficultyType difficulty = EDifficultyType.Easy_Normal;


		[Header("보드 설정")]
		public int boardWidth = 8;
		public int boardHeight = 8;
		public int maxLayers = 1;

		[Header("게임 규칙")]
		public int slotCount = 7;
		public int matchCount = 3;
        //public float timeLimit = 0; // 0 = 무제한
        //public int targetScore = 1000;

        [Header("타일 배치 데이터")]
		public List<LayerDataWrapper> layerList = new List<LayerDataWrapper>();
		public List<TilePlacement> tilePlacements = new();

		[Header("사용 가능한 타일 타입")]
		public List<TileTypeConfig> availableTileTypes = new();

		[Header("레벨에 적용될 배경 데이터")]
		public Sprite levelBackgroundSprite;
		public Sprite tileBackgroundSprite;

		[Header("아이템 설정")]
		public int initialShuffleCount = 3;
		public int initialUndoCount = 3;
		public int initialHintCount = 3;

		[Header("특수 설정")]
		public List<SpecialTileConfig> specialTiles = new();
		public List<ObstacleConfig> obstacles = new();

		/// <summary>
		/// 레벨이 유효한지 검증
		/// </summary>
		public bool Validate(out string errorMessage, out List<string> idList)
		{
			errorMessage = "";
			idList = new List<string>();

            bool isValidate = true;

            // 타일 수가 matchCount의 배수인지 확인
            if (GetTileCountOnlyMatchable() % matchCount != 0)
			{
				errorMessage = $"총 타일 수({GetTileCountOnlyMatchable()})가 {matchCount}의 배수가 아닙니다.\n";
				isValidate = false;
			}

			// 각 타일 타입별로 matchCount의 배수인지 확인
			Dictionary<string, int> typeCount = new Dictionary<string, int>();
			foreach (LayerDataWrapper layerWrapper in layerList)
			{
				foreach (TilePlacement placement in layerWrapper.tilePlacementList)
				{
					if(placement.tileTypeId == "Bonus" || placement.tileTypeId == "Jewerly")
					{
						continue; // 보너스 타일은 매치용 타일에서 제외
					}
					if (!typeCount.ContainsKey(placement.tileTypeId))
					{
						typeCount[placement.tileTypeId] = 0;
					}
					typeCount[placement.tileTypeId]++;
				}
			}
			int jewerlyCount = GetJewerlyTileCount();

            foreach (KeyValuePair<string, int> kvp in typeCount)
			{
				int tileCount = kvp.Value;
                if (tileCount % matchCount != 0)
				{
					while(tileCount % matchCount != 0)
					{
						if(jewerlyCount <= 0)
						{
                            errorMessage += $"타일 '{kvp.Key}'의 개수({kvp.Value})가 {matchCount}의 배수가 아닙니다.\n";
                            idList.Add(kvp.Key);
                            isValidate = false;
                            break;
						}
						tileCount++;
						jewerlyCount--;
					}
				}
			}
			if(jewerlyCount > 0)
			{
				errorMessage += $"보석 타일이 {jewerlyCount}개 남았습니다.\n";
                idList.Add("Jewerly");
                isValidate = false;
			}

			// 타일이 보드 범위 내에 있는지 확인
			for(int i = 0; i < layerList.Count; i++)
			{
                foreach (TilePlacement placement in layerList[i].tilePlacementList)
                {
                    if (placement.gridX < 0 || placement.gridX > boardWidth ||
                        placement.gridY < 0 || placement.gridY > boardHeight)
                    {
                        errorMessage += $"타일이 보드 범위를 벗어났습니다: ({placement.gridX}, {placement.gridY}, Layer {i})\n";
						isValidate = false;
                    }
                }
            }
				return isValidate;
			}
		
		/// <summary>
		/// 레벨 통계 정보
		/// </summary>
		//public LevelStatistics GetStatistics()
		//{
		//	LevelStatistics stats = new()
		//	{
				
		//		totalTiles = GetTileCount(),
		//		uniqueTileTypes = new HashSet<string>(),
		//		tilesPerLayer = new int[maxLayers]
		//	};
  //          for (int i = 0; i < layerList.Count; i++)
  //          {

  //          }
  //          foreach (LayerDataWrapper layerWrapper in layerList)
		//	{
		//		foreach (TilePlacement placement in layerWrapper.tilePlacementList)
		//		{
		//			stats.uniqueTileTypes.Add(placement.tileTypeId);
		//			if (placement.layer < maxLayers)
		//			{
		//				stats.tilesPerLayer[placement.layer]++;
		//			}
		//		}
		//	}

  //          return stats;
  //      }
	
		public LevelData Clone()
		{
            LevelData clone = ScriptableObject.CreateInstance<LevelData>();
            string json = JsonUtility.ToJson(this);
            JsonUtility.FromJsonOverwrite(json, clone);
            return clone;
        }
		/// <summary>
		/// 보너스 타일을 제외한 매치용 타일들 개수만 반환
		/// </summary>
		/// <returns></returns>
		public int GetTileCount()
		{
			int count = 0;
			foreach (LayerDataWrapper layerWrapper in layerList)
			{
				count += layerWrapper.tilePlacementList.Count;
			}
			return count;
		}
		public int GetTileCountOnlyMatchable()
		{
            int count = 0;
            foreach (LayerDataWrapper layerWrapper in layerList)
            {
                int without = 0;
                foreach (TilePlacement placement in layerWrapper.tilePlacementList)
                {
                    if (placement.tileTypeId == "Bonus")
                    {
                        without++;
                    }
                }
                count += layerWrapper.tilePlacementList.Count - without;
            }
            return count;
        }
		public int GetBonusTileCount()
		{
            int count = 0;
            foreach (LayerDataWrapper layerWrapper in layerList)
            {
                foreach (TilePlacement placement in layerWrapper.tilePlacementList)
                {
                    if (placement.tileTypeId == "Bonus")
                    {
                        count++;
                    }
                }
            }
            return count;
        }
		public int GetJewerlyTileCount()
		{
            int count = 0;
            foreach (LayerDataWrapper layerWrapper in layerList)
            {
                foreach (TilePlacement placement in layerWrapper.tilePlacementList)
                {
                    if (placement.tileTypeId == "Jewerly")
                    {
                        count++;
                    }
                }
            }
            return count;
        }
		public int GetRandomTileCount(ETileCartegory eTileCartegory)
		{
			int count = 0;
			foreach(var wrapper in layerList)
			{
				foreach(var tile in wrapper.tilePlacementList)
				{
					if(tile.tileTypeId.Contains("Random"))
					{
						if(tile.tileTypeId.Contains(eTileCartegory.ToString()))
						{
							count++;
						}
					}
				}
			}
			return count;
		}
    }

	/// <summary>
	/// 레벨 난이도
	/// </summary>
	public enum ELevelDifficulty
	{
		Tutorial,
		Easy,
		Normal,
		Hard,
		Expert
	}

	/// <summary>
	/// 개별 타일 배치 정보
	/// </summary>
	[Serializable]
	public class TilePlacement
	{
		public float gridX;
		public float gridY;
		public int layer;
		public string tileTypeId;
		public bool isLocked;      // 잠금 타일 (특정 조건 후 해제)
		public bool isFrozen;      // 얼음 타일 (여러 번 클릭 필요)
		public int frozenCount;    // 얼음 두께

		public TilePlacement() { }

		public TilePlacement(float x, float y, int layer, string typeId)
		{
			this.gridX = x;
			this.gridY = y;
			this.tileTypeId = typeId;
		}

		public Vector3 GridPosition => new Vector3(gridX, gridY);
	}

	/// <summary>
	/// 타일 타입 설정 (Serializable - 레벨 데이터 내 저장용)
	/// </summary>
	[Serializable]
	public class TileTypeConfig
	{
		public string typeId;
		public ECardSuit suit;
		public ECardRank rank;
		public ETileCartegory tileCartegoty;
		public Sprite sprite;
		public int weight = 1; // 출현 확률 가중치
	}

	/// <summary>
	/// 타일 타입 데이터 (ScriptableObject - 에셋 저장용)
	/// </summary>
	[CreateAssetMenu(fileName = "NewTileType", menuName = "TileMatch/Tile Type Data")]
	public class TileTypeData : ScriptableObject
	{
		public string typeId;
		public ECardRank rank;
		public ECardSuit suit;
		public Sprite sprite;
		public int weight = 1;

		public TileTypeConfig ToConfig()
		{
			return new TileTypeConfig
			{
				typeId = typeId,
				rank = rank,	
				suit = suit,
				sprite = sprite,
				weight = weight
			};
		}
	}

	/// <summary>
	/// 특수 타일 설정
	/// </summary>
	[Serializable]
	public class SpecialTileConfig
	{
		public ESpecialTileType type;
		public int gridX;
		public int gridY;
		public int layer;
		public int value; // 타입별 추가 값
	}

	/// <summary>
	/// 특수 타일 종류
	/// </summary>
	public enum ESpecialTileType
	{
		Frozen,     // 얼음 (여러 번 탭 필요)
		Locked,     // 잠금 (조건 해제)
		Bomb,       // 폭탄 (주변 타일 제거)
		Rainbow,    // 무지개 (모든 타일과 매치)
		Double,     // 더블 (2개로 카운트)
		Chain       // 체인 (연결된 타일 동시 제거)
	}

	/// <summary>
	/// 장애물 설정
	/// </summary>
	[Serializable]
	public class ObstacleConfig
	{
		public EObstacleType type;
		public int gridX;
		public int gridY;
		public int width = 1;
		public int height = 1;
	}

	/// <summary>
	/// 장애물 종류
	/// </summary>
	public enum EObstacleType
	{
		Block,      // 빈 공간 (타일 배치 불가)
		Wall,       // 벽 (시각적 구분)
		Portal      // 포탈 (타일 이동)
	}

	/// <summary>
	/// 레벨 통계
	/// </summary>
	public class LevelStatistics
	{
		public int totalTiles;
		public HashSet<string> uniqueTileTypes;
		public int[] tilesPerLayer;
	}

}
