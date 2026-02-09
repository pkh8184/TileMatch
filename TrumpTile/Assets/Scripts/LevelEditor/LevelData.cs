using UnityEngine;
using System;
using System.Collections.Generic;

namespace TileMatch.LevelEditor
{
    /// <summary>
    /// 레벨 데이터를 저장하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "Level_001", menuName = "TileMatch/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("레벨 기본 정보")]
        public int levelNumber = 1;
        public string levelName = "New Level";
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
        
        [Header("보드 설정")]
        public int boardWidth = 8;
        public int boardHeight = 8;
        public int maxLayers = 4;
        
        [Header("게임 규칙")]
        public int slotCount = 7;
        public int matchCount = 3;
        public float timeLimit = 0; // 0 = 무제한
        public int targetScore = 1000;
        
        [Header("타일 배치 데이터")]
        public List<TilePlacement> tilePlacements = new List<TilePlacement>();
        
        [Header("사용 가능한 타일 타입")]
        public List<TileTypeConfig> availableTileTypes = new List<TileTypeConfig>();
        
        [Header("아이템 설정")]
        public int initialShuffleCount = 3;
        public int initialUndoCount = 3;
        public int initialHintCount = 3;
        
        [Header("특수 설정")]
        public List<SpecialTileConfig> specialTiles = new List<SpecialTileConfig>();
        public List<ObstacleConfig> obstacles = new List<ObstacleConfig>();
        
        /// <summary>
        /// 레벨이 유효한지 검증
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = "";
            
            // 타일 수가 matchCount의 배수인지 확인
            if (tilePlacements.Count % matchCount != 0)
            {
                errorMessage = $"타일 수({tilePlacements.Count})가 {matchCount}의 배수가 아닙니다.";
                return false;
            }
            
            // 각 타일 타입별로 matchCount의 배수인지 확인
            var typeCount = new Dictionary<string, int>();
            foreach (var placement in tilePlacements)
            {
                if (!typeCount.ContainsKey(placement.tileTypeId))
                    typeCount[placement.tileTypeId] = 0;
                typeCount[placement.tileTypeId]++;
            }
            
            foreach (var kvp in typeCount)
            {
                if (kvp.Value % matchCount != 0)
                {
                    errorMessage = $"타일 '{kvp.Key}'의 개수({kvp.Value})가 {matchCount}의 배수가 아닙니다.";
                    return false;
                }
            }
            
            // 타일이 보드 범위 내에 있는지 확인
            foreach (var placement in tilePlacements)
            {
                if (placement.gridX < 0 || placement.gridX >= boardWidth ||
                    placement.gridY < 0 || placement.gridY >= boardHeight ||
                    placement.layer < 0 || placement.layer >= maxLayers)
                {
                    errorMessage = $"타일이 보드 범위를 벗어났습니다: ({placement.gridX}, {placement.gridY}, Layer {placement.layer})";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 레벨 통계 정보
        /// </summary>
        public LevelStatistics GetStatistics()
        {
            var stats = new LevelStatistics();
            stats.totalTiles = tilePlacements.Count;
            stats.uniqueTileTypes = new HashSet<string>();
            stats.tilesPerLayer = new int[maxLayers];
            
            foreach (var placement in tilePlacements)
            {
                stats.uniqueTileTypes.Add(placement.tileTypeId);
                if (placement.layer < maxLayers)
                    stats.tilesPerLayer[placement.layer]++;
            }
            
            return stats;
        }
    }
    
    /// <summary>
    /// 레벨 난이도
    /// </summary>
    public enum LevelDifficulty
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
        public int gridX;
        public int gridY;
        public int layer;
        public string tileTypeId;
        public bool isLocked;      // 잠금 타일 (특정 조건 후 해제)
        public bool isFrozen;      // 얼음 타일 (여러 번 클릭 필요)
        public int frozenCount;    // 얼음 두께
        
        public TilePlacement() { }
        
        public TilePlacement(int x, int y, int layer, string typeId)
        {
            this.gridX = x;
            this.gridY = y;
            this.layer = layer;
            this.tileTypeId = typeId;
        }
        
        public Vector3Int GridPosition => new Vector3Int(gridX, gridY, layer);
    }
    
    /// <summary>
    /// 타일 타입 설정 (Serializable - 레벨 데이터 내 저장용)
    /// </summary>
    [Serializable]
    public class TileTypeConfig
    {
        public string typeId;
        public CardSuit suit;
        public CardRank rank;
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
        public CardSuit suit;
        public CardRank rank;
        public Sprite sprite;
        public int weight = 1;
        
        public TileTypeConfig ToConfig()
        {
            return new TileTypeConfig
            {
                typeId = typeId,
                suit = suit,
                rank = rank,
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
        public SpecialTileType type;
        public int gridX;
        public int gridY;
        public int layer;
        public int value; // 타입별 추가 값
    }
    
    /// <summary>
    /// 특수 타일 종류
    /// </summary>
    public enum SpecialTileType
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
        public ObstacleType type;
        public int gridX;
        public int gridY;
        public int width = 1;
        public int height = 1;
    }
    
    /// <summary>
    /// 장애물 종류
    /// </summary>
    public enum ObstacleType
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
    
    /// <summary>
    /// 카드 무늬 (트럼프)
    /// </summary>
    public enum CardSuit
    {
        Spade,      // ♠
        Heart,      // ♥
        Diamond,    // ♦
        Club        // ♣
    }
    
    /// <summary>
    /// 카드 숫자
    /// </summary>
    public enum CardRank
    {
        Ace = 1, Two, Three, Four, Five, Six, Seven,
        Eight, Nine, Ten, Jack, Queen, King
    }
}
