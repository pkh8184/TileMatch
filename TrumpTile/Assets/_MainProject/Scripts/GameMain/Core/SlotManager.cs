using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrumpTile.GameMain.Core
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
		[SerializeField] private int mMaxSlots = 7;
		[SerializeField] private Transform[] mSlotPositions;

		[Header("Animation")]
		[SerializeField] private float mTileMoveDuration = 0.15F;
		[SerializeField] private float mTileMergeTime = 0.15F;

		// 이벤트
		public event Action<int> OnMatch;
		public event Action<int> OnMatchFound;
		public event Action OnSlotFull;
		public event Action OnGameOver;
		public event Action OnLevelClear;

		// 타일 리스트
		private List<TileController> mSlotTiles = new List<TileController>();

		// 처리 중 락
		private bool mIsProcessingMatch = false;
		private bool mIsGameEnded = false;

		// Undo 스택
		private Stack<UndoData> mUndoStack = new Stack<UndoData>();

		private struct UndoData
		{
			public TileController tile;
			public Vector3 originalPosition;
			public int originalGridX;
			public int originalGridY;
			public int originalLayer;
		}

		// Properties
		public int CurrentTileCount => mSlotTiles.Count;
		public int MaxSlots => mMaxSlots;
		public bool IsProcessing => mIsProcessingMatch;
		public bool IsGameEnded => mIsGameEnded;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		#region Public Methods

		public void ResetSlots()
		{
			StopAllCoroutines();

			foreach (TileController tile in mSlotTiles)
			{
				if (tile != null)
				{
					Destroy(tile.gameObject);
				}
			}

			mSlotTiles.Clear();
			mUndoStack.Clear();
			mIsProcessingMatch = false;
			mIsGameEnded = false;
		}

		public void ClearSlots() => ResetSlots();

		public void SetSlotCount(int count) => mMaxSlots = count;

		public void ResumeGame()
		{
			StopAllCoroutines();
			mIsGameEnded = false;
			mIsProcessingMatch = false;
		}

		/// <summary>
		/// 마지막 타일의 위치 반환
		/// </summary>
		public Vector3 GetLastTilePosition()
		{
			if (mSlotTiles.Count == 0)
			{
				return transform.position;
			}

			TileController lastTile = mSlotTiles[mSlotTiles.Count - 1];
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
			if (tile == null)
			{
				return false;
			}
			if (mIsGameEnded)
			{
				return false;
			}

			if (mSlotTiles.Count >= mMaxSlots)
			{
				Debug.Log($"[SlotManager] Slot full! Count: {mSlotTiles.Count}");
				OnSlotFull?.Invoke();
				return false;
			}

			mUndoStack.Push(new UndoData
			{
				tile = tile,
				originalPosition = tile.transform.position,
				originalGridX = tile.GridX,
				originalGridY = tile.GridY,
				originalLayer = tile.LayerIndex
			});

			int insertIndex = FindInsertIndex(tile);
			mSlotTiles.Insert(insertIndex, tile);

			Debug.Log($"[SlotManager] Tile added: {tile.Data?.TileID}, Index: {insertIndex}, Total: {mSlotTiles.Count}");

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

			if (mSlotTiles.Count == 0)
			{
				return false;
			}
			if (mIsProcessingMatch)
			{
				return false;
			}

			TileController tile = mSlotTiles[mSlotTiles.Count - 1];
			if (tile == null)
			{
				return false;
			}

			mSlotTiles.Remove(tile);

			bool bPlaced = BoardManager.Instance?.PlaceTileOnEmptySpot(tile) ?? false;

			if (!bPlaced)
			{
				mSlotTiles.Add(tile);
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

			if (mSlotPositions != null && insertIndex < mSlotPositions.Length && mSlotPositions[insertIndex] != null)
			{
				newTile.MoveToSlot(mSlotPositions[insertIndex].position, insertIndex);
			}

			RearrangeSlots();

			yield return new WaitForSeconds(mTileMoveDuration);

			// 매칭 체크 (다른 매칭 처리 중이 아닐 때만)
			if (!mIsProcessingMatch && !mIsGameEnded)
			{
				StartCoroutine(CheckAndProcessAllMatches());
			}
		}

		private int FindInsertIndex(TileController newTile)
		{
			if (newTile?.Data == null)
			{
				return mSlotTiles.Count;
			}

			string tileID = newTile.Data.TileID;

			for (int i = mSlotTiles.Count - 1; i >= 0; i--)
			{
				TileController existing = mSlotTiles[i];
				if (existing != null && existing != newTile && existing.Data?.TileID == tileID)
				{
					return i + 1;
				}
			}

			return mSlotTiles.Count;
		}

		private void RearrangeSlots()
		{
			if (mSlotPositions == null)
			{
				return;
			}

			for (int i = 0; i < mSlotTiles.Count && i < mSlotPositions.Length; i++)
			{
				TileController tile = mSlotTiles[i];
				if (tile == null || mSlotPositions[i] == null)
				{
					continue;
				}

				Vector3 targetPos = mSlotPositions[i].position;

				if (Vector3.Distance(tile.transform.position, targetPos) > 0.01F)
				{
					tile.AdjustSlotPosition(targetPos, i);
				}

				SpriteRenderer sr = tile.GetComponentInChildren<SpriteRenderer>();
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
			if (mIsProcessingMatch)
			{
				Debug.Log("[SlotManager] Already processing match - skipped");
				yield break;
			}

			mIsProcessingMatch = true;

			// 약간의 딜레이로 중복 호출 방지
			yield return new WaitForSeconds(0.05F);

			while (true)
			{
				if (mIsGameEnded)
				{
					break;
				}

				List<string> tileIDs = mSlotTiles.Where(t => t?.Data != null).Select(t => t.Data.TileID).ToList();
				Debug.Log($"[SlotManager] Checking matches. Tiles({tileIDs.Count}): {string.Join(", ", tileIDs)}");

				int matchCount = GameManager.Instance?.MatchCount ?? 3;

				List<TileController> matchGroup = FindMatchGroup(matchCount);

				if (matchGroup == null || matchGroup.Count < matchCount)
				{
					Debug.Log($"[SlotManager] No match found");
					break;
				}

				Debug.Log($"[SlotManager] Match found! Type: {matchGroup[0].Data?.TileID}, Count: {matchGroup.Count}");

				yield return StartCoroutine(AnimateAndRemoveMatch(matchGroup));

				RearrangeSlots();

				yield return new WaitForSeconds(0.05F);
			}

			mIsProcessingMatch = false;

			CheckGameState();
		}

		private List<TileController> FindMatchGroup(int matchCount)
		{
			List<TileController> validTiles = mSlotTiles.Where(t => t != null && t.Data != null).ToList();

			List<IGrouping<string, TileController>> groups = validTiles
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= matchCount)
				.ToList();

			if (groups.Count == 0)
			{
				return null;
			}

			return groups[0].Take(matchCount).ToList();
		}

		private IEnumerator AnimateAndRemoveMatch(List<TileController> matched)
		{
			if (matched == null || matched.Count == 0)
			{
				yield break;
			}

			Vector3 center = Vector3.zero;
			int suitIndex = 0;

			foreach (TileController tile in matched)
			{
				if (tile != null)
				{
					center += tile.transform.position;
					if (tile.Data != null)
					{
						suitIndex = (int)tile.Data.suit;
					}
				}
			}
			center /= matched.Count;

			List<Vector3> startPositions = matched.Select(t => t?.transform.position ?? Vector3.zero).ToList();
			List<Vector3> startScales = matched.Select(t => t?.transform.localScale ?? Vector3.one).ToList();

			float elapsed = 0F;
			while (elapsed < mTileMergeTime)
			{
				elapsed += Time.deltaTime;
				float t = elapsed / mTileMergeTime;
				float easeT = t * t;

				for (int i = 0; i < matched.Count; i++)
				{
					if (matched[i] != null)
					{
						matched[i].transform.position = Vector3.Lerp(startPositions[i], center, easeT);
						matched[i].transform.localScale = Vector3.Lerp(startScales[i], Vector3.one * 0.3F, easeT);
					}
				}
				yield return null;
			}

			EffectManager.Instance?.PlayMatchEffect(center, suitIndex, 1);
			AudioManager.Instance?.PlayMatchSound(1);

			foreach (TileController tile in matched)
			{
				if (tile != null)
				{
					mSlotTiles.Remove(tile);
					Destroy(tile.gameObject);
				}
			}

			OnMatch?.Invoke(matched.Count);
			OnMatchFound?.Invoke(matched.Count);
			if (EventManager.Inst != null)
			{
				EventManager.Inst.ActiveEvent(EventKeys.MATCH_OCCURRED, matched.Count);
			}
		}

		#endregion

		#region Game State

		private void CheckGameState()
		{
			if (mIsGameEnded)
			{
				return;
			}

			bool bBoardEmpty = BoardManager.Instance == null || !BoardManager.Instance.HasRemainingTiles();
			bool bSlotEmpty = mSlotTiles.Count == 0;

			if (bBoardEmpty && bSlotEmpty)
			{
				Debug.Log("[SlotManager] Level Clear!");
				mIsGameEnded = true;
				OnLevelClear?.Invoke();
				return;
			}

			if (mSlotTiles.Count >= mMaxSlots)
			{
				int matchCount = GameManager.Instance?.MatchCount ?? 3;

				bool bHasMatch = mSlotTiles
					.Where(t => t?.Data != null)
					.GroupBy(t => t.Data.TileID)
					.Any(g => g.Count() >= matchCount);

				if (!bHasMatch)
				{
					Debug.Log("[SlotManager] Game Over!");
					mIsGameEnded = true;
					OnGameOver?.Invoke();
				}
			}
		}

		#endregion

		#region Item Methods

		public bool Undo()
		{
			if (mIsGameEnded || mIsProcessingMatch || mUndoStack.Count == 0)
			{
				return false;
			}

			UndoData data = mUndoStack.Pop();
			if (data.tile == null || !mSlotTiles.Contains(data.tile))
			{
				return false;
			}

			mSlotTiles.Remove(data.tile);

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
				List<IGrouping<string, TileController>> groups = mSlotTiles
					.Where(t => t?.Data != null)
					.GroupBy(t => t.Data.TileID)
					.Where(g => g.Count() >= matchCount)
					.ToList();

				if (groups.Count == 0)
				{
					break;
				}

				IGrouping<string, TileController> group = groups[UnityEngine.Random.Range(0, groups.Count)];
				List<TileController> toRemove = group.Take(matchCount).ToList();

				Vector3 center = Vector3.zero;
				foreach (TileController t in toRemove)
				{
					center += t.transform.position;
				}
				center /= toRemove.Count;

				EffectManager.Instance?.PlayMatchEffect(center, 0, 1);

				foreach (TileController t in toRemove)
				{
					mSlotTiles.Remove(t);
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
			return mSlotTiles.Where(t => t != null).ToList();
		}

		/// <summary>
		/// 타일 직접 제거 (Boom 아이템용)
		/// </summary>
		public void RemoveTileDirectly(TileController tile)
		{
			if (tile == null)
			{
				return;
			}

			mSlotTiles.Remove(tile);
			RearrangeSlots();

			Debug.Log($"[SlotManager] Tile removed directly: {tile.Data?.TileID}");
		}

		public List<TileController> GetMatchableTiles()
		{
			List<TileController> result = new List<TileController>();
			int matchCount = GameManager.Instance?.MatchCount ?? 3;

			IEnumerable<IGrouping<string, TileController>> groups = mSlotTiles
				.Where(t => t?.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= matchCount - 1);

			foreach (IGrouping<string, TileController> g in groups)
			{
				result.AddRange(g);
			}

			return result;
		}

		public int GetTileCount(string tileID)
		{
			return mSlotTiles.Count(t => t?.Data?.TileID == tileID);
		}

		public List<string> GetSlotTileIDs()
		{
			return mSlotTiles
				.Where(t => t?.Data != null)
				.Select(t => t.Data.TileID)
				.ToList();
		}

		#endregion
	}
}
