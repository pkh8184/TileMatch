using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TrumpTile.UI;
using TrumpTile.Effects;
using TrumpTile.Audio;
using TrumpTile.Data;
using TileMatch.LevelEditor;
using TileMatch;

namespace TrumpTile.Core
{
	/// <summary>
	/// 게임 전체 상태 및 흐름 관리
	/// 
	/// [UserDataManager 연동]
	/// - SelectedStage: 메인에서 선택한 스테이지로 시작
	/// - ClearStage(): 클리어 시 호출하여 진행 저장
	/// - CurrentStage: 다음에 플레이할 스테이지
	/// </summary>
	public class GameManager : MonoBehaviour
	{
		public static GameManager Instance { get; private set; }

		[Header("References")]
		[SerializeField] private BoardManager boardManager;
		[SerializeField] private SlotManager slotManager;
		[SerializeField] private GameOverPopup gameOverPopup;
		[SerializeField] private VictoryPopup victoryPopup;

		[Header("Level Settings")]
		[SerializeField] private string levelFolderPath = "Levels";
		[SerializeField] private int startLevel = 1;

		[Header("Game Rules")]
		[SerializeField] private int matchCount = 3;
		[SerializeField] private int maxSlots = 7;

		[Header("Scoring")]
		[SerializeField] private int baseMatchScore = 100;
		[SerializeField] private int comboMultiplier = 50;
		[SerializeField] private int[] starThresholds = { 1000, 2000, 3000 };

		[Header("Items")]
		[SerializeField] private int initialStrikeCount = 3;
		[SerializeField] private int initialBlackHoleCount = 3;
		[SerializeField] private int initialBoomCount = 3;

		[Header("Debug")]
		[SerializeField] private bool enableDebugKeys = true;
		[SerializeField] private float slowMotionScale = 0.2f;
		private bool isSlowMotion = false;

		// 게임 상태
		public enum GameState { Loading, Playing, Paused, GameOver, GameClear }
		public GameState CurrentState { get; private set; }

		// Public 프로퍼티
		public int MatchCount => matchCount;

		// 레벨 정보
		private LevelData[] allLevels;
		private int currentLevelIndex;
		public int CurrentLevel => currentLevelIndex + 1;
		public int MaxLevel => allLevels != null ? allLevels.Length : 0;

		// 점수 및 통계
		private int currentScore;
		private int comboCount;
		private int matchedTileCount;
		private int totalTileCount;

		// 아이템
		private int strikeCount;
		private int blackHoleCount;
		private int boomCount;

		// 아이템 사용 중 플래그
		private bool isItemInProgress = false;

		// 이벤트
		public event System.Action<int> OnScoreChanged;
		public event System.Action<int> OnComboChanged;
		public event System.Action<int, int, int> OnItemCountChanged;
		public event System.Action<int, int> OnProgressChanged;

		#region Unity Lifecycle

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
				return;
			}

			LoadAllLevels();
		}

		private void Start()
		{
			LoadProgress();
			SubscribeEvents();
			InitializeItems();

			Debug.Log($"[GameManager] Starting level: {startLevel}");
			StartLevel(startLevel);
		}

		private void OnDestroy()
		{
			UnsubscribeEvents();

			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void Update()
		{
			if (enableDebugKeys)
			{
				HandleDebugKeys();
			}
		}

		private void HandleDebugKeys()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				RestartLevel();
			}

			if (Input.GetKeyDown(KeyCode.N))
			{
				NextLevel();
			}

			if (Input.GetKeyDown(KeyCode.T))
			{
				isSlowMotion = !isSlowMotion;
				Time.timeScale = isSlowMotion ? slowMotionScale : 1f;
				Debug.Log($"[GameManager] SlowMotion: {isSlowMotion}");
			}

			if (Input.GetKeyDown(KeyCode.C))
			{
				LevelClear();
			}

			if (Input.GetKeyDown(KeyCode.G))
			{
				OnGameOver();
			}
		}

		#endregion

		#region Level Management

		private void LoadAllLevels()
		{
			allLevels = Resources.LoadAll<LevelData>(levelFolderPath)
				.OrderBy(l => l.levelNumber)
				.ToArray();

			if (allLevels == null || allLevels.Length == 0)
			{
				Debug.LogError($"[GameManager] No levels found in Resources/{levelFolderPath}");
			}
			else
			{
				Debug.Log($"[GameManager] Loaded {allLevels.Length} levels");
			}
		}

		public void StartLevel(int levelNumber)
		{
			if (allLevels == null || allLevels.Length == 0)
			{
				Debug.LogError("[GameManager] No levels available!");
				return;
			}

			currentLevelIndex = Mathf.Clamp(levelNumber - 1, 0, allLevels.Length - 1);
			LevelData levelData = allLevels[currentLevelIndex];

			Debug.Log($"[GameManager] Starting Level {CurrentLevel}: {levelData.levelName}");

			CurrentState = GameState.Loading;
			currentScore = 0;
			comboCount = 0;
			matchedTileCount = 0;
			isItemInProgress = false;

			slotManager?.ResetSlots();
			boardManager?.LoadLevel(levelData);

			totalTileCount = boardManager?.TotalTileCount ?? 0;

			UIManager.Instance?.UpdateLevel(CurrentLevel);
			UIManager.Instance?.UpdateScore(currentScore);
			OnScoreChanged?.Invoke(currentScore);
			OnComboChanged?.Invoke(0);
			OnItemCountChanged?.Invoke(strikeCount, blackHoleCount, boomCount);

			CurrentState = GameState.Playing;
		}

		public void RestartLevel()
		{
			Debug.Log($"[GameManager] RestartLevel - Level {CurrentLevel}");
			StartLevel(CurrentLevel);
		}

		/// <summary>
		/// 다음 레벨로 이동
		/// </summary>
		public void NextLevel()
		{
			Debug.Log($"[GameManager] NextLevel called - Current: {CurrentLevel}, Max: {MaxLevel}");

			if (currentLevelIndex < allLevels.Length - 1)
			{
				int nextLevelNumber = CurrentLevel + 1;
				Debug.Log($"[GameManager] Going to level {nextLevelNumber}");
				StartLevel(nextLevelNumber);
			}
			else
			{
				Debug.Log("[GameManager] Max level reached - Going to main menu");
				GoToMainMenu();
			}
		}

		/// <summary>
		/// 다음 레벨이 있는지 확인
		/// </summary>
		public bool HasNextLevel()
		{
			return currentLevelIndex < allLevels.Length - 1;
		}

		public void GoToLevel(int levelNumber)
		{
			StartLevel(levelNumber);
		}

		/// <summary>
		/// 메인 화면으로 이동
		/// </summary>
		public void GoToMainMenu()
		{
			Debug.Log("[GameManager] GoToMainMenu called");

			AudioManager.Instance?.PlayMainMenuBGM();

			if (TransitionManager.Instance != null)
			{
				TransitionManager.Instance.LoadScene("MainScene");
			}
			else
			{
				SceneManager.LoadScene("MainScene");
			}
		}

		#endregion

		#region Score

		public void AddScore(int amount)
		{
			currentScore += amount;
			OnScoreChanged?.Invoke(currentScore);
		}

		public int GetScore() => currentScore;

		#endregion

		#region Match Handler

		private void OnMatchHandler(int matchedCount)
		{
			comboCount++;
			OnComboChanged?.Invoke(comboCount);

			if (comboCount > 1)
			{
				AddScore(comboMultiplier * (comboCount - 1));
			}

			matchedTileCount += matchedCount;
			OnProgressChanged?.Invoke(matchedTileCount, totalTileCount);

			if (comboCount > 1)
			{
				AudioManager.Instance?.PlayMatchSound(comboCount);
			}
		}

		#endregion

		#region Game State

		public void OnGameOver()
		{
			if (CurrentState == GameState.GameOver) return;

			Debug.Log("[GameManager] Game Over!");

			CurrentState = GameState.GameOver;

			UIManager.Instance?.DisableItemButtons();
			EffectManager.Instance?.PlayGameOverEffect();
			AudioManager.Instance?.PlayGameOver();

			if (gameOverPopup != null)
			{
				gameOverPopup.Show();
			}
		}

		private void OnContinueGame()
		{
			Debug.Log("[GameManager] Continue game - Revive");

			CurrentState = GameState.Playing;

			slotManager?.ResumeGame();

			slotManager?.RemoveOneTileToBoard();
			slotManager?.RemoveOneTileToBoard();

			UIManager.Instance?.UpdateItemButtonStates();
		}

		public void LevelClear()
		{
			if (CurrentState == GameState.GameClear) return;
			StartCoroutine(LevelClearCoroutine());
		}

		private IEnumerator LevelClearCoroutine()
		{
			CurrentState = GameState.GameClear;

			UIManager.Instance?.DisableItemButtons();

			yield return new WaitForSeconds(0.5f);

			EffectManager.Instance?.PlayClearEffect();
			AudioManager.Instance?.PlayGameClear();

			int stars = CalculateStars();

			// UserDataManager에 클리어 정보 저장
			SaveLevelProgress(CurrentLevel, stars);

			yield return new WaitForSeconds(0.5f);

			// VictoryPopup 표시
			if (victoryPopup != null)
			{
				bool hasNext = HasNextLevel();
				Debug.Log($"[GameManager] Showing VictoryPopup - Level: {CurrentLevel}, HasNext: {hasNext}");
				victoryPopup.Show(CurrentLevel, currentScore, stars, hasNext);
			}
			else
			{
				Debug.LogWarning("[GameManager] VictoryPopup is null!");
				UIManager.Instance?.ShowLevelClearPanel(stars);
			}
		}

		private int CalculateStars()
		{
			int stars = 1;
			for (int i = 0; i < starThresholds.Length; i++)
			{
				if (currentScore >= starThresholds[i])
				{
					stars = i + 1;
				}
			}
			return Mathf.Min(stars, 3);
		}

		public void PauseGame()
		{
			if (CurrentState != GameState.Playing) return;

			CurrentState = GameState.Paused;
			Time.timeScale = 0f;
			AudioManager.Instance?.PauseBGM();
			UIManager.Instance?.ShowPausePanel();
		}

		public void ResumeGame()
		{
			if (CurrentState != GameState.Paused) return;

			CurrentState = GameState.Playing;
			Time.timeScale = 1f;
			AudioManager.Instance?.ResumeBGM();
		}

		#endregion

		#region Items

		public bool CanUseItem()
		{
			return CurrentState == GameState.Playing && !isItemInProgress;
		}

		public void UseStrike()
		{
			if (!CanUseItem()) return;
			if (strikeCount <= 0) return;
			if (slotManager == null || slotManager.CurrentTileCount == 0) return;

			strikeCount--;
			OnItemCountChanged?.Invoke(strikeCount, blackHoleCount, boomCount);

			StartCoroutine(StrikeCoroutine());
		}

		private IEnumerator StrikeCoroutine()
		{
			isItemInProgress = true;

			Vector3 popPosition = slotManager.GetLastTilePosition();

			EffectManager.Instance?.PlayStrikePopEffect(popPosition);
			AudioManager.Instance?.PlayItemUse();

			yield return new WaitForSeconds(0.3f);

			Vector3 landPosition;
			bool success = slotManager.RemoveOneTileToBoard(out landPosition);

			if (success)
			{
				Vector3 actualLandPosition = boardManager?.GetLastPlacedTilePosition() ?? landPosition;
				EffectManager.Instance?.PlayStrikeLandEffect(actualLandPosition);
			}

			yield return new WaitForSeconds(0.2f);

			isItemInProgress = false;
		}

		public void UseBlackHole()
		{
			if (!CanUseItem()) return;
			if (blackHoleCount <= 0) return;
			if (boardManager == null || !boardManager.HasRemainingTiles()) return;

			blackHoleCount--;
			OnItemCountChanged?.Invoke(strikeCount, blackHoleCount, boomCount);

			StartCoroutine(BlackHoleCoroutine());
		}

		private IEnumerator BlackHoleCoroutine()
		{
			isItemInProgress = true;

			var boardTiles = boardManager.GetBoardTiles();

			var tileTransforms = boardTiles
				.Where(t => t != null)
				.Select(t => t.transform)
				.ToList();

			var originalPositions = new Dictionary<TileController, Vector3>();
			foreach (var tile in boardTiles)
			{
				if (tile != null)
				{
					originalPositions[tile] = tile.transform.position;
				}
			}

			EffectManager.Instance?.PlayBlackHoleEffect(
				tileTransforms,
				() => { },
				() => { boardManager.StartCoroutine(boardManager.ShuffleBoardAnimated()); }
			);

			yield return new WaitForSeconds(1.5f);

			foreach (var tile in boardTiles)
			{
				if (tile != null && originalPositions.ContainsKey(tile))
				{
					tile.transform.position = originalPositions[tile];
				}
			}

			yield return new WaitForSeconds(0.5f);

			isItemInProgress = false;
		}

		public void UseBoom()
		{
			if (!CanUseItem()) return;
			if (boomCount <= 0) return;

			var allBoardTiles = boardManager?.GetBoardTiles() ?? new List<TileController>();
			var allSlotTiles = slotManager?.GetAllSlotTiles() ?? new List<TileController>();

			var allTiles = new List<TileController>();
			allTiles.AddRange(allBoardTiles);
			allTiles.AddRange(allSlotTiles);

			var selectableTiles = allTiles
				.Where(t => t != null && t.Data != null)
				.ToList();

			var groups = selectableTiles
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= matchCount)
				.ToList();

			if (groups.Count == 0) return;

			boomCount--;
			OnItemCountChanged?.Invoke(strikeCount, blackHoleCount, boomCount);

			StartCoroutine(BoomCoroutine(groups));
		}

		private IEnumerator BoomCoroutine(List<IGrouping<string, TileController>> groups)
		{
			isItemInProgress = true;

			AudioManager.Instance?.PlayItemUse();

			int setsToRemove = Mathf.Min(3, groups.Count);

			var allPositions = new List<Vector3>();
			var allTilesToRemove = new List<TileController>();

			for (int i = 0; i < setsToRemove; i++)
			{
				var group = groups[i];
				var tilesToRemove = group.Take(matchCount).ToList();

				foreach (var tile in tilesToRemove)
				{
					if (tile != null)
					{
						allPositions.Add(tile.transform.position);
						allTilesToRemove.Add(tile);
					}
				}
			}

			bool effectComplete = false;
			EffectManager.Instance?.PlayBoomEffect(allPositions, () => { effectComplete = true; });

			foreach (var tile in allTilesToRemove)
			{
				if (tile != null)
				{
					if (tile.IsInSlot)
					{
						slotManager?.RemoveTileDirectly(tile);
					}
					else
					{
						boardManager?.RemoveTile(tile);
					}

					tile.Remove();
				}
			}

			float timeout = 2f;
			float elapsed = 0f;
			while (!effectComplete && elapsed < timeout)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			boardManager?.UpdateAllBlockedStates();

			yield return new WaitForSeconds(0.3f);

			isItemInProgress = false;

			CheckLevelClear();
		}

		private void InitializeItems()
		{
			strikeCount = initialStrikeCount;
			blackHoleCount = initialBlackHoleCount;
			boomCount = initialBoomCount;

			OnItemCountChanged?.Invoke(strikeCount, blackHoleCount, boomCount);
		}

		public int GetStrikeCount() => strikeCount;
		public int GetBlackHoleCount() => blackHoleCount;
		public int GetBoomCount() => boomCount;

		#endregion

		#region Events

		private void SubscribeEvents()
		{
			if (slotManager != null)
			{
				slotManager.OnMatch += OnMatchHandler;
				slotManager.OnGameOver += OnGameOver;
				slotManager.OnLevelClear += LevelClear;
			}
		}

		private void UnsubscribeEvents()
		{
			if (slotManager != null)
			{
				slotManager.OnMatch -= OnMatchHandler;
				slotManager.OnGameOver -= OnGameOver;
				slotManager.OnLevelClear -= LevelClear;
			}
		}

		#endregion

		#region Clear Check

		private void CheckLevelClear()
		{
			if (CurrentState != GameState.Playing) return;

			bool boardEmpty = boardManager == null || !boardManager.HasRemainingTiles();
			bool slotEmpty = slotManager == null || slotManager.CurrentTileCount == 0;

			if (boardEmpty && slotEmpty)
			{
				LevelClear();
			}
		}

		#endregion

		#region Save/Load

		/// <summary>
		/// 레벨 클리어 시 진행 상황 저장
		/// </summary>
		private void SaveLevelProgress(int level, int stars)
		{
			Debug.Log($"[GameManager] SaveLevelProgress - Level: {level}, Stars: {stars}");

			// UserDataManager가 있으면 사용
			if (UserDataManager.Instance != null)
			{
				UserDataManager.Instance.ClearStage(level, stars);
				Debug.Log($"[GameManager] Saved via UserDataManager - NextStage: {UserDataManager.Instance.CurrentStage}");
			}
			else
			{
				// Fallback: PlayerPrefs 직접 사용
				int savedStars = PlayerPrefs.GetInt($"Level_{level}_Stars", 0);
				if (stars > savedStars)
				{
					PlayerPrefs.SetInt($"Level_{level}_Stars", stars);
				}

				int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
				if (level >= unlockedLevel)
				{
					PlayerPrefs.SetInt("UnlockedLevel", level + 1);
				}

				PlayerPrefs.SetInt("CurrentLevel", level + 1);
				PlayerPrefs.Save();

				Debug.Log($"[GameManager] Saved via PlayerPrefs - NextLevel: {level + 1}");
			}
		}

		/// <summary>
		/// 시작 시 진행 상황 로드
		/// </summary>
		private void LoadProgress()
		{
			// Inspector에서 startLevel을 1보다 크게 설정했으면 그 값 사용 (디버그용)
			if (startLevel > 1)
			{
				Debug.Log($"[GameManager] Using Inspector startLevel: {startLevel}");
				return;
			}

			// UserDataManager가 있으면 사용
			if (UserDataManager.Instance != null)
			{
				// SelectedStage가 설정되어 있으면 (메인에서 선택한 경우)
				if (UserDataManager.Instance.SelectedStage > 0)
				{
					startLevel = UserDataManager.Instance.SelectedStage;
					Debug.Log($"[GameManager] Using SelectedStage: {startLevel}");
				}
				else
				{
					// 아니면 CurrentStage 사용 (다음 플레이할 스테이지)
					startLevel = UserDataManager.Instance.CurrentStage;
					Debug.Log($"[GameManager] Using CurrentStage: {startLevel}");
				}
			}
			else
			{
				// Fallback: PlayerPrefs
				startLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
				Debug.Log($"[GameManager] Using PlayerPrefs CurrentLevel: {startLevel}");
			}

			// 최대 레벨 제한
			if (MaxLevel > 0)
			{
				startLevel = Mathf.Clamp(startLevel, 1, MaxLevel);
			}
		}

		public int GetLevelStars(int level)
		{
			if (UserDataManager.Instance != null)
			{
				return UserDataManager.Instance.GetStageStars(level);
			}
			return PlayerPrefs.GetInt($"Level_{level}_Stars", 0);
		}

		public int GetUnlockedLevel()
		{
			if (UserDataManager.Instance != null)
			{
				return UserDataManager.Instance.MaxClearedStage + 1;
			}
			return PlayerPrefs.GetInt("UnlockedLevel", 1);
		}

		#endregion
	}
}