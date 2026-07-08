using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrumpTile.LevelEditor.Editor;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 보드 관리자 (수정됨)
	///
	/// [추가된 기능]
	/// - GetLastPlacedTilePosition(): 마지막으로 배치된 타일 위치 반환
	/// </summary>
	public class BoardManager : MonoBehaviour
	{
		public static BoardManager Instance { get; private set; }

		#region Settings

		[Header("Grid Settings")]
		[SerializeField] private float mCellSize = 1F;
		[SerializeField] private float mOverlapThreshold = 0.8F;
		[Header("Tile Size By Board Max Size")]
		[SerializeField] [Tooltip("보드 최대 크기가 5 이하일 때 타일 사이즈")] private float mTileSize_5;
		[SerializeField] [Tooltip("보드 최대 크기가 6일 때 타일 사이즈")] private float mTileSize_6;
		[SerializeField] [Tooltip("보드 최대 크기가 7일 때 타일 사이즈")] private float mTileSize_7;
		[SerializeField] [Tooltip("보드 최대 크기가 8일 때 타일 사이즈")] private float mTileSize_8;
		private float mMaxBoardSize;
		private Vector3 mTileScale;

		[Header("References")]
		[SerializeField] private TileController mTilePrefab;
		[SerializeField] private List<TileData> mAllTileTypes;
		[SerializeField] private Sprite mDefaultTileBackground;
		[SerializeField] private Sprite[] mDifficultyTileBackgroundArray = new Sprite[3];
		[SerializeField] private TileData mBonusTileData;
		[Header("Spawn Animation")]	
		[SerializeField] private float mSpawnDelayPerTile = 0.02F;
		[SerializeField] private float mMaxSpawnDelay = 0.6F;

		[Header("Debug")]
		[SerializeField] private bool mEnableDebugLog = false;

		#endregion

		#region Private Fields

		[SerializeField] private List<TileController> mAllTiles = new List<TileController>();
		private Dictionary<Vector3, TileController> mTileGridMap = new Dictionary<Vector3, TileController>();
		private Dictionary<string, List<TileController>> mTileIdGroupMap = new Dictionary<string, List<TileController>>();

		private int mGridWidth;
		private int mOriginWidth;
		private int mGridHeight;
		private int mOriginHeight;
		private int mMaxLayers;

		private Vector3[,,] mBoardMap;

		private bool mIsShuffling = false;
		private bool mIsLevelLoaded = false;

		private int count;

		[Header("LayerList 기반으로 레벨 생성(기존 레벨들은 false로 해줘야 생성 가능)")]
		[SerializeField] private bool mbCreateTileByLayerList = false;
		[Header("보석 수집 타일 프리팹")]
		[SerializeField] private GameObject mGemTilePrefab;
		[Header("보석 수집 타일 최소 / 최대 개수")]
		[SerializeField] private int mGemTileMinCount = 1;
		[SerializeField] private int mGemTileMaxCount = 4;
		[Header("보석 수집 타일 하나당 보석 최소 / 최대 개수")]
		[SerializeField] private int mGemMinCount = 1;
		[SerializeField] private int mGemMaxCount = 4;
		private List<GemTile> mGemTileList = new List<GemTile>();
		private Vector3 mGemPos = Vector3.one * 10;
		private Vector3 mGemOverlapTilePos = Vector3.one * 100;
		// 마지막으로 배치된 타일 위치 저장
		private Vector3 mLastPlacedTilePosition = Vector3.zero;

		//랜덤 타일을 세트로 만들기 위한 변수
		private int mRandomTileCreateCount = 0;
		private TileData mCurrnetRandomTile;
		// 카테고리별로 랜덤 타일 풀을 분리해 저장 (같은 카테고리 placeholder끼리만 뽑히도록)
		private Dictionary<ETileCartegory, List<TileData>> mRandomTileMap = new Dictionary<ETileCartegory, List<TileData>>();

		private Dictionary<string, int> mCreatedTileMap = new Dictionary<string, int>();
		private List<TileController> mJewerlyTileList = new List<TileController>();
		#endregion

		#region Properties

		public int GridWidth => mGridWidth;
		public int GridHeight => mGridHeight;
		public int MaxLayers => mMaxLayers;
		public int TotalTileCount => mAllTiles.Count(t => t != null && !t.IsInSlot);
		public bool IsShuffling => mIsShuffling;
		public bool IsLevelLoaded => mIsLevelLoaded;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			// if (Instance != null && Instance != this)
			// {
			// 	Destroy(gameObject);
			// 	return;
			// }
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		#endregion

		#region Initialization
		public void LoadLevel(LevelData levelData)
		{
			if (levelData == null)
			{
				Debug.LogError("[BoardManager] LevelData is null!");
				return;
			}

			Log($"Loading level: {levelData.levelNumber}");

			mRandomTileMap.Clear();
			mJewerlyTileList.Clear();
			mCreatedTileMap.Clear();
			
			ClearBoard();

			mOriginWidth = levelData.boardWidth;
			mOriginHeight = levelData.boardHeight;
			mMaxLayers = levelData.maxLayers;

			float maxX = levelData.layerList.SelectMany(layer => layer.tilePlacementList).Max(x => x.gridX);
			float minX = levelData.layerList.SelectMany(layer => layer.tilePlacementList).Min(x => x.gridX);

			float maxY = levelData.layerList.SelectMany(layer => layer.tilePlacementList).Max(x => x.gridY);
			float minY = levelData.layerList.SelectMany(layer => layer.tilePlacementList).Min(x => x.gridY);

			float x = maxX - minX;
			float y = maxY - minY;

			int gridX = (int)x + 1;
			int gridY = (int)y + 1;
			
			mGridWidth = (int)maxX - (int)minX + 1;
			mGridHeight = (int)maxY - (int)minY + 1;

			mMaxBoardSize = Mathf.Max(gridX, gridY);			
			switch(mMaxBoardSize)
			{
				case 6:
					mTileScale = new Vector3(mTileSize_6, mTileSize_6, 1);
					break;
				case 7:
					mTileScale = new Vector3(mTileSize_7, mTileSize_7, 1);
					break;
				case 8:
					mTileScale = new Vector3(mTileSize_8, mTileSize_8, 1);
					break;
				default:
					mTileScale = new Vector3(mTileSize_5, mTileSize_5, 1);
					break;
			}

			mBoardMap = new Vector3[8,8,mMaxLayers];

			float startX = 0;
			if(mOriginWidth % 2 == 0)
			{
				startX -= mTileScale.x / 2;
				startX -= mTileScale.x * (mOriginWidth / 2 - 1);
			}
			else
			{
				startX -= mTileScale.x * (mOriginWidth / 2);
			}
			float startY = 0;
			if(mOriginHeight % 2 == 0)
			{
				startY -= mTileScale.x / 2;
				startY -= mTileScale.x * (mOriginHeight / 2 - 1);
			}
			else
			{
				startY -= mTileScale.x * (mOriginHeight / 2);
			}

			for (int i = 0; i < levelData.layerList.Count; i++)
            {
                foreach (TilePlacement placement in levelData.layerList[i].tilePlacementList)
                {
					float posX = startX + (mTileScale.x * placement.gridX);
					float posY = startY + (mTileScale.x * placement.gridY);
                    mBoardMap[(int)placement.gridX,(int)placement.gridY,i] = new Vector3(posX, posY, -0.1f * i);
				}
            }
			
			// string difficulty = levelData.difficulty.ToString();
			// Sprite difficultyBackground = null;
			// if(levelData.ChampionsLevel)
			// {
			// 	difficultyBackground = mDifficultyTileBackgroundArray[4];
			// }
			// else if(difficulty.Contains("Bonus"))
			// {
			// 	difficultyBackground = mDifficultyTileBackgroundArray[3];
			// }
			// else if(difficulty.Contains("VeryHard"))
			// {
			// 	difficultyBackground = mDifficultyTileBackgroundArray[2];
			// }
			// else if(difficulty.Contains("Hard"))
			// {
			// 	difficultyBackground = mDifficultyTileBackgroundArray[1];
			// }
			// else
			// {
			// 	difficultyBackground = mDifficultyTileBackgroundArray[0];
			// }

			// Sprite tileBackground = difficultyBackground ? difficultyBackground : mDefaultTileBackground;
			// if (DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
			// {
			// 	int dailyTileIndex = DailyPuzzleManager.Inst.GetTodayRandomIndex(mDifficultyTileBackgroundArray.Length);
			// 	Sprite dailyTileSprite = mDifficultyTileBackgroundArray[dailyTileIndex];
			// 	tileBackground = dailyTileSprite ? dailyTileSprite : mDefaultTileBackground;
			// }
			Sprite tileBackground;
			if(DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
			{
				tileBackground = GameManager.Instance.ResourceDatabase.GetTileBackgroundSprite(true);
			}
			else
			{
				tileBackground = GameManager.Instance.ResourceDatabase.GetTileBackgroundSprite(false);
			}

			SortingManager.SetMaxGridY(mGridHeight);

			Dictionary<string, TileData> tileDataMap = CreateTileDataMap();

			InitRandomTileList(levelData);

			ESpawnAnimType spawnAnimType = (ESpawnAnimType)Random.Range(0, TileController.SPAWN_ANIM_COUNT);

			int createdCount = 0;
			if(mbCreateTileByLayerList)
			{
                for (int i = 0; i < levelData.layerList.Count; i++)
                {
                    foreach (TilePlacement placement in levelData.layerList[i].tilePlacementList)
                    {
                        if (!tileDataMap.TryGetValue(placement.tileTypeId, out TileData data))
                        {
                            Debug.LogWarning($"[BoardManager] TileData not found: {placement.tileTypeId}");
                            continue;
                        }
                        CreateTile(data, placement.gridX, placement.gridY, i, tileBackground, createdCount, spawnAnimType, levelData.difficulty);
                        createdCount++;
                    }
                }
            }
			else
			{
                foreach (TilePlacement placement in levelData.tilePlacements)
                {
                    if (!tileDataMap.TryGetValue(placement.tileTypeId, out TileData data))
                    {
                        Debug.LogWarning($"[BoardManager] TileData not found: {placement.tileTypeId}");
                        continue;
                    }

                    CreateTile(data, placement.gridX, placement.gridY, placement.layer, tileBackground, createdCount, spawnAnimType);
                    createdCount++;
                }
            }
			
			// (0,0) 중심에서 가장 먼 타일부터 가까운 타일 순으로 스폰 애니메이션 재생
			PlaySpawnAnimationsByDistance(spawnAnimType);

			SetBonusTileRandom(levelData);
			SetJewerlyTileValid();
			UpdateAllBlockedStates();
			CreateGemTile();

			mIsLevelLoaded = true;
			Log($"Level loaded: {createdCount} tiles, Grid: {mGridWidth}x{mGridHeight}, Layers: {mMaxLayers}");
		}

		/// <summary>
		/// (0,0) 중심에서 가장 먼 타일부터 가까운 타일 순으로 스폰 애니메이션을 재생한다.
		/// 거리에 비례해 딜레이를 주어 바깥→안쪽 동심원 형태로 퍼지게 한다.
		/// </summary>
		private void PlaySpawnAnimationsByDistance(ESpawnAnimType animType)
		{
			float maxDistance = 0F;
			foreach (TileController tile in mAllTiles)
			{
				Vector3 pos = tile.transform.position;
				float distance = new Vector2(pos.x, pos.y).magnitude;
				if (distance > maxDistance)
				{
					maxDistance = distance;
				}
			}

			foreach (TileController tile in mAllTiles)
			{
				Vector3 pos = tile.transform.position;
				float distance = new Vector2(pos.x, pos.y).magnitude;
				float ratio = maxDistance > 0F ? distance / maxDistance : 0F;   // 0=중심, 1=최외곽
				float spawnDelay = (1F - ratio) * mMaxSpawnDelay;               // 최외곽=0(먼저), 중심=최대(나중)
				tile.PlaySpawnAnimation(spawnDelay, animType);
			}
		}

		private void CreateGemTile()
		{
			if(!GameManager.Instance.IsGemCollectActive)
			{
				return;
			}
			if(mGemTileList.Count > 0)
			{
				foreach(var item in mGemTileList)
				{
					Destroy(item.gameObject);
				}
			}
			mGemTileList.Clear();

			int createCount = Random.Range(mGemTileMinCount, mGemTileMaxCount + 1);

			List<TileController> validTiles = new List<TileController>();
			foreach(var item in mAllTiles)
			{
				if(item.GridX == mGridWidth - 1)
				{
					continue;
				}
				if(item.GridY == 0)
				{
					continue;
				}
				if(item.LayerIndex == 0)
				{
					continue;
				}

				validTiles.Add(item);
			}
			ShuffleList(validTiles);
			List<TileController> prevTileList = new List<TileController>();
			for(int i = 0; i < createCount; i++)
			{
				int gemCount = Random.Range(mGemMinCount, mGemMaxCount + 1);

				TileController tile;
				int add = 0;
				while(true)
				{   				
					if(i + add >= validTiles.Count)
					{
						Debug.Log("더이상 유효한 젬 타일을 만들 수 없습니다");
						return;
					}
					bool isOverlap = false;
					tile = validTiles[i + add];
					foreach(var item in prevTileList)
					{
						if(tile.LayerIndex == item.LayerIndex)
						{
							if(Mathf.Abs(tile.GridX - item.GridX) < 2 && Mathf.Abs(tile.GridY - item.GridY) < 2)
							{
								add++;
								isOverlap = true;
								break;
							}
						}
					}
					if(isOverlap)
					{
						continue;
					}
					break;
				}
				
				prevTileList.Add(tile);
				GemTile gem = Instantiate(mGemTilePrefab, transform).GetComponent<GemTile>();

				Vector3 gemPos = GridToWorldPosition(tile.GridX,tile.GridY,tile.LayerIndex);
				gemPos += Vector3.right * (mTileScale.x / 2);
				gemPos += Vector3.down * (mTileScale.y / 2);
				gemPos += Vector3.forward * 0.01f;

				Vector3 gemScale = mTileScale * 2 - Vector3.forward;

				List<(int,int,int)> checkList = new List<(int, int, int)>();
				List<(int,int,int)> originList = new List<(int, int, int)>();
				
				FindBlockTiles(tile.GridX, tile.GridY, tile.LayerIndex, checkList, originList);

				FindBlockTiles(tile.GridX + 1, tile.GridY, tile.LayerIndex, checkList, originList);

				FindBlockTiles(tile.GridX, tile.GridY - 1, tile.LayerIndex, checkList, originList);

				FindBlockTiles(tile.GridX + 1, tile.GridY - 1, tile.LayerIndex, checkList, originList);

				gem.Initialize(gemCount, checkList, originList, tile.GetLayerSortingOrder() - 18, gemPos, gemScale);

				mGemTileList.Add(gem);
				gem.gameObject.name += tile.LayerIndex;
			}

		}
		private void FindBlockTiles(int x, int y, int layerIndex, List<(int,int,int)> blockIndexList, List<(int,int,int)> originList)
		{
			originList.Add((x,y,layerIndex));
			if(mBoardMap[x,y,layerIndex] == Vector3.zero)
			{
				mBoardMap[x,y,layerIndex] = mGemPos;
			}
			else
			{
				mBoardMap[x,y,layerIndex] = mGemOverlapTilePos;
			}
			int tileX = x;
			int tileY = y;
			int layer = layerIndex;
					
			int rangeX = 0;
			if(mOriginWidth == 8)
			{
				if(layer % 2 == 0)
				{
					rangeX = -1;
				}
				else
				{
					rangeX = 1;
				}
			}
			else
			{
				if(layer % 2 == 0)
				{
					rangeX = 1;
				}
				else
				{
					rangeX = -1;
				}
			}
			int rangeY = 0;
			if(mOriginHeight == 8)
			{
				if(layer % 2 == 0)
				{
					rangeY = -1;
				}
				else
				{
					rangeY = 1;
				}
			}
			else
			{
				if(layer % 2 == 0)
				{
					rangeY = 1;
				}
				else
				{
					rangeY = -1;
				}
			}
			int count = 1;
			for(int i = layer + 1; i < MaxLayers; i++)
			{
				if(count % 2 == 0)
				{
					blockIndexList.Add((tileX, tileY, i));
				}
				else
				{
					blockIndexList.Add((tileX, tileY, i));
					if(tileX + rangeX >= 0 && tileX + rangeX < mOriginWidth)
					{
						blockIndexList.Add((tileX + rangeX, tileY, i));
					}
					if(tileY + rangeY >= 0 && tileY + rangeY < mOriginHeight)
					{
						blockIndexList.Add((tileX, tileY + rangeY, i));
					}
					if(tileX + rangeX >= 0 && tileX + rangeX < mOriginWidth)
					{
						if(tileY + rangeY >= 0 && tileY + rangeY < mOriginHeight)
						{
							blockIndexList.Add((tileX + rangeX, tileY + rangeY, i));
						}
					}
				}
				count++;
			}
		}	
		private void SetBonusTileRandom(LevelData level)
		{
			if(level.difficulty != EDifficultyType.Bonus)
			{
				return;
			}
			if(!level.createRandomBonusTile)
			{
				return;
			}
			
			List<int> random = new List<int>();
			for(int i = 0; i < mAllTiles.Count; i++)
			{
				random.Add(i);
			}
			for(int i = 0; i < level.bonusTileSet; i++)
			{
				int index = random[Random.Range(0, random.Count)];
				string id = mAllTiles[index].TileTypeId;
				List<TileController> targets = mAllTiles.Where(t => t != null && t.TileTypeId == id).Take(3).ToList();
				foreach(var item in targets)
				{
					item.SetTileToBonus(mBonusTileData);
					int j = mAllTiles.IndexOf(item);
					random.Remove(j);
				}
			}

		}

		private Dictionary<string, TileData> CreateTileDataMap()
		{
			Dictionary<string, TileData> map = new Dictionary<string, TileData>();
			if (mAllTileTypes == null)
			{
				return map;
			}

			foreach (TileData data in mAllTileTypes)
			{
				if (data != null && !string.IsNullOrEmpty(data.tileTypeId))
				{
					if (!map.ContainsKey(data.tileTypeId))
					{
						map[data.tileTypeId] = data;
					}
				}
			}
			return map;
		}

		private TileController CreateTile(TileData data, float x, float y, int layer, Sprite tileBackground, int spawnIndex = 0, ESpawnAnimType animType = ESpawnAnimType.Random, EDifficultyType  eDifficultyType = EDifficultyType.Easy_Normal)
		{
			if (mTilePrefab == null)
			{
				Debug.LogError("[BoardManager] Tile prefab is null!");
				return null;
			}
			data = GetFilteredTile(data);

			if(!mCreatedTileMap.ContainsKey(data.tileTypeId))
			{
				mCreatedTileMap[data.tileTypeId] = 0;
			}
			mCreatedTileMap[data.tileTypeId]++;
			count++;

			Vector3 position = GridToWorldPosition(x, y, layer);
			TileController tile = Instantiate(mTilePrefab, position, Quaternion.identity, transform);
			tile.name += count.ToString();
			tile.transform.localScale = mTileScale;
			tile.Initialize(data, x, y, layer, tileBackground, eDifficultyType);

			// 스폰 애니메이션은 전체 생성 후 PlaySpawnAnimationsByDistance 에서 거리순으로 재생한다.
			mAllTiles.Add(tile);
			if(data.tileTypeId == "Jewerly")
			{
				mJewerlyTileList.Add(tile);
			}

			Vector3 gridPos = new Vector3(x, y, layer);
			mTileGridMap[gridPos] = tile;

			if(!mTileIdGroupMap.ContainsKey(data.tileTypeId))
			{
				mTileIdGroupMap[data.tileTypeId] = new List<TileController>();
			}
			mTileIdGroupMap[data.tileTypeId].Add(tile);

			return tile;
		}
        private void InitRandomTileList(LevelData levelData)
        {
			for(int i = 0; i < (int)ETileCartegory.Length - 1; i++)
			{
				int count = levelData.GetRandomTileCount((ETileCartegory)i);
				Debug.Log($"[BoardManager] {(ETileCartegory)i} 랜덤 타일 개수 : {count}");
				if(count == 0) continue;
				int min = mAllTileTypes.FindIndex(x => x.tileTypeId.Contains(((ETileCartegory)i).ToString()));
				int max = mAllTileTypes.FindLastIndex(x => x.tileTypeId.Contains(((ETileCartegory)i).ToString()));
				
				// max 인덱스(카테고리 마지막 = Random 플레이스홀더 타일)는 선택 대상에서 제외
				int rangeCount = max - min;
				int typeCount = levelData.randomTileRangeByCartegory[i];
				Debug.Log($"[BoardManager] {(ETileCartegory)i} 랜덤 타일 범위 : {typeCount}");
				if(typeCount > rangeCount || typeCount <= 0)
				{
					typeCount = rangeCount;
				}

				// [min, max) 인덱스를 셔플해 앞에서 typeCount개를 중복 없이 추출
				// (이전 코드는 중복을 허용해 카테고리 타일이 적으면 1종류로 붕괴하는 버그가 있었음)
				List<int> indexPool = new List<int>();
				for(int j = min; j < max; j++)
				{
					indexPool.Add(j);
				}
				for(int j = indexPool.Count - 1; j > 0; j--)
				{
					int k = Random.Range(0, j + 1);
					(indexPool[j], indexPool[k]) = (indexPool[k], indexPool[j]);
				}

				List<TileData> filteredTileList = new List<TileData>();
				for(int j = 0; j < typeCount; j++)
				{
					TileData filteredTile = mAllTileTypes[indexPool[j]];
					Debug.Log($"[BoardManager] {j+1}번째 선택된 {(ETileCartegory)i} 랜덤 타일 : {filteredTile.TileID}");
					filteredTileList.Add(filteredTile);
				}

				// 이 카테고리 전용 풀에 담는다 (다른 카테고리와 섞이지 않도록)
				List<TileData> categoryTileList = new List<TileData>();
				mRandomTileMap[(ETileCartegory)i] = categoryTileList;

				TileData tileData = filteredTileList[Random.Range(0, typeCount)];

				for(int j = 0; j < count / 3; j++)
				{
					for(int k = 0; k < 3; k++)
					{
						categoryTileList.Add(tileData);
					}
					tileData = filteredTileList[Random.Range(0, typeCount)];
				}
				for(int j = 0; j < count % 3; j++)
				{
					categoryTileList.Add(tileData);
				}
			}
        }
        private TileData GetFilteredTile(TileData data)
		{
			if(!data.tileTypeId.Contains("Random"))
			{
				return data;
			}

			// placeholder의 카테고리를 파싱해 같은 카테고리 풀에서만 뽑는다
			ETileCartegory category = GetRandomTileCartegory(data.tileTypeId);
			if(category == ETileCartegory.Length
				|| !mRandomTileMap.TryGetValue(category, out List<TileData> pool)
				|| pool.Count == 0)
			{
				Debug.LogWarning($"[BoardManager] 랜덤 타일 풀이 비어있음: {data.tileTypeId}");
				return data;
			}

			int randomIndex = Random.Range(0, pool.Count);
			TileData tile = pool[randomIndex];
			pool.RemoveAt(randomIndex);

			return tile;
		}
		/// <summary>
		/// 랜덤 placeholder의 tileTypeId(예: "Fruit_Random")에서 카테고리를 파싱한다.
		/// 없으면 ETileCartegory.Length 반환.
		/// </summary>
		private ETileCartegory GetRandomTileCartegory(string tileTypeId)
		{
			for(int i = 0; i < (int)ETileCartegory.Length - 1; i++)
			{
				if(tileTypeId.Contains(((ETileCartegory)i).ToString()))
				{
					return (ETileCartegory)i;
				}
			}
			return ETileCartegory.Length;
		}
		private void SetJewerlyTileValid()
		{
			for (int i = mJewerlyTileList.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				(mJewerlyTileList[i], mJewerlyTileList[j]) = (mJewerlyTileList[j], mJewerlyTileList[i]);
			}

			foreach(var item in mCreatedTileMap)
			{
				if(item.Key == "Jewerly")
				{
					continue;
				}
				
				int count = item.Value % 3;
				if(count == 0)
				{
					continue;
				}
				TileData data = mAllTileTypes.Find(x => x.tileTypeId == item.Key);
				Debug.Log($"[BoardManager] 3의 배수가 아닌 타일 : {item.Key}");

				Debug.Log($"[BoardManager] 모자란 숫자 : {3 - count}");

				for(int i = 0; i < 3 - count; i++)
				{
					Debug.Log($"[BoardManager] Change Jewerly To {item.Key}, Current Jewerly Index : {mJewerlyTileList.Count - 1}");
					mJewerlyTileList[mJewerlyTileList.Count - 1].SetTileDataOnly(data);
					mJewerlyTileList.RemoveAt(mJewerlyTileList.Count - 1);
				}

				if(mJewerlyTileList.Count == 0) break;
			}
		}
		#endregion

		#region Blocked State

		public bool IsCoveredByGem(TileController tile)
		{
			if (tile == null)
			{
				return false;
			}
			return mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] == mGemPos;
		}

		public bool IsTileBlocked(TileController tile)
		{
			if (tile == null)
			{
				return false;
			}
			if (tile.IsInSlot)
			{
				return false;
			}
			if (IsCoveredByGem(tile))
			{
				return true;
			}

			int tileX = tile.GridX;
			int tileY = tile.GridY;
			int layer = tile.LayerIndex;
			
			int rangeX = 0;
			if(mOriginWidth == 8)
			{
				if(layer % 2 == 0)
				{
					rangeX = -1;
				}
				else
				{
					rangeX = 1;
				}
			}
			else
			{
				if(layer % 2 == 0)
				{
					rangeX = 1;
				}
				else
				{
					rangeX = -1;
				}
			}
			int rangeY = 0;
			if(mOriginHeight == 8)
			{
				if(layer % 2 == 0)
				{
					rangeY = -1;
				}
				else
				{
					rangeY = 1;
				}
			}
			else
			{
				if(layer % 2 == 0)
				{
					rangeY = 1;
				}
				else
				{
					rangeY = -1;
				}
			}
			int count = 1;
			for(int i = layer + 1; i < MaxLayers; i++)
			{
				if(count % 2 == 0)
				{
					if(mBoardMap[tileX, tileY, i] != Vector3.zero)
					{
						return true;
					}
				}
				else
				{
					if(mBoardMap[tileX, tileY, i] != Vector3.zero)
					{
						return true;
					}
					if(tileX + rangeX >= 0 && tileX + rangeX < mOriginWidth)
					{
						if(mBoardMap[tileX + rangeX, tileY, i] != Vector3.zero)
						{
							return true;
						}
					}
					if(tileY + rangeY >= 0 && tileY + rangeY < mOriginHeight)
					{
						if(mBoardMap[tileX, tileY + rangeY, i] != Vector3.zero)
						{
							return true;
						}
					}
					if(tileX + rangeX >= 0 && tileX + rangeX < mOriginWidth)
					{
						if(tileY + rangeY >= 0 && tileY + rangeY < mOriginHeight)
						{
							if(mBoardMap[tileX + rangeX, tileY + rangeY, i] != Vector3.zero)
							{
								return true;
							}
						}
					}
				}
				count++;
			}
			// int tileX = tile.GridX;
			// int tileY = tile.GridY;
			// int tileLayer = tile.LayerIndex;
			// Vector3 tilePos = tile.transform.position;

			// float checkRadius = mCellSize * mOverlapThreshold;

			// foreach (TileController other in mAllTiles)
			// {
			// 	if (other == null || other == tile || other.IsInSlot)
			// 	{
			// 		continue;
			// 	}
			// 	if (other.LayerIndex <= tileLayer)
			// 	{
			// 		continue;
			// 	}
			// 	Vector3 otherPos = other.transform.position;

			// 	float dx = Mathf.Abs(otherPos.x - tilePos.x);
			// 	float dy = Mathf.Abs(otherPos.y - tilePos.y);

			// 	if (dx < checkRadius && dy < checkRadius)
			// 	{
			// 		Debug.Log($"{tile.name } 타일 상호작용 불가, {other.name} 타일이 위에 있음");
			// 		return true;
			// 	}
			// }

			return false;
		}

		public void UpdateAllBlockedStates()
		{
			foreach(var item in mGemTileList)
			{
				if(!item.gameObject.activeSelf)
				{
					continue;
				}
				item.CheckCanCollect();
			}

			foreach (TileController tile in mAllTiles)
			{
				if (tile != null && !tile.IsInSlot)
				{
					bool bBlocked = IsTileBlocked(tile);
					tile.SetSelectable(!bBlocked);
				}
			}
		}

		#endregion

		#region Remove Tile

		public void RemoveTileFromBoard(TileController tile, bool bProcessBonus = true)
		{
			if (tile == null)
			{
				return;
			}

			mAllTiles.Remove(tile);

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap.Remove(gridPos);

			if(mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] == mGemOverlapTilePos)
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = mGemPos;
			}
			else
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = Vector3.zero;
			}

			mTileIdGroupMap[tile.TileTypeId].Remove(tile);
			
			UpdateAllBlockedStates();
			
			Log($"Tile removed from board: {tile.TileTypeId}");
		}
		public void RemoveTileFromBoardBeforeAdd(TileController tile, bool bProcessBonus = true)
		{
			if (tile == null)
			{
				return;
			}

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap.Remove(gridPos);
			
			if(mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] == mGemOverlapTilePos)
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = mGemPos;
			}
			else
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = Vector3.zero;
			}
			
			mTileIdGroupMap[tile.TileTypeId].Remove(tile);
			
			UpdateAllBlockedStates();

			Log($"Tile removed from board: {tile.TileTypeId}");
		}

		#endregion

		#region Place Tile

		public void ReturnTileToBoard(TileController tile, int origX, int origY, int origLayer)
		{
			if (tile == null)
			{
				return;
			}

			Vector3Int origGridPos = new Vector3Int(origX, origY, origLayer);

			if(mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] == mGemPos)
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = mGemOverlapTilePos;
			}
			else
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = Vector3.one;
			}

			if (!mTileGridMap.ContainsKey(origGridPos))
			{
				PlaceTileAt(tile, origX, origY, origLayer);
				return;
			}

			PlaceTileOnOriginSpot(tile, bWithSpin: false);
		}

		public bool PlaceTileOnOriginSpot(TileController tile, bool bWithSpin = false)
		{
			if (tile == null)
			{
				return false;
			}

			PlaceTileAtOrigin(tile, bWithSpin);

			return true;
		}

		private Vector2Int? FindEmptyPositionOnLayer(int layer)
		{
			HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

			foreach (TileController tile in mAllTiles)
			{
				if (tile == null || tile.IsInSlot)
				{
					continue;
				}

				if (tile.LayerIndex >= layer)
				{
					for (int dx = -1; dx <= 1; dx++)
					{
						for (int dy = -1; dy <= 1; dy++)
						{
							occupiedPositions.Add(new Vector2Int(tile.GridX + dx, tile.GridY + dy));
						}
					}
				}
			}

			int centerX = mGridWidth / 2;
			int centerY = mGridHeight / 2;

			for (int radius = 0; radius <= Mathf.Max(mGridWidth, mGridHeight); radius++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
						{
							continue;
						}

						int x = centerX + dx;
						int y = centerY + dy;

						if (x < 0 || x >= mGridWidth || y < 0 || y >= mGridHeight)
						{
							continue;
						}

						Vector2Int pos = new Vector2Int(x, y);
						if (!occupiedPositions.Contains(pos))
						{
							Vector3Int gridPos = new Vector3Int(x, y, layer);
							if (!mTileGridMap.ContainsKey(gridPos))
							{
								return pos;
							}
						}
					}
				}
			}

			return null;
		}
		private void PlaceTileAtOrigin(TileController tile, bool bWithSpin = false)
		{
			if(bWithSpin)
			{
				tile.FlyToBoard();
			}
			else
			{
				tile.ReturnToBoard();
			}
			if (!mAllTiles.Contains(tile))
			{
				mAllTiles.Add(tile);
			}

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap[gridPos] = tile;
			
			if(mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] == mGemPos)
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = mGemOverlapTilePos;
			}
			else
			{
				mBoardMap[tile.GridX, tile.GridY, tile.LayerIndex] = Vector3.one;
			}

			mLastPlacedTilePosition = tile.transform.position;

			UpdateAllBlockedStates();
		}
		private void PlaceTileAt(TileController tile, int x, int y, int layer, bool bWithSpin = false)
		{
			Vector3 position = GridToWorldPosition(x, y, layer);

			if (bWithSpin)
			{
				//tile.FlyToBoard(position, x, y, layer);
				tile.FlyToBoard();
			}
			else
			{
				//tile.ReturnToBoard(position, x, y, layer);
				tile.ReturnToBoard();
			}

			if (!mAllTiles.Contains(tile))
			{
				mAllTiles.Add(tile);
			}

			//Vector3Int gridPos = new Vector3Int(x, y, layer);
			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap[gridPos] = tile;

			mLastPlacedTilePosition = tile.transform.position;

			UpdateAllBlockedStates();
		}

		/// <summary>
		/// 마지막으로 배치된 타일의 위치 반환
		/// </summary>
		public Vector3 GetLastPlacedTilePosition()
		{
			return mLastPlacedTilePosition;
		}

		#endregion

		#region Shuffle

		public IEnumerator ShuffleBoardAnimated()
		{
			if (mIsShuffling)
			{
				yield break;
			}
			mIsShuffling = true;

			List<TileController> boardTiles = mAllTiles
				.Where(t => t != null && !t.IsInSlot)
				.ToList();

			Log($"Shuffling {boardTiles.Count} tiles");

			if (boardTiles.Count <= 1)
			{
				mIsShuffling = false;
				yield break;
			}

			foreach (TileController tile in boardTiles)
			{
				tile.SetSelectable(false);
			}

			List<TileData> dataList = boardTiles
				.Where(t => t.Data != null)
				.Select(t => t.Data)
				.ToList();

			ShuffleList(dataList);

			yield return null;

			mTileIdGroupMap.Clear();
			for (int i = 0; i < boardTiles.Count && i < dataList.Count; i++)
			{
				boardTiles[i].SetTileData(dataList[i]);
				boardTiles[i].transform.rotation = Quaternion.identity;
				if(!mTileIdGroupMap.ContainsKey(boardTiles[i].TileTypeId))
				{
					mTileIdGroupMap[boardTiles[i].TileTypeId] = new List<TileController>();
				}
				mTileIdGroupMap[boardTiles[i].TileTypeId].Add(boardTiles[i]);
			}

			UpdateAllBlockedStates();

			mIsShuffling = false;
		}

		private IEnumerator ShuffleAnimation(List<TileController> tiles)
		{
			float duration = GameRules.SHUFFLE_DURATION;
			float elapsed = 0F;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;
				float angle = Mathf.Lerp(0F, 360F, t);

				foreach (TileController tile in tiles)
				{
					if (tile != null)
					{
						tile.transform.rotation = Quaternion.Euler(0, 0, angle);
					}
				}
				yield return null;
			}
		}

		public void ShuffleBoard()
		{
			StartCoroutine(ShuffleBoardAnimated());
		}

		#endregion

		#region Remove Match Sets

		public int RemoveRandomMatchSets(int setCount)
		{
			int removedSets = 0;
			int matchCount = GameRules.MATCH_COUNT;

			for (int i = 0; i < setCount; i++)
			{
				List<TileController> selectableTiles = mAllTiles
					.Where(t => t != null && !t.IsInSlot && !IsTileBlocked(t) && t.Data != null)
					.ToList();

				List<IGrouping<string, TileController>> groups = selectableTiles
					.GroupBy(t => t.Data.TileID)
					.Where(g => g.Count() >= matchCount)
					.ToList();

				if (groups.Count == 0)
				{
					break;
				}

				IGrouping<string, TileController> randomGroup = groups[Random.Range(0, groups.Count)];
				List<TileController> tilesToRemove = randomGroup.Take(matchCount).ToList();

				foreach (TileController tile in tilesToRemove)
				{
					RemoveTileFromBoard(tile, bProcessBonus: false);
					tile.Remove();
				}

				removedSets++;
			}

			Log($"Removed {removedSets} match sets");
			return removedSets;
		}

		#endregion

		#region Utility
		public bool CheckBoardMapEmpty(List<(int,int,int)> checkList, List<(int,int,int)> originList, GemTile self)
		{
			foreach(var item in originList)
			{
				int x = item.Item1;
				int y = item.Item2;
				int layer = item.Item3;

				if(mBoardMap[x,y,layer] != Vector3.zero && mBoardMap[x,y,layer] != mGemPos)
				{
					Debug.Log($"오리진 인덱스에 아직 타일 있음({x},{y},{layer})");
					return false;
				}
			}
			foreach(var item in checkList)
			{
				int x = item.Item1;
				int y = item.Item2;
				int layer = item.Item3;
				
				if(mBoardMap[x,y,layer] != Vector3.zero)
				{
					return false;
				}
			}
			foreach(var item in originList)
			{
				int x = item.Item1;
				int y = item.Item2;
				int layer = item.Item3;

				mBoardMap[x,y,layer] = Vector3.zero;
			}
			return true;
		}
		public Vector3 GridToWorldPosition(float x, float y, int layer)
		{
			float offsetX = 0;
			float offsetY = 0;
			if(layer % 2 == 1)
			{
				if(mOriginWidth < 8)
				{
					offsetX -= mTileScale.x / 2f;
				}
				else
				{
					offsetX = mTileScale.x / 2f;
				}
				if(mOriginHeight < 8)
				{
					offsetY -= mTileScale.x / 2f;
				}
				else
				{
					offsetY = mTileScale.x / 2f;
				}
			}
			Vector3 offsetPos = new Vector3(offsetX, offsetY + 0.05f * (layer / 2), 0);
			return mBoardMap[(int)x,(int)y,layer] + offsetPos;
		}

		/// <summary>
		/// 레이어별 오프셋 반환
		/// </summary>
		private Vector2 GetLayerOffset(int layer)
		{
			float offsetX = 0;
			float offsetY = 0;
			if(layer % 2 == 1)
			{
				if(mOriginWidth < 8)
				{
					offsetX = -0.3f;
				}
				else
				{
					offsetX = 0.3f;
                }
				if(mOriginHeight < 8)
				{
					offsetY = -0.3f;
				}
				else
				{
					offsetY = 0.3f;
                }
			}
			return new Vector2(offsetX, offsetY);
		}

		private void ClearBoard()
		{
			foreach (TileController tile in mAllTiles)
			{
				if (tile != null)
				{
					Destroy(tile.gameObject);
				}
			}

			mAllTiles.Clear();
			mTileGridMap.Clear();
			mTileIdGroupMap.Clear();
			mIsLevelLoaded = false;
			mIsShuffling = false;
			mLastPlacedTilePosition = Vector3.zero;
		}

		private void ShuffleList<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				T temp = list[i];
				list[i] = list[j];
				list[j] = temp;
			}
		}

		private void Log(string message)
		{
			if (mEnableDebugLog)
			{
				Debug.Log($"[BoardManager] {message}");
			}
		}

		#endregion

		#region Public Getters

		public bool HasRemainingTiles()
		{
			return mAllTiles.Any(t => t != null && !t.IsInSlot);
		}

		public List<TileController> GetAllTiles()
		{
			return mAllTiles;
		}

		public List<TileController> GetBoardTiles()
		{
			return mAllTiles.Where(t => t != null && !t.IsInSlot).ToList();
		}
		public List<TileController> GetBoardTilesContainSlot()
		{
			return mAllTiles.Where(t => t != null).ToList();
		}
		public void SetSize(int width, int height)
		{
			mGridWidth = width;
			mGridHeight = height;
		}

        #endregion

        #region Bonus Tile
		/// <summary>
		/// 보너스 타일에 대한 처리
		/// </summary>
		/// <param name="tile"></param>
		private void ProcessBonusTile(TileController tile)
		{
            if (tile.TileTypeId == "Bonus")
            {
                //AudioManager.Inst?.PlayBonus();
                //03.26 곽원준 : 플레이어 골드 증가시켜야함 -> 스테이지 종료 데이터 만들 필요 있음
                Destroy(tile.gameObject);
                Debug.Log("[BoardManager] Remove Bonus Tile ");
            }
        }
        #endregion

		public void ShowHint()
		{
			if(SlotManager.Instance.GetAllSlotTiles().Count == 0)
			{
				ShowHintOnBoard();
			}
			else
			{
				ShowHintOnBoardWithSlot();
			}
		}
		private void ShowHintOnBoard()
		{
			List<TileController> hintList = new List<TileController>();
			for(int i = mAllTiles.Count - 1; i >= 0; i--)
			{
				TileController tile = mAllTiles[i];
				if(tile.IsInSlot || IsTileBlocked(tile))
				{
					continue;
				}
				
				hintList = GetHintTileList(mAllTiles[i], 2);
				if(hintList == null || hintList.Count < 2)
				{
					continue;
				}
				
				hintList.Add(mAllTiles[i]);
				break;
			}
			if(hintList == null || hintList.Count < 2)
			{
				GameManager.Instance.IsIdleProcess = false;
				return;
			}

			//힌트 타일(전부 보드 타일)을 슬롯에 다 담을 수 없으면 오버플로우로 게임오버되므로 힌트를 숨긴다.
			if(!CanFitHintInSlot(hintList.Count))
			{
				GameManager.Instance.IsIdleProcess = false;
				return;
			}

			foreach(var item in hintList)
			{
				item.ShowHint();
			}
			GameManager.Instance.IsIdleProcess = false;
		}
		private void ShowHintOnBoardWithSlot()
		{
			List<TileController> hintList = new List<TileController>();
			List<TileController> slotList = SlotManager.Instance.GetAllSlotTiles();
			for(int i = 0; i < slotList.Count; i++)
			{
				TileController tile = slotList[i];

				int findCount = 2;

				if(i + 1 < slotList.Count)
				{
					if(tile.TileTypeId == slotList[i + 1].TileTypeId)
					{
						findCount = 1;
					}
				}
				hintList = GetHintTileList(tile, findCount);
				if(hintList == null || hintList.Count < findCount)
				{
					continue;
				}

				//보드 타일(findCount개)을 슬롯에 다 담을 수 없는 후보는 스킵(오버플로우 방지).
				if(!CanFitHintInSlot(findCount))
				{
					continue;
				}

				for(int j = i; j < i + (3 - findCount); j++)
				{
					hintList.Add(slotList[j]);
				}
				break;
			}

			if(hintList.Count >= 3)
			{
				foreach(var item in hintList)
				{
					item.ShowHint();
				}
				GameManager.Instance.IsIdleProcess = false;
				return;
			}
			
			ShowHintOnBoard();
		}
		private List<TileController> GetHintTileList(TileController tile, int findCount)
		{
			string id = tile.TileTypeId;

			if(!mTileIdGroupMap.ContainsKey(id))
			{
				return null;
			}

			List<TileController> value = new List<TileController>();

			int count = 0;
			foreach(var item in mTileIdGroupMap[id])
			{
				if(item == null)
				{
					continue;
				}
				if(item == tile)
				{
					continue;
				}
				if(!IsTileBlocked(item))
				{
					count++;
					value.Add(item);
					if(count >= findCount)
					{
						break;
					}
				}
			}

			return value;
		}
		//힌트로 보드 타일 boardTileCount개를 슬롯에 넣어 3매치를 완성할 때,
		//피크 점유량(현재 슬롯 수 + 추가 타일 수)이 최대 칸수를 넘으면 매치 완성 전에 오버플로우 → 게임오버.
		//넘지 않을 때만 힌트를 표시한다.
		private bool CanFitHintInSlot(int boardTileCount)
		{
			int currentSlotCount = SlotManager.Instance.GetAllSlotTiles().Count;
			return currentSlotCount + boardTileCount <= SlotManager.Instance.MaxSlots;
		}
    }
}
