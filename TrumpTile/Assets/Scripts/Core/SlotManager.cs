using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TileMatch;
using TrumpTile.Audio;
using TrumpTile.Effects;
using UnityEngine;

namespace TrumpTile.Core
{
	/// <summary>
	/// 슬롯 관리자 (수정됨)
	/// 
	/// [추가된 기능]
	/// - GetLastTilePosition(): 마지막 타일 위치 반환
	/// - RemoveOneTileToBoard(out Vector3): 착지 위치 반환
	/// </summary>
	public class SlotManager : MonoBehaviour
	{
		public static SlotManager Instance { get; private set; }

		[Header("Slot Settings")]
		[SerializeField] private int maxSlots = 7;
		[SerializeField] private Transform[] slotPositions;

		[Header("Animation")]
		[SerializeField] private float tileMoveDuration = 0.15f;
		[SerializeField] private float tileMergeTime = 0.15f;

		// 이벤트
		public event Action<int> OnMatch;
		public event Action<int> OnMatchFound;
		public event Action OnSlotFull;
		public event Action OnGameOver;
		public event Action OnLevelClear;

		// 타일 리스트
		private List<TileController> slotTiles = new List<TileController>();

		// 처리 중 락
		private bool isProcessingMatch = false;
		private bool isGameEnded = false;

		// Undo 스택
		private Stack<UndoData> undoStack = new Stack<UndoData>();

		private struct UndoData
		{
			public TileController tile;
			public Vector3 originalPosition;
			public int originalGridX;
			public int originalGridY;
			public int originalLayer;
		}

		// Properties
		public int CurrentTileCount => slotTiles.Count;
		public int MaxSlots => maxSlots;
		public bool IsProcessing => isProcessingMatch;
		public bool IsGameEnded => isGameEnded;

		private void Awake()
		{
			if (Instance == null)
				Instance = this;
			else
				Destroy(gameObject);
		}

		#region Public Methods

		public void ResetSlots()
		{
			StopAllCoroutines();

			foreach (var tile in slotTiles)
			{
				if (tile != null) Destroy(tile.gameObject);
			}

			slotTiles.Clear();
			undoStack.Clear();
			isProcessingMatch = false;
			isGameEnded = false;
		}

		public void ClearSlots() => ResetSlots();

		public void SetSlotCount(int count) => maxSlots = count;

		public void ResumeGame()
		{
			StopAllCoroutines();
			isGameEnded = false;
			isProcessingMatch = false;
		}

		/// <summary>
		/// 마지막 타일의 위치 반환
		/// </summary>
		public Vector3 GetLastTilePosition()
		{
			if (slotTiles.Count == 0) return transform.position;

			var lastTile = slotTiles[slotTiles.Count - 1];
			if (lastTile != null)
			{
				return lastTile.transform.position;
			}

			return transform.position;
		}

		/// <summary>
		/// 타일 추가
		/// </summary>
		public bool AddTile(TileController tile)
		{
			if (tile == null) return false;
			if (isGameEnded) return false;

			if (slotTiles.Count >= maxSlots)
			{
				Debug.Log($"[SlotManager] Slot full! Count: {slotTiles.Count}");
				OnSlotFull?.Invoke();
				return false;
			}

			undoStack.Push(new UndoData
			{
				tile = tile,
				originalPosition = tile.transform.position,
				originalGridX = tile.GridX,
				originalGridY = tile.GridY,
				originalLayer = tile.LayerIndex
			});

			int insertIndex = FindInsertIndex(tile);
			slotTiles.Insert(insertIndex, tile);

			Debug.Log($"[SlotManager] Tile added: {tile.Data?.TileID}, Index: {insertIndex}, Total: {slotTiles.Count}");

			StartCoroutine(ProcessTileAddition(tile, insertIndex));

			return true;
		}

		/// <summary>
		/// 타일 하나를 보드로 복귀 (기본)
		/// </summary>
		public bool RemoveOneTileToBoard()
		{
			Vector3 landPosition;
			return RemoveOneTileToBoard(out landPosition);
		}

		/// <summary>
		/// 타일 하나를 보드로 복귀 (착지 위치 반환)
		/// </summary>
		public bool RemoveOneTileToBoard(out Vector3 landPosition)
		{
			landPosition = Vector3.zero;

			if (slotTiles.Count == 0) return false;
			if (isProcessingMatch) return false;

			var tile = slotTiles[slotTiles.Count - 1];
			if (tile == null) return false;

			slotTiles.Remove(tile);

			bool placed = BoardManager.Instance?.PlaceTileOnEmptySpot(tile) ?? false;

			if (!placed)
			{
				slotTiles.Add(tile);
				Debug.LogWarning("[SlotManager] Failed to place tile on board");
				return false;
			}

			// 착지 위치 반환
			landPosition = tile.transform.position;

			RearrangeSlots();

			Debug.Log($"[SlotManager] Tile returned to board: {tile.Data?.TileID} at {landPosition}");
			return true;
		}

		#endregion

		#region Tile Processing

		private IEnumerator ProcessTileAddition(TileController newTile, int insertIndex)
		{
			AudioManager.Instance?.PlayTileSelect();

			if (slotPositions != null && insertIndex < slotPositions.Length && slotPositions[insertIndex] != null)
			{
				newTile.MoveToSlot(slotPositions[insertIndex].position, insertIndex);
			}

			RearrangeSlots();

			yield return new WaitForSeconds(tileMoveDuration);

			// 매칭 체크 (다른 매칭 처리 중이 아닐 때만)
			if (!isProcessingMatch && !isGameEnded)
			{
				StartCoroutine(CheckAndProcessAllMatches());
			}
		}

		private int FindInsertIndex(TileController newTile)
		{
			if (newTile?.Data == null) return slotTiles.Count;

			string tileID = newTile.Data.TileID;

			for (int i = slotTiles.Count - 1; i >= 0; i--)
			{
				var existing = slotTiles[i];
				if (existing != null && existing != newTile && existing.Data?.TileID == tileID)
				{
					return i + 1;
				}
			}

			return slotTiles.Count;
		}

		private void RearrangeSlots()
		{
			if (slotPositions == null) return;

			for (int i = 0; i < slotTiles.Count && i < slotPositions.Length; i++)
			{
				var tile = slotTiles[i];
				if (tile == null || slotPositions[i] == null) continue;

				Vector3 targetPos = slotPositions[i].position;

				if (Vector3.Distance(tile.transform.position, targetPos) > 0.01f)
				{
					tile.AdjustSlotPosition(targetPos, i);
				}

				var sr = tile.GetComponentInChildren<SpriteRenderer>();
				if (sr != null)
				{
					sr.sortingOrder = SortingManager.GetSlotTileSortingOrder(i);
				}
			}
		}

		#endregion

		#region Match Processing

		private IEnumerator CheckAndProcessAllMatches()
		{
			// 이미 매치 처리 중이면 스킵
			if (isProcessingMatch)
			{
				Debug.Log("[SlotManager] Already processing match - skipped");
				yield break;
			}

			isProcessingMatch = true;

			// 약간의 딜레이로 중복 호출 방지
			yield return new WaitForSeconds(0.05f);

			while (true)
			{
				if (isGameEnded) break;

				var tileIDs = slotTiles.Where(t => t?.Data != null).Select(t => t.Data.TileID).ToList();
				Debug.Log($"[SlotManager] Checking matches. Tiles({tileIDs.Count}): {string.Join(", ", tileIDs)}");

				int matchCount = GameManager.Instance?.MatchCount ?? 3;

				var matchGroup = FindMatchGroup(matchCount);

				if (matchGroup == null || matchGroup.Count < matchCount)
				{
					Debug.Log($"[SlotManager] No match found");
					break;
				}

				Debug.Log($"[SlotManager] Match found! Type: {matchGroup[0].Data?.TileID}, Count: {matchGroup.Count}");

				yield return StartCoroutine(AnimateAndRemoveMatch(matchGroup));

				RearrangeSlots();

				yield return new WaitForSeconds(0.05f);
			}

			isProcessingMatch = false;

			CheckGameState();
		}

		private List<TileController> FindMatchGroup(int matchCount)
		{
			var validTiles = slotTiles.Where(t => t != null && t.Data != null).ToList();

			var groups = validTiles
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= matchCount)
				.ToList();

			if (groups.Count == 0) return null;

			return groups[0].Take(matchCount).ToList();
		}

		private IEnumerator AnimateAndRemoveMatch(List<TileController> matched)
		{
			if (matched == null || matched.Count == 0) yield break;

			Vector3 center = Vector3.zero;
			int suitIndex = 0;

			foreach (var tile in matched)
			{
				if (tile != null)
				{
					center += tile.transform.position;
					if (tile.Data != null) suitIndex = (int)tile.Data.suit;
				}
			}
			center /= matched.Count;

			var startPositions = matched.Select(t => t?.transform.position ?? Vector3.zero).ToList();
			var startScales = matched.Select(t => t?.transform.localScale ?? Vector3.one).ToList();

			float elapsed = 0f;
			while (elapsed < tileMergeTime)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / tileMergeTime;
				float easeT = t * t;

				for (int i = 0; i < matched.Count; i++)
				{
					if (matched[i] != null)
					{
						matched[i].transform.position = Vector3.Lerp(startPositions[i], center, easeT);
						matched[i].transform.localScale = Vector3.Lerp(startScales[i], Vector3.one * 0.3f, easeT);
					}
				}
				yield return null;
			}

			EffectManager.Instance?.PlayMatchEffect(center, suitIndex, 1);
			AudioManager.Instance?.PlayMatchSound(1);

			foreach (var tile in matched)
			{
				if (tile != null)
				{
					slotTiles.Remove(tile);
					Destroy(tile.gameObject);
				}
			}

			OnMatch?.Invoke(matched.Count);
			OnMatchFound?.Invoke(matched.Count);
		}

		#endregion

		#region Game State

		private void CheckGameState()
		{
			if (isGameEnded) return;

			bool boardEmpty = BoardManager.Instance == null || !BoardManager.Instance.HasRemainingTiles();
			bool slotEmpty = slotTiles.Count == 0;

			if (boardEmpty && slotEmpty)
			{
				Debug.Log("[SlotManager] Level Clear!");
				isGameEnded = true;
				OnLevelClear?.Invoke();
				return;
			}

			if (slotTiles.Count >= maxSlots)
			{
				int matchCount = GameManager.Instance?.MatchCount ?? 3;

				bool hasMatch = slotTiles
					.Where(t => t?.Data != null)
					.GroupBy(t => t.Data.TileID)
					.Any(g => g.Count() >= matchCount);

				if (!hasMatch)
				{
					Debug.Log("[SlotManager] Game Over!");
					isGameEnded = true;
					OnGameOver?.Invoke();
				}
			}
		}

		#endregion

		#region Item Methods

		public bool Undo()
		{
			if (isGameEnded || isProcessingMatch || undoStack.Count == 0)
				return false;

			var data = undoStack.Pop();
			if (data.tile == null || !slotTiles.Contains(data.tile))
				return false;

			slotTiles.Remove(data.tile);

			BoardManager.Instance?.ReturnTileToBoard(data.tile, data.originalGridX, data.originalGridY, data.originalLayer);

			RearrangeSlots();
			AudioManager.Instance?.PlayUndo();

			return true;
		}

		public bool UseUndoItem() => Undo();

		public int RemoveRandomMatchSets(int setCount)
		{
			int removed = 0;
			int matchCount = GameManager.Instance?.MatchCount ?? 3;

			for (int i = 0; i < setCount; i++)
			{
				var groups = slotTiles
					.Where(t => t?.Data != null)
					.GroupBy(t => t.Data.TileID)
					.Where(g => g.Count() >= matchCount)
					.ToList();

				if (groups.Count == 0) break;

				var group = groups[UnityEngine.Random.Range(0, groups.Count)];
				var toRemove = group.Take(matchCount).ToList();

				Vector3 center = Vector3.zero;
				foreach (var t in toRemove) center += t.transform.position;
				center /= toRemove.Count;

				EffectManager.Instance?.PlayMatchEffect(center, 0, 1);

				foreach (var t in toRemove)
				{
					slotTiles.Remove(t);
					Destroy(t.gameObject);
				}

				removed++;
			}

			if (removed > 0)
			{
				RearrangeSlots();
			}

			return removed;
		}

		#endregion

		#region Utility

		/// <summary>
		/// 슬롯의 모든 타일 반환
		/// </summary>
		public List<TileController> GetAllSlotTiles()
		{
			return slotTiles.Where(t => t != null).ToList();
		}

		/// <summary>
		/// 타일 직접 제거 (Boom 아이템용)
		/// </summary>
		public void RemoveTileDirectly(TileController tile)
		{
			if (tile == null) return;

			slotTiles.Remove(tile);
			RearrangeSlots();

			Debug.Log($"[SlotManager] Tile removed directly: {tile.Data?.TileID}");
		}

		public List<TileController> GetMatchableTiles()
		{
			var result = new List<TileController>();
			int matchCount = GameManager.Instance?.MatchCount ?? 3;

			var groups = slotTiles
				.Where(t => t?.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= matchCount - 1);

			foreach (var g in groups)
				result.AddRange(g);

			return result;
		}

		public int GetTileCount(string tileID)
		{
			return slotTiles.Count(t => t?.Data?.TileID == tileID);
		}

		public List<string> GetSlotTileIDs()
		{
			return slotTiles
				.Where(t => t?.Data != null)
				.Select(t => t.Data.TileID)
				.ToList();
		}

		#endregion
	}
}