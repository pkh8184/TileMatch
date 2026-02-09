using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TileMatch.LevelEditor;
using TrumpTile.Core;
using UnityEngine;

namespace TileMatch
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
		[SerializeField] private float cellSize = 1f;
		[SerializeField] private float overlapThreshold = 0.8f;

		[Header("Layer Position Offsets")]
		[Tooltip("각 레이어별 X, Y 오프셋 설정 (Index = Layer)")]
		[SerializeField]
		private Vector2[] layerOffsets = new Vector2[]
		{
			new Vector2(0f, 0f),      // Layer 0 (맨 아래)
			new Vector2(0.06f, 0.06f), // Layer 1
			new Vector2(0.12f, 0.12f), // Layer 2
			new Vector2(0.18f, 0.18f), // Layer 3
			new Vector2(0.24f, 0.24f)  // Layer 4 (맨 위)
		};

		[Header("References")]
		[SerializeField] private TileController tilePrefab;
		[SerializeField] private List<TileData> allTileTypes;
		[SerializeField] private string tileDataFolderPath = "TileData";
		[SerializeField] private bool autoLoadTileData = true;

		[Header("Debug")]
		[SerializeField] private bool enableDebugLog = false;

		#endregion

		#region Private Fields

		private List<TileController> allTiles = new List<TileController>();
		private Dictionary<Vector3Int, TileController> tileGridMap = new Dictionary<Vector3Int, TileController>();

		private int gridWidth;
		private int gridHeight;
		private int maxLayers;

		private bool isShuffling = false;
		private bool isLevelLoaded = false;

		// 마지막으로 배치된 타일 위치 저장
		private Vector3 lastPlacedTilePosition = Vector3.zero;

		#endregion

		#region Properties

		public int GridWidth => gridWidth;
		public int GridHeight => gridHeight;
		public int MaxLayers => maxLayers;
		public int TotalTileCount => allTiles.Count(t => t != null && !t.IsInSlot);
		public bool IsShuffling => isShuffling;
		public bool IsLevelLoaded => isLevelLoaded;

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;

			if (autoLoadTileData)
			{
				LoadAllTileData();
			}
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

		private void LoadAllTileData()
		{
			var loadedTiles = Resources.LoadAll<TileData>(tileDataFolderPath);

			if (loadedTiles != null && loadedTiles.Length > 0)
			{
				allTileTypes = new List<TileData>(loadedTiles);
				Log($"Loaded {allTileTypes.Count} TileData");
			}
			else
			{
				loadedTiles = Resources.LoadAll<TileData>("Data/TileData");
				if (loadedTiles != null && loadedTiles.Length > 0)
				{
					allTileTypes = new List<TileData>(loadedTiles);
				}
				else
				{
					Debug.LogWarning("[BoardManager] No TileData found!");
				}
			}
		}

		public void LoadLevel(LevelData levelData)
		{
			if (levelData == null)
			{
				Debug.LogError("[BoardManager] LevelData is null!");
				return;
			}

			Log($"Loading level: {levelData.levelNumber}");

			ClearBoard();

			gridWidth = levelData.boardWidth;
			gridHeight = levelData.boardHeight;
			maxLayers = levelData.maxLayers;

			SortingManager.SetMaxGridY(gridHeight);

			var tileDataMap = CreateTileDataMap();

			int createdCount = 0;
			foreach (var placement in levelData.tilePlacements)
			{
				if (!tileDataMap.TryGetValue(placement.tileTypeId, out TileData data))
				{
					Debug.LogWarning($"[BoardManager] TileData not found: {placement.tileTypeId}");
					continue;
				}

				CreateTile(data, placement.gridX, placement.gridY, placement.layer);
				createdCount++;
			}

			UpdateAllBlockedStates();

			isLevelLoaded = true;
			Log($"Level loaded: {createdCount} tiles, Grid: {gridWidth}x{gridHeight}, Layers: {maxLayers}");
		}

		private Dictionary<string, TileData> CreateTileDataMap()
		{
			var map = new Dictionary<string, TileData>();
			if (allTileTypes == null) return map;

			foreach (var data in allTileTypes)
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

		private TileController CreateTile(TileData data, int x, int y, int layer)
		{
			if (tilePrefab == null)
			{
				Debug.LogError("[BoardManager] Tile prefab is null!");
				return null;
			}

			Vector3 position = GridToWorldPosition(x, y, layer);
			TileController tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);

			// Initialize에서 Sorting Order도 설정됨
			tile.Initialize(data, x, y, layer);

			allTiles.Add(tile);

			Vector3Int gridPos = new Vector3Int(x, y, layer);
			tileGridMap[gridPos] = tile;

			return tile;
		}

		#endregion

		#region Blocked State

		public bool IsTileBlocked(TileController tile)
		{
			if (tile == null) return false;
			if (tile.IsInSlot) return false;

			int tileX = tile.GridX;
			int tileY = tile.GridY;
			int tileLayer = tile.LayerIndex;
			Vector3 tilePos = tile.transform.position;

			float checkRadius = cellSize * overlapThreshold;

			foreach (var other in allTiles)
			{
				if (other == null || other == tile || other.IsInSlot) continue;
				if (other.LayerIndex <= tileLayer) continue;

				Vector3 otherPos = other.transform.position;

				float dx = Mathf.Abs(otherPos.x - tilePos.x);
				float dy = Mathf.Abs(otherPos.y - tilePos.y);

				if (dx < checkRadius && dy < checkRadius)
				{
					return true;
				}
			}

			return false;
		}

		public void UpdateAllBlockedStates()
		{
			foreach (var tile in allTiles)
			{
				if (tile != null && !tile.IsInSlot)
				{
					bool blocked = IsTileBlocked(tile);
					tile.SetSelectable(!blocked);
				}
			}
		}

		#endregion

		#region Remove Tile

		public void RemoveTileFromBoard(TileController tile)
		{
			if (tile == null) return;

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			tileGridMap.Remove(gridPos);

			UpdateAllBlockedStates();

			Log($"Tile removed from board: {tile.TileTypeId}");
		}

		public void RemoveTile(TileController tile)
		{
			if (tile == null) return;

			allTiles.Remove(tile);

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			tileGridMap.Remove(gridPos);

			UpdateAllBlockedStates();
		}

		#endregion

		#region Place Tile

		public void ReturnTileToBoard(TileController tile, int origX, int origY, int origLayer)
		{
			if (tile == null) return;

			Vector3Int origGridPos = new Vector3Int(origX, origY, origLayer);

			if (!tileGridMap.ContainsKey(origGridPos))
			{
				PlaceTileAt(tile, origX, origY, origLayer);
				return;
			}

			PlaceTileOnEmptySpot(tile);
		}

		public bool PlaceTileOnEmptySpot(TileController tile)
		{
			if (tile == null) return false;

			for (int layer = maxLayers - 1; layer >= 0; layer--)
			{
				var emptyPos = FindEmptyPositionOnLayer(layer);
				if (emptyPos.HasValue)
				{
					PlaceTileAt(tile, emptyPos.Value.x, emptyPos.Value.y, layer);
					Log($"Tile placed at empty spot: ({emptyPos.Value.x}, {emptyPos.Value.y}, L{layer})");
					return true;
				}
			}

			int newX = gridWidth;
			PlaceTileAt(tile, newX, 0, 0);
			Log($"Tile placed at extended position: ({newX}, 0, L0)");
			return true;
		}

		private Vector2Int? FindEmptyPositionOnLayer(int layer)
		{
			HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

			foreach (var tile in allTiles)
			{
				if (tile == null || tile.IsInSlot) continue;

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

			int centerX = gridWidth / 2;
			int centerY = gridHeight / 2;

			for (int radius = 0; radius <= Mathf.Max(gridWidth, gridHeight); radius++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

						int x = centerX + dx;
						int y = centerY + dy;

						if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;

						Vector2Int pos = new Vector2Int(x, y);
						if (!occupiedPositions.Contains(pos))
						{
							Vector3Int gridPos = new Vector3Int(x, y, layer);
							if (!tileGridMap.ContainsKey(gridPos))
							{
								return pos;
							}
						}
					}
				}
			}

			return null;
		}

		private void PlaceTileAt(TileController tile, int x, int y, int layer)
		{
			Vector3 position = GridToWorldPosition(x, y, layer);

			tile.ReturnToBoard(position, x, y, layer);

			if (!allTiles.Contains(tile))
			{
				allTiles.Add(tile);
			}

			Vector3Int gridPos = new Vector3Int(x, y, layer);
			tileGridMap[gridPos] = tile;

			// 마지막 배치 위치 저장
			lastPlacedTilePosition = position;

			UpdateAllBlockedStates();
		}

		public bool PlaceTileOnBoard(TileController tile)
		{
			return PlaceTileOnEmptySpot(tile);
		}

		/// <summary>
		/// 마지막으로 배치된 타일의 위치 반환
		/// </summary>
		public Vector3 GetLastPlacedTilePosition()
		{
			return lastPlacedTilePosition;
		}

		#endregion

		#region Shuffle

		public IEnumerator ShuffleBoardAnimated()
		{
			if (isShuffling) yield break;
			isShuffling = true;

			var boardTiles = allTiles
				.Where(t => t != null && !t.IsInSlot)
				.ToList();

			Log($"Shuffling {boardTiles.Count} tiles");

			if (boardTiles.Count <= 1)
			{
				isShuffling = false;
				yield break;
			}

			foreach (var tile in boardTiles)
			{
				tile.SetSelectable(false);
			}

			var dataList = boardTiles
				.Where(t => t.Data != null)
				.Select(t => t.Data)
				.ToList();

			ShuffleList(dataList);

			yield return StartCoroutine(ShuffleAnimation(boardTiles));

			for (int i = 0; i < boardTiles.Count && i < dataList.Count; i++)
			{
				boardTiles[i].SetTileData(dataList[i]);
				boardTiles[i].transform.rotation = Quaternion.identity;
			}

			UpdateAllBlockedStates();

			isShuffling = false;
		}

		private IEnumerator ShuffleAnimation(List<TileController> tiles)
		{
			float duration = GameRules.SHUFFLE_DURATION;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / duration;
				float angle = Mathf.Lerp(0f, 360f, t);

				foreach (var tile in tiles)
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
				var selectableTiles = allTiles
					.Where(t => t != null && !t.IsInSlot && !IsTileBlocked(t) && t.Data != null)
					.ToList();

				var groups = selectableTiles
					.GroupBy(t => t.Data.TileID)
					.Where(g => g.Count() >= matchCount)
					.ToList();

				if (groups.Count == 0) break;

				var randomGroup = groups[Random.Range(0, groups.Count)];
				var tilesToRemove = randomGroup.Take(matchCount).ToList();

				foreach (var tile in tilesToRemove)
				{
					RemoveTile(tile);
					tile.Remove();
				}

				removedSets++;
			}

			Log($"Removed {removedSets} match sets");
			return removedSets;
		}

		#endregion

		#region Utility

		public Vector3 GridToWorldPosition(int x, int y, int layer)
		{
			Vector3 boardOrigin = transform.position - new Vector3(
				(gridWidth - 1) * cellSize / 2f,
				(gridHeight - 1) * cellSize / 2f,
				0
			);

			// 레이어별 오프셋 가져오기
			Vector2 offset = GetLayerOffset(layer);

			return boardOrigin + new Vector3(
				x * cellSize + offset.x,
				y * cellSize + offset.y,
				-layer * 0.1f
			);
		}

		/// <summary>
		/// 레이어별 오프셋 반환
		/// </summary>
		private Vector2 GetLayerOffset(int layer)
		{
			if (layerOffsets == null || layerOffsets.Length == 0)
			{
				// 기본값: 레이어당 0.06씩 증가
				return new Vector2(layer * 0.06f, layer * 0.06f);
			}

			// 배열 범위 내면 해당 값, 아니면 마지막 값 사용
			int index = Mathf.Clamp(layer, 0, layerOffsets.Length - 1);
			return layerOffsets[index];
		}

		private void ClearBoard()
		{
			foreach (var tile in allTiles)
			{
				if (tile != null)
				{
					Destroy(tile.gameObject);
				}
			}

			allTiles.Clear();
			tileGridMap.Clear();
			isLevelLoaded = false;
			isShuffling = false;
			lastPlacedTilePosition = Vector3.zero;
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
			if (enableDebugLog)
			{
				Debug.Log($"[BoardManager] {message}");
			}
		}

		#endregion

		#region Public Getters

		public bool HasRemainingTiles()
		{
			return allTiles.Any(t => t != null && !t.IsInSlot);
		}

		public List<TileController> GetAllTiles()
		{
			return allTiles;
		}

		public List<TileController> GetBoardTiles()
		{
			return allTiles.Where(t => t != null && !t.IsInSlot).ToList();
		}

		public void SetSize(int width, int height)
		{
			gridWidth = width;
			gridHeight = height;
		}

		#endregion
	}
}