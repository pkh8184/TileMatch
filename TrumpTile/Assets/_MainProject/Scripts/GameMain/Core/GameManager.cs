using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrumpTile.GameMain.UI;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using TrumpTile.LevelEditor;
using System;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 게임 전체 상태 및 흐름 관리
	///
	///</summary>
	public class GameManager : MonoBehaviour, ITimerControllable
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
		[SerializeField] private int mMaxSlots = 6;

		[Header("Star Config")]
		[SerializeField] private StarConfig mStarConfig;

		[Header("Scoring")]
		[SerializeField] private int mBaseMatchScore = 100;
		[SerializeField] private int mComboMultiplier = 50;

		[Header("Debug")]
		[SerializeField] private bool mEnableDebugKeys = true;
		[SerializeField] private float mSlowMotionScale = 0.2F;
		private bool mIsSlowMotion = false;
		[SerializeField] private bool mEnableTimerLog = false;
		private float mTimerLogAccumulator = 0F;

		// 게임 상태
		public enum EGameState { Loading, Playing, Paused, GameOver, GameClear }
		public EGameState CurrentState { get; private set; }

		//로딩 애니메이션 완료 체크
		public bool LoadingAnimComplete { get; set; }

		// Public 프로퍼티
		public int MatchCount => mMatchCount;


		private int mCurrentLevelIndex;
		public int CurrentLevel => mCurrentLevelIndex + 1;
		public int MaxLevel => DataManager.Instance != null ? DataManager.Instance.TotalStages : 0;

		// 타이머
		private float mElapsedTime;
		private float mTargetClearTime;
		private bool mIsTimerFrozen = false;

		// 점수 및 통계
		private int mCurrentScore;
		private int mComboCount;
		private int mMatchedTileCount;
		private int mTotalTileCount;

		// 이벤트
		public event System.Action<int> OnScoreChanged;
		public event System.Action<int> OnComboChanged;
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

            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);

            if (uiBaseArray != null)
            {
                foreach (UIBase uiBase in uiBaseArray)
                {
                    uiBase.Initialize();
                }
            }
            else
            {
                Debug.Log("UIBase를 찾지 못했습니다.");
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
			ItemManager.Inst.Initialize(mBoardManager, mSlotManager, EffectManager.Instance, this, mMatchCount);

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
			if (CurrentState == EGameState.Playing && !mIsTimerFrozen)
			{
				mElapsedTime += Time.deltaTime;

				if (mEnableTimerLog)
				{
					mTimerLogAccumulator += Time.deltaTime;
					if (mTimerLogAccumulator >= 1F)
					{
						mTimerLogAccumulator -= 1F;
						int minutes = Mathf.FloorToInt(mElapsedTime / 60F);
						int seconds = Mathf.FloorToInt(mElapsedTime % 60F);
						Debug.Log($"[GameManager] Timer: {minutes:D2}:{seconds:D2} ({mElapsedTime:F2}s) | Target: {mTargetClearTime:F1}s");
					}
				}
			}

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

			mSlotManager?.Initialize();  // 반드시 ResetSlots() 이전
		mSlotManager?.ResetSlots();
			mBoardManager?.LoadLevel(levelData);

			mTotalTileCount = mBoardManager?.TotalTileCount ?? 0;

			// 타이머 초기화
			mElapsedTime = 0F;
			mTargetClearTime = mStarConfig != null
				? mTotalTileCount * mStarConfig.TileTimeCoefficient
				: mTotalTileCount * 2.0F;

			Debug.Log($"[GameManager] TargetClearTime: {mTargetClearTime}s (tiles: {mTotalTileCount})");

            UIManager.Instance?.UpdateLevel(CurrentLevel);
			UIManager.Instance?.UpdateScore(mCurrentScore);
			UIManager.Instance?.RefreshAllItemButtons();
			OnScoreChanged?.Invoke(mCurrentScore);
			OnComboChanged?.Invoke(0);

            //임시
            EventManager.Inst.ActiveEvent("IngameLoadingComplete", (object)levelData.levelBackgroundSprite);

            await WaitUntill(() => LoadingAnimComplete);

            CurrentState = EGameState.Playing;
		}
		public void RestartLevel()
		{
			Debug.Log($"[GameManager] RestartLevel - Level {CurrentLevel}");
			StartLevel(CurrentLevel);
		}
		private async Task WaitUntill(Func<bool> condition)
		{
			while(!condition())
			{
				await Task.Yield();
			}
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

			AudioEvent.Play(EAudioKey.BGM_MainMenu);

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
				AudioEvent.Play(EAudioKey.SFX_Combo, mComboCount);
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

			ItemManager.Inst.SaveItemCountsToServer();
			UIManager.Instance?.DisableItemButtons();
			EffectManager.Instance?.PlayGameOverEffect();
			AudioEvent.Play(EAudioKey.SFX_GameOver);

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

			ItemManager.Inst.SaveItemCountsToServer();
			UIManager.Instance?.DisableItemButtons();

			yield return new WaitForSeconds(0.5F);

			EffectManager.Instance?.PlayClearEffect();
			AudioEvent.Play(EAudioKey.SFX_GameClear);

			int stars = CalculateStars();

			SaveLevelProgress(CurrentLevel, stars);

			yield return new WaitForSeconds(0.5F);

			// VictoryPopup 표시
			if (mVictoryPopup != null)
			{
				bool bHasNext = HasNextLevel();
				Debug.Log($"[GameManager] Showing VictoryPopup - Level: {CurrentLevel}, HasNext: {bHasNext}");
				mVictoryPopup.Show(CurrentLevel, mElapsedTime, stars, bHasNext);
			}
			else
			{
				Debug.LogWarning("[GameManager] VictoryPopup is null!");
				UIManager.Instance?.ShowLevelClearPanel(stars);
			}
		}

		private int CalculateStars()
		{
			if (mStarConfig == null)
			{
				Debug.LogWarning("[GameManager] StarConfig is null, defaulting to 1 star");
				return 1;
			}

			if (mElapsedTime <= mTargetClearTime)
			{
				return 3;
			}
			else if (mElapsedTime <= mTargetClearTime * mStarConfig.Star2TimeRatio)
			{
				return 2;
			}
			else
			{
				return 1;
			}
		}

		public void PauseGame()
		{
			if (CurrentState != EGameState.Playing)
			{
				return;
			}

			CurrentState = EGameState.Paused;
			Time.timeScale = 0F;
			AudioEvent.Pause();
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
			AudioEvent.Resume();
		}

		#endregion

		#region Items

		public bool CanUseItem() => CurrentState == EGameState.Playing;

		#endregion

		#region ITimerControllable

		public void FreezeTimer(float seconds)
		{
			StartCoroutine(FreezeTimerCoroutine(seconds));
		}

		private IEnumerator FreezeTimerCoroutine(float seconds)
		{
			mIsTimerFrozen = true;
			yield return new WaitForSeconds(seconds);
			mIsTimerFrozen = false;
		}

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

			if (mGameOverPopup != null)
			{
				mGameOverPopup.OnContinue += OnContinueGame;
				mGameOverPopup.OnRestart += RestartLevel;
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

			if (mGameOverPopup != null)
			{
				mGameOverPopup.OnContinue -= OnContinueGame;
				mGameOverPopup.OnRestart -= RestartLevel;
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

			PlayerDataManager.Inst.ClearStage(level, stars);
			Debug.Log($"[GameManager] Saved - NextStage: {PlayerDataManager.Inst.CurrentStage}");
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

			int bSelectedStage = PlayerDataManager.Inst.SelectedStage;
			if (bSelectedStage > 0)
			{
				mStartLevel = bSelectedStage;
				Debug.Log($"[GameManager] Using SelectedStage: {mStartLevel}");
			}
			else
			{
				mStartLevel = PlayerDataManager.Inst.CurrentStage;
				Debug.Log($"[GameManager] Using CurrentStage: {mStartLevel}");
			}

			// 최대 레벨 제한
			if (MaxLevel > 0)
			{
				mStartLevel = Mathf.Clamp(mStartLevel, 1, MaxLevel);
			}
		}

		public int GetLevelStars(int level)
		{
			return PlayerDataManager.Inst.GetStageStars(level);
		}

		public int GetUnlockedLevel()
		{
			return PlayerDataManager.Inst.MaxClearedStage + 1;
		}

		#endregion
	}
}
