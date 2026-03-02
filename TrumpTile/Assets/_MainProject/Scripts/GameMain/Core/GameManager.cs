using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrumpTile.GameMain.UI;
using TrumpTile.GameMain.Data;
using TrumpTile.LevelEditor;

namespace TrumpTile.GameMain.Core
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
		[SerializeField] private BoardManager mBoardManager;
		[SerializeField] private SlotManager mSlotManager;
		[SerializeField] private GameOverPopup mGameOverPopup;
		[SerializeField] private VictoryPopup mVictoryPopup;

		[Header("Level Settings")]
		[SerializeField] private int mStartLevel = 1;

		[Header("Game Rules")]
		[SerializeField] private int mMatchCount = 3;
		[SerializeField] private int mMaxSlots = 7;

		[Header("Scoring")]
		[SerializeField] private int mBaseMatchScore = 100;
		[SerializeField] private int mComboMultiplier = 50;
		[SerializeField] private int[] mStarThresholds = { 1000, 2000, 3000 };

		[Header("Items")]
		[SerializeField] private int mInitialStrikeCount = 3;
		[SerializeField] private int mInitialBlackHoleCount = 3;
		[SerializeField] private int mInitialBoomCount = 3;

		[Header("Debug")]
		[SerializeField] private bool mEnableDebugKeys = true;
		[SerializeField] private float mSlowMotionScale = 0.2F;
		private bool mIsSlowMotion = false;

		// 게임 상태
		public enum EGameState { Loading, Playing, Paused, GameOver, GameClear }
		public EGameState CurrentState { get; private set; }

		// Public 프로퍼티
		public int MatchCount => mMatchCount;


		private int mCurrentLevelIndex;
		public int CurrentLevel => mCurrentLevelIndex + 1;
		public int MaxLevel => DataManager.Instance != null ? DataManager.Instance.TotalStages : 0;

		// 점수 및 통계
		private int mCurrentScore;
		private int mComboCount;
		private int mMatchedTileCount;
		private int mTotalTileCount;

		// 아이템
		private int mStrikeCount;
		private int mBlackHoleCount;
		private int mBoomCount;

		// 아이템 사용 중 플래그
		private bool mIsItemInProgress = false;

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
		}

		private async void Start()
		{
			// DataManager 초기화 대기
			while (DataManager.Instance == null || !DataManager.Instance.IsInitialized)
			{
				await Task.Yield();
			}

			LoadProgress();
			SubscribeEvents();
			InitializeItems();

			Debug.Log($"[GameManager] Starting level: {mStartLevel}");
			await StartLevelAsync(mStartLevel);
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
			if (mEnableDebugKeys)
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
				mIsSlowMotion = !mIsSlowMotion;
				Time.timeScale = mIsSlowMotion ? mSlowMotionScale : 1F;
				Debug.Log($"[GameManager] SlowMotion: {mIsSlowMotion}");
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
		public void StartLevel(int levelNumber)
		{
			_ = StartLevelAsync(levelNumber);
		}

		private async Task StartLevelAsync(int levelNumber)
		{
			int maxLevel = MaxLevel;
			mCurrentLevelIndex = maxLevel > 0
				? Mathf.Clamp(levelNumber - 1, 0, maxLevel - 1)
				: levelNumber - 1;

			CurrentState = EGameState.Loading;

			LevelData levelData = await DataManager.Instance.LoadLevelAsync(levelNumber);
			if (levelData == null)
			{
				Debug.LogError($"[GameManager] LevelData load failed: Level {levelNumber}");
				return;
			}

			Debug.Log($"[GameManager] Starting Level {CurrentLevel}: {levelData.levelName}");

			mCurrentScore = 0;
			mComboCount = 0;
			mMatchedTileCount = 0;
			mIsItemInProgress = false;

			mSlotManager?.ResetSlots();
			mBoardManager?.LoadLevel(levelData);

			mTotalTileCount = mBoardManager?.TotalTileCount ?? 0;

			UIManager.Instance?.UpdateLevel(CurrentLevel);
			UIManager.Instance?.UpdateScore(mCurrentScore);
			OnScoreChanged?.Invoke(mCurrentScore);
			OnComboChanged?.Invoke(0);
			OnItemCountChanged?.Invoke(mStrikeCount, mBlackHoleCount, mBoomCount);

			CurrentState = EGameState.Playing;
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

			if (HasNextLevel())
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
			return CurrentLevel < MaxLevel;
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
			mCurrentScore += amount;
			OnScoreChanged?.Invoke(mCurrentScore);
		}

		public int GetScore() => mCurrentScore;

		#endregion

		#region Match Handler

		private void OnMatchHandler(int matchedCount)
		{
			mComboCount++;
			OnComboChanged?.Invoke(mComboCount);

			if (mComboCount > 1)
			{
				AddScore(mComboMultiplier * (mComboCount - 1));
			}

			mMatchedTileCount += matchedCount;
			OnProgressChanged?.Invoke(mMatchedTileCount, mTotalTileCount);

			if (mComboCount > 1)
			{
				AudioManager.Instance?.PlayMatchSound(mComboCount);
			}
		}

		#endregion

		#region Game State

		public void OnGameOver()
		{
			if (CurrentState == EGameState.GameOver)
			{
				return;
			}

			Debug.Log("[GameManager] Game Over!");

			CurrentState = EGameState.GameOver;

			UIManager.Instance?.DisableItemButtons();
			EffectManager.Instance?.PlayGameOverEffect();
			AudioManager.Instance?.PlayGameOver();

			if (mGameOverPopup != null)
			{
				mGameOverPopup.Show();
			}
		}

		private void OnContinueGame()
		{
			Debug.Log("[GameManager] Continue game - Revive");

			CurrentState = EGameState.Playing;

			mSlotManager?.ResumeGame();

			mSlotManager?.RemoveOneTileToBoard();
			mSlotManager?.RemoveOneTileToBoard();

			UIManager.Instance?.UpdateItemButtonStates();
		}

		public void LevelClear()
		{
			if (CurrentState == EGameState.GameClear)
			{
				return;
			}
			StartCoroutine(LevelClearCoroutine());
		}

		private IEnumerator LevelClearCoroutine()
		{
			CurrentState = EGameState.GameClear;

			UIManager.Instance?.DisableItemButtons();

			yield return new WaitForSeconds(0.5F);

			EffectManager.Instance?.PlayClearEffect();
			AudioManager.Instance?.PlayGameClear();

			int stars = CalculateStars();

			// UserDataManager에 클리어 정보 저장
			SaveLevelProgress(CurrentLevel, stars);

			yield return new WaitForSeconds(0.5F);

			// VictoryPopup 표시
			if (mVictoryPopup != null)
			{
				bool bHasNext = HasNextLevel();
				Debug.Log($"[GameManager] Showing VictoryPopup - Level: {CurrentLevel}, HasNext: {bHasNext}");
				mVictoryPopup.Show(CurrentLevel, mCurrentScore, stars, bHasNext);
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
			for (int i = 0; i < mStarThresholds.Length; i++)
			{
				if (mCurrentScore >= mStarThresholds[i])
				{
					stars = i + 1;
				}
			}
			return Mathf.Min(stars, 3);
		}

		public void PauseGame()
		{
			if (CurrentState != EGameState.Playing)
			{
				return;
			}

			CurrentState = EGameState.Paused;
			Time.timeScale = 0F;
			AudioManager.Instance?.PauseBGM();
			UIManager.Instance?.ShowPausePanel();
		}

		public void ResumeGame()
		{
			if (CurrentState != EGameState.Paused)
			{
				return;
			}

			CurrentState = EGameState.Playing;
			Time.timeScale = 1F;
			AudioManager.Instance?.ResumeBGM();
		}

		#endregion

		#region Items

		public bool CanUseItem()
		{
			return CurrentState == EGameState.Playing && !mIsItemInProgress;
		}

		public void UseStrike()
		{
			if (!CanUseItem())
			{
				return;
			}
			if (mStrikeCount <= 0)
			{
				return;
			}
			if (mSlotManager == null || mSlotManager.CurrentTileCount == 0)
			{
				return;
			}

			mStrikeCount--;
			OnItemCountChanged?.Invoke(mStrikeCount, mBlackHoleCount, mBoomCount);

			StartCoroutine(StrikeCoroutine());
		}

		private IEnumerator StrikeCoroutine()
		{
			mIsItemInProgress = true;

			Vector3 popPosition = mSlotManager.GetLastTilePosition();

			EffectManager.Instance?.PlayStrikePopEffect(popPosition);
			AudioManager.Instance?.PlayItemUse();

			yield return new WaitForSeconds(0.3F);

			Vector3 landPosition;
			bool bSuccess = mSlotManager.RemoveOneTileToBoard(out landPosition);

			if (bSuccess)
			{
				Vector3 actualLandPosition = mBoardManager?.GetLastPlacedTilePosition() ?? landPosition;
				EffectManager.Instance?.PlayStrikeLandEffect(actualLandPosition);
			}

			yield return new WaitForSeconds(0.2F);

			mIsItemInProgress = false;
		}

		public void UseBlackHole()
		{
			if (!CanUseItem())
			{
				return;
			}
			if (mBlackHoleCount <= 0)
			{
				return;
			}
			if (mBoardManager == null || !mBoardManager.HasRemainingTiles())
			{
				return;
			}

			mBlackHoleCount--;
			OnItemCountChanged?.Invoke(mStrikeCount, mBlackHoleCount, mBoomCount);

			StartCoroutine(BlackHoleCoroutine());
		}

		private IEnumerator BlackHoleCoroutine()
		{
			mIsItemInProgress = true;

			List<TileController> boardTiles = mBoardManager.GetBoardTiles();

			List<Transform> tileTransforms = boardTiles
				.Where(t => t != null)
				.Select(t => t.transform)
				.ToList();

			// EffectManager가 타일 위치 복원 애니메이션까지 담당하므로 중복 복원 제거
			bool bEffectComplete = false;
			EffectManager.Instance?.PlayBlackHoleEffect(
				tileTransforms,
				() => { },
				() =>
				{
					mBoardManager.StartCoroutine(mBoardManager.ShuffleBoardAnimated());
					bEffectComplete = true;
				}
			);

			// 이펙트 완료 콜백 대기 (타임아웃 5초)
			float timeout = 5F;
			float elapsed = 0F;
			while (!bEffectComplete && elapsed < timeout)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			yield return new WaitForSeconds(0.2F);

			mIsItemInProgress = false;
		}

		public void UseBoom()
		{
			if (!CanUseItem())
			{
				return;
			}
			if (mBoomCount <= 0)
			{
				return;
			}

			List<TileController> allBoardTiles = mBoardManager?.GetBoardTiles() ?? new List<TileController>();
			List<TileController> allSlotTiles = mSlotManager?.GetAllSlotTiles() ?? new List<TileController>();

			List<TileController> allTiles = new List<TileController>();
			allTiles.AddRange(allBoardTiles);
			allTiles.AddRange(allSlotTiles);

			List<IGrouping<string, TileController>> selectableTiles = allTiles
				.Where(t => t != null && t.Data != null)
				.GroupBy(t => t.Data.TileID)
				.Where(g => g.Count() >= mMatchCount)
				.ToList();

			if (selectableTiles.Count == 0)
			{
				return;
			}

			mBoomCount--;
			OnItemCountChanged?.Invoke(mStrikeCount, mBlackHoleCount, mBoomCount);

			StartCoroutine(BoomCoroutine(selectableTiles));
		}

		private IEnumerator BoomCoroutine(List<IGrouping<string, TileController>> groups)
		{
			mIsItemInProgress = true;

			AudioManager.Instance?.PlayItemUse();

			int setsToRemove = Mathf.Min(3, groups.Count);

			List<Vector3> allPositions = new List<Vector3>();
			List<TileController> allTilesToRemove = new List<TileController>();

			for (int i = 0; i < setsToRemove; i++)
			{
				IGrouping<string, TileController> group = groups[i];
				List<TileController> tilesToRemove = group.Take(mMatchCount).ToList();

				foreach (TileController tile in tilesToRemove)
				{
					if (tile != null)
					{
						allPositions.Add(tile.transform.position);
						allTilesToRemove.Add(tile);
					}
				}
			}

			bool bEffectComplete = false;
			EffectManager.Instance?.PlayBoomEffect(allPositions, () => { bEffectComplete = true; });

			foreach (TileController tile in allTilesToRemove)
			{
				if (tile != null)
				{
					if (tile.IsInSlot)
					{
						mSlotManager?.RemoveTileDirectly(tile);
					}
					else
					{
						mBoardManager?.RemoveTile(tile);
					}

					tile.Remove();
				}
			}

			float timeout = 2F;
			float elapsed = 0F;
			while (!bEffectComplete && elapsed < timeout)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}

			mBoardManager?.UpdateAllBlockedStates();

			yield return new WaitForSeconds(0.3F);

			mIsItemInProgress = false;

			CheckLevelClear();
		}

		private void InitializeItems()
		{
			mStrikeCount = mInitialStrikeCount;
			mBlackHoleCount = mInitialBlackHoleCount;
			mBoomCount = mInitialBoomCount;

			OnItemCountChanged?.Invoke(mStrikeCount, mBlackHoleCount, mBoomCount);
		}

		public int GetStrikeCount() => mStrikeCount;
		public int GetBlackHoleCount() => mBlackHoleCount;
		public int GetBoomCount() => mBoomCount;

		#endregion

		#region Events

		private void SubscribeEvents()
		{
			if (mSlotManager != null)
			{
				mSlotManager.OnMatch += OnMatchHandler;
				mSlotManager.OnGameOver += OnGameOver;
				mSlotManager.OnLevelClear += LevelClear;
			}
		}

		private void UnsubscribeEvents()
		{
			if (mSlotManager != null)
			{
				mSlotManager.OnMatch -= OnMatchHandler;
				mSlotManager.OnGameOver -= OnGameOver;
				mSlotManager.OnLevelClear -= LevelClear;
			}
		}

		#endregion

		#region Clear Check

		private void CheckLevelClear()
		{
			if (CurrentState != EGameState.Playing)
			{
				return;
			}

			bool bBoardEmpty = mBoardManager == null || !mBoardManager.HasRemainingTiles();
			bool bSlotEmpty = mSlotManager == null || mSlotManager.CurrentTileCount == 0;

			if (bBoardEmpty && bSlotEmpty)
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
			// Inspector에서 mStartLevel을 1보다 크게 설정했으면 그 값 사용 (디버그용)
			if (mStartLevel > 1)
			{
				Debug.Log($"[GameManager] Using Inspector startLevel: {mStartLevel}");
				return;
			}

			// UserDataManager가 있으면 사용
			if (UserDataManager.Instance != null)
			{
				// SelectedStage가 설정되어 있으면 (메인에서 선택한 경우)
				if (UserDataManager.Instance.SelectedStage > 0)
				{
					mStartLevel = UserDataManager.Instance.SelectedStage;
					Debug.Log($"[GameManager] Using SelectedStage: {mStartLevel}");
				}
				else
				{
					// 아니면 CurrentStage 사용 (다음 플레이할 스테이지)
					mStartLevel = UserDataManager.Instance.CurrentStage;
					Debug.Log($"[GameManager] Using CurrentStage: {mStartLevel}");
				}
			}
			else
			{
				// Fallback: PlayerPrefs
				mStartLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
				Debug.Log($"[GameManager] Using PlayerPrefs CurrentLevel: {mStartLevel}");
			}

			// 최대 레벨 제한
			if (MaxLevel > 0)
			{
				mStartLevel = Mathf.Clamp(mStartLevel, 1, MaxLevel);
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
