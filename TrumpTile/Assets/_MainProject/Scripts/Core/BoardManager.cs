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
		[SerializeField] private float mCellSize = 1F;
		[SerializeField] private float mOverlapThreshold = 0.8F;

		[Header("Layer Position Offsets")]
		[Tooltip("각 레이어별 X, Y 오프셋 설정 (Index = Layer)")]
		[SerializeField]
		private Vector2[] mLayerOffsets = new Vector2[]
		{
			new Vector2(0F, 0F),      // Layer 0 (맨 아래)
			new Vector2(0.06F, 0.06F), // Layer 1
			new Vector2(0.12F, 0.12F), // Layer 2
			new Vector2(0.18F, 0.18F), // Layer 3
			new Vector2(0.24F, 0.24F)  // Layer 4 (맨 위)
		};

		[Header("References")]
		[SerializeField] private TileController mTilePrefab;
		[SerializeField] private List<TileData> mAllTileTypes;

		[Header("Debug")]
		[SerializeField] private bool mEnableDebugLog = false;

		#endregion

		#region Private Fields

		private List<TileController> mAllTiles = new List<TileController>();
		private Dictionary<Vector3Int, TileController> mTileGridMap = new Dictionary<Vector3Int, TileController>();

		private int mGridWidth;
		private int mGridHeight;
		private int mMaxLayers;

		private bool mIsShuffling = false;
		private bool mIsLevelLoaded = false;

		// 마지막으로 배치된 타일 위치 저장
		private Vector3 mLastPlacedTilePosition = Vector3.zero;

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
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
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

			ClearBoard();

			mGridWidth = levelData.boardWidth;
			mGridHeight = levelData.boardHeight;
			mMaxLayers = levelData.maxLayers;

			SortingManager.SetMaxGridY(mGridHeight);

			Dictionary<string, TileData> tileDataMap = CreateTileDataMap();

			int createdCount = 0;
			foreach (TilePlacement placement in levelData.tilePlacements)
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

			mIsLevelLoaded = true;
			Log($"Level loaded: {createdCount} tiles, Grid: {mGridWidth}x{mGridHeight}, Layers: {mMaxLayers}");
		}

		private Dictionary<string, TileData> CreateTileDataMap()
		{
			Dictionary<string, TileData> map = new Dictionary<string, TileData>();
			if (mAllTileTypes == null) return map;

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

		private TileController CreateTile(TileData data, int x, int y, int layer)
		{
			if (mTilePrefab == null)
			{
				Debug.LogError("[BoardManager] Tile prefab is null!");
				return null;
			}

			Vector3 position = GridToWorldPosition(x, y, layer);
			TileController tile = Instantiate(mTilePrefab, position, Quaternion.identity, transform);

			// Initialize에서 Sorting Order도 설정됨
			tile.Initialize(data, x, y, layer);

			mAllTiles.Add(tile);

			Vector3Int gridPos = new Vector3Int(x, y, layer);
			mTileGridMap[gridPos] = tile;

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

			float checkRadius = mCellSize * mOverlapThreshold;

			foreach (TileController other in mAllTiles)
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

		public void RemoveTileFromBoard(TileController tile)
		{
			if (tile == null) return;

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap.Remove(gridPos);

			UpdateAllBlockedStates();

			Log($"Tile removed from board: {tile.TileTypeId}");
		}

		public void RemoveTile(TileController tile)
		{
			if (tile == null) return;

			mAllTiles.Remove(tile);

			Vector3Int gridPos = new Vector3Int(tile.GridX, tile.GridY, tile.LayerIndex);
			mTileGridMap.Remove(gridPos);

			UpdateAllBlockedStates();
		}

		#endregion

		#region Place Tile

		public void ReturnTileToBoard(TileController tile, int origX, int origY, int origLayer)
		{
			if (tile == null) return;

			Vector3Int origGridPos = new Vector3Int(origX, origY, origLayer);

			if (!mTileGridMap.ContainsKey(origGridPos))
			{
				PlaceTileAt(tile, origX, origY, origLayer);
				return;
			}

			PlaceTileOnEmptySpot(tile);
		}

		public bool PlaceTileOnEmptySpot(TileController tile)
		{
			if (tile == null) return false;

			for (int layer = mMaxLayers - 1; layer >= 0; layer--)
			{
				Vector2Int? emptyPos = FindEmptyPositionOnLayer(layer);
				if (emptyPos.HasValue)
				{
					PlaceTileAt(tile, emptyPos.Value.x, emptyPos.Value.y, layer);
					Log($"Tile placed at empty spot: ({emptyPos.Value.x}, {emptyPos.Value.y}, L{layer})");
					return true;
				}
			}

			int newX = mGridWidth;
			PlaceTileAt(tile, newX, 0, 0);
			Log($"Tile placed at extended position: ({newX}, 0, L0)");
			return true;
		}

		private Vector2Int? FindEmptyPositionOnLayer(int layer)
		{
			HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

			foreach (TileController tile in mAllTiles)
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

			int centerX = mGridWidth / 2;
			int centerY = mGridHeight / 2;

			for (int radius = 0; radius <= Mathf.Max(mGridWidth, mGridHeight); radius++)
			{
				for (int dx = -radius; dx <= radius; dx++)
				{
					for (int dy = -radius; dy <= radius; dy++)
					{
						if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

						int x = centerX + dx;
						int y = centerY + dy;

						if (x < 0 || x >= mGridWidth || y < 0 || y >= mGridHeight) continue;

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

		private void PlaceTileAt(TileController tile, int x, int y, int layer)
		{
			Vector3 position = GridToWorldPosition(x, y, layer);

			tile.ReturnToBoard(position, x, y, layer);

			if (!mAllTiles.Contains(tile))
			{
				mAllTiles.Add(tile);
			}

			Vector3Int gridPos = new Vector3Int(x, y, layer);
			mTileGridMap[gridPos] = tile;

			// 마지막 배치 위치 저장
			mLastPlacedTilePosition = position;

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
			return mLastPlacedTilePosition;
		}

		#endregion

		#region Shuffle

		public IEnumerator ShuffleBoardAnimated()
		{
			if (mIsShuffling) yield break;
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

			yield return StartCoroutine(ShuffleAnimation(boardTiles));

			for (int i = 0; i < boardTiles.Count && i < dataList.Count; i++)
			{
				boardTiles[i].SetTileData(dataList[i]);
				boardTiles[i].transform.rotation = Quaternion.identity;
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

				if (groups.Count == 0) break;

				IGrouping<string, TileController> randomGroup = groups[Random.Range(0, groups.Count)];
				List<TileController> tilesToRemove = randomGroup.Take(matchCount).ToList();

				foreach (TileController tile in tilesToRemove)
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
				(mGridWidth - 1) * mCellSize / 2F,
				(mGridHeight - 1) * mCellSize / 2F,
				0
			);

			// 레이어별 오프셋 가져오기
			Vector2 offset = GetLayerOffset(layer);

			return boardOrigin + new Vector3(
				x * mCellSize + offset.x,
				y * mCellSize + offset.y,
				-layer * 0.1F
			);
		}

		/// <summary>
		/// 레이어별 오프셋 반환
		/// </summary>
		private Vector2 GetLayerOffset(int layer)
		{
			if (mLayerOffsets == null || mLayerOffsets.Length == 0)
			{
				// 기본값: 레이어당 0.06씩 증가
				return new Vector2(layer * 0.06F, layer * 0.06F);
			}

			// 배열 범위 내면 해당 값, 아니면 마지막 값 사용
			int index = Mathf.Clamp(layer, 0, mLayerOffsets.Length - 1);
			return mLayerOffsets[index];
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

		public void SetSize(int width, int height)
		{
			mGridWidth = width;
			mGridHeight = height;
		}

		#endregion
	}
}
