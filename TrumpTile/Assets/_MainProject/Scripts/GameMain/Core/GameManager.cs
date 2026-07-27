using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrumpTile.FirebaseLibrary;
using TrumpTile.GameMain.UI;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.Item;
using TrumpTile.LevelEditor.Editor;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using System.Runtime.ExceptionServices;

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
		[SerializeField] private float mTargetIdleTime = 7;

		[Header("Star Config")]
		[SerializeField] private StarConfig mStarConfig;
		private int mStarCount;

		[Header("Scoring")]
		[SerializeField] private int mBaseMatchScore = 100;
		[SerializeField] private int mComboMultiplier = 50;
		[Header("부활 비용")]
		[SerializeField] private int[] mReviveCost = new int[3];
		public int[] ReviveCost => mReviveCost;
		
		[Header("Debug")]
		[SerializeField] private bool mEnableDebugKeys = true;
		[SerializeField] private float mSlowMotionScale = 0.2F;
		private bool mIsSlowMotion = false;
		[SerializeField] private bool mEnableTimerLog = false;
		[SerializeField] private bool mbLevelTestMode = false;
		[Header("빌드 테스트용 참조")]
		[SerializeField] private GameObject mBuildTestObject;
		[SerializeField] private TMP_Text mBuildTestText;
		[Header("젬 생성 테스트")]
		[SerializeField] private bool mbGemTest;
		private float mTimerLogAccumulator = 0F;
		private bool mbIsRetry = false;
		[Header("리소스 컨테이너")]
		[SerializeField] private LevelDifficultyResourceDatabase mLevelDifficultyResourceDatabase;
		public LevelDifficultyResourceDatabase ResourceDatabase => mLevelDifficultyResourceDatabase;

		[Header("타일 착지 젤리 (일일 퍼즐 바다/심해 테마 전용)")]
		[SerializeField] private bool mbEnableTileJelly = true;

		[Header("시간제한 및 별점 적용을 위한 참조")]
		[SerializeField] private ScoreManager mScoreManager;
		// 게임 상태
		public enum EGameState { Loading, Playing, Paused, GameOver, GameClear }
		public EGameState CurrentState { get; private set; }
		private EDifficultyType mELevelDifficulty;
		public EDifficultyType LevelDifficulty => mELevelDifficulty;
		public bool IsIdleProcess;
		//로딩 애니메이션 완료 체크
		public bool LoadingAnimComplete { get; set; }

		// Public 프로퍼티
		public int MatchCount => mMatchCount;
		public int StarCount => mStarCount;

		private int mCurrentLevelIndex;
		public int CurrentLevel => mCurrentLevelIndex + 1;
		public int MaxLevel => DataManager.Instance != null ? DataManager.Instance.TotalStages : 0;

		// 타이머
		private float mElapsedTime;
		public float ElapsedTime => mElapsedTime;
		private float mTargetClearTime;
		private bool mIsTimerFrozen = false;
		private string mTimerString;
		private float mCurrentTime;
		private float mCurrentIdleTime;
		private float mTotalPlayTime;
		public float TotalPlayTime => mTotalPlayTime;
		public float CurrentIdleTime => mCurrentIdleTime;

		// 점수 및 통계
		private int mCurrentScore;
		private int mComboCount;
		private int mMatchedTileCount;
		private int mTotalTileCount;
		private StageScoreData mStageScoreData;
		// 부활 관련
		private bool mbIsTimeOut;
		private int mCurrentReviveCount = 0;
		public int CurrentReviveCount { get => mCurrentReviveCount; set => mCurrentReviveCount = value; }
		public bool FreeReviveStage {get;set;}
		// 튜토리얼 체크
		public bool tutorialComplete { get; set; }
		public bool IsTimerFrozen => mIsTimerFrozen;
		public bool IsGemCollectActive {get; private set;}

		// 챔피언스 리그 모드 체크
		private bool mbIsChampionsMode;
		public bool IsChampionsMode => mbIsChampionsMode;

		// 이벤트
		public event System.Action<int> OnScoreChanged;
		public event System.Action<int> OnComboChanged;
		public event System.Action<int, int> OnProgressChanged;

		#region Unity Lifecycle

		private void Awake()
		{
			Instance = this;

			tutorialComplete = false;
			
			PlayerDataManager.Inst?.Initialize();

			if(ContentManager.Inst.GetContentData<GemCollectContent>("GemCollection") == null)
			{
				IsGemCollectActive = mbGemTest;
			}
			else
			{
				if(ContentManager.Inst.GetContentData<GemCollectContent>("GemCollection").IsActive)
				{
					IsGemCollectActive = true;
				}	
			}

            UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);

            foreach (var item in uiBaseArray)
            {
                item.Initialize();
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
#if UNITY_EDITOR
			int level = UnityEditor.EditorPrefs.GetInt("DebugLevelIndex", mStartLevel);
			await StartLevelAsync(level);
			UnityEditor.EditorPrefs.DeleteKey("DebugLevelIndex");
#else
			await StartLevelAsync(mStartLevel);
#endif
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
			if(CurrentState == EGameState.Playing)
			{
				if(Input.GetMouseButtonDown(0))
				{
					IsIdleProcess = false;
					mCurrentIdleTime = 0;
				}
				else
				{
					if(!IsIdleProcess)
					{
						if(mCurrentIdleTime >= mTargetIdleTime)
						{
							IsIdleProcess = true;
							mCurrentIdleTime = 0;
							BoardManager.Instance.ShowHint();				
						}
						else
						{
							mCurrentIdleTime += Time.deltaTime;
						}
					}		
				}
			}
			if (CurrentState == EGameState.Playing && !mIsTimerFrozen)
			{
				if(!mbLevelTestMode)
				{
                    mElapsedTime += Time.deltaTime;
					mTotalPlayTime += Time.deltaTime;
                    if (mEnableTimerLog)
                    {
                        mTimerLogAccumulator += Time.deltaTime;
                        if (mTimerLogAccumulator >= 1F)
                        {
                            mTimerLogAccumulator -= 1F;
                            int minutes = Mathf.FloorToInt(mElapsedTime / 60F);
                            int seconds = Mathf.FloorToInt(mElapsedTime % 60F);
                            Debug.Log($"[GameManager] Timer: {minutes:D2}:{seconds:D2} ({mElapsedTime:F2}s) | Target: {mTargetClearTime:F1}s");

                            int limitMinutes = Mathf.FloorToInt(mCurrentTime / 60F);
                            int limitSeconds = Mathf.FloorToInt(mCurrentTime % 60F);

                            mTimerString = $"{limitMinutes:D1}:{limitSeconds:D2}";
                        }
                    }
                    mCurrentTime = mTargetClearTime - mElapsedTime;
                    if (mCurrentTime <= 0)
					{
						//시간이 다 됐어도 마이너스로 두지 않고 0에서 멈춘 뒤,
						//슬롯 이동/매치가 진행 중이면 결과를 기다렸다가 승패를 판정한다.
						mCurrentTime = 0;
						HandleTimeOver();
					}
                }		
            }
			if(Input.GetKeyDown(KeyCode.Escape))
			{
				if(CurrentState == EGameState.GameClear) return;
				EventManager.Inst.ActiveEvent(EventKeys.ON_EXIT_BUTTON);
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
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                FindObjectOfType<MatchTutorialPopup>(true)?.Show();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                FindObjectOfType<SlotTutorialPopup>(true)?.Show();
            }
			if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayerDataManager.Inst.AddGold(1000);
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
			CoreContainer.RewardContainer.Clear();
			int maxLevel = MaxLevel;
			mCurrentLevelIndex = maxLevel > 0
				? Mathf.Clamp(levelNumber - 1, 0, maxLevel - 1)
				: levelNumber - 1;

			CurrentState = EGameState.Loading;


			LoadingAnimComplete = false;
      		tutorialComplete = false;
			mStarCount = 0;

			if(levelNumber <= 5 && !mbIsChampionsMode)
			{
				FreeReviveStage = true;
			}

			LevelData levelData;
			
			if (DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
			{
				AssetReferenceT<LevelData> assetRef = DailyPuzzleManager.Inst.GetTodayAssetRef();
				if (assetRef == null)
				{
					Debug.LogError("[GameManager] Daily puzzle assetRef is null. Exiting daily mode.");
					DailyPuzzleManager.Inst.ExitDailyMode();
					levelData = await DataManager.Instance.LoadLevelAsync(levelNumber);
				}
				else
				{
					FreeReviveStage = false;
					levelData = await DataManager.Instance.LoadDailyLevelAsync(assetRef);
					mStageScoreData = mScoreManager.GetDailyPuzzleStageScoreData(DailyPuzzleManager.Inst.GetTodayIndex());
				}
			}
			else if(mbIsChampionsMode)
			{
				levelData = await DataManager.Instance.LoadChampionsLevelAsync(PlayerDataManager.Inst.ChampionsLevel);
				mStageScoreData = mScoreManager.GetStageScoreData(levelData.levelNumber);
			}
			else
			{
				levelData = await DataManager.Instance.LoadLevelAsync(levelNumber);
				mStageScoreData = mScoreManager.GetStageScoreData(levelNumber);
			}

			if (levelData == null)
			{
				mBuildTestObject.SetActive(true);
				mBuildTestText.text = "레벨 데이터 로드에 실패했습니다. 5초 후 메인 화면으로 돌아갑니다.";
				
				Debug.LogError($"[GameManager] LevelData load failed: Level {levelNumber}");
				StartCoroutine(Co_BuildTest_ToMain());
				return;
			}
			mLevelDifficultyResourceDatabase.Initialize(levelData);

			// 착지 젤리: 토글 ON + 일일 퍼즐 + Water/Dark(바다/심해) 테마일 때만 켠다.
			bool bIsDailyForJelly = DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive;
			ELevelTheme currentTheme = mLevelDifficultyResourceDatabase.GetCurrentTheme(bIsDailyForJelly);
			TileJuice.IsEnabled = mbEnableTileJelly
				&& bIsDailyForJelly
				&& (currentTheme == ELevelTheme.Water || currentTheme == ELevelTheme.Dark);

			mELevelDifficulty = levelData.difficulty;
			Debug.Log($"[GameManager] Starting Level {CurrentLevel}: {levelData.levelName}");

			mCurrentScore = 0;
			mComboCount = 0;
			mMatchedTileCount = 0;
			//스테이지 도전 횟수. 재시도도 각각 1회로 집계된다.
			FirebaseAnalyticsService.LogStageStart(CurrentLevel);
			//새 레벨/재시도 시작 시 HUD 입력 다시 활성화 (직전 클리어에서 차단됐을 수 있음)
			IngameViewRef?.SetHudInteractable(true);

			mSlotManager?.Initialize();  // 반드시 ResetSlots() 이전
			mSlotManager?.ResetSlots();

			FindObjectOfType<SlotTensionController>(true)?.ResetForNewStage();

            //임시
            EventManager.Inst.ActiveEvent(EventKeys.INGAME_LOADING_COMPLETE, (object)(levelData, mbIsRetry));

            mBoardManager?.LoadLevel(levelData);

            mTotalTileCount = mBoardManager?.TotalTileCount ?? 0;

			// 타이머 초기화
			mElapsedTime = 0F;
			//재시도/새 스테이지 시작 시 별 계산에 쓰는 누적 플레이시간도 리셋한다.
			//(안 하면 이전 시도 시간이 누적돼 클리어타임/별이 잘못 계산됨. 부활은 이 경로를 안 타므로 부활 페널티는 유지)
			mTotalPlayTime = 0F;

			mTargetClearTime = mStageScoreData.TimeLimit;


            Debug.Log($"[GameManager] TargetClearTime: {mTargetClearTime}s (tiles: {mTotalTileCount})");

            int limitMinutes = Mathf.FloorToInt(mTargetClearTime / 60F);
            int limitSeconds = Mathf.FloorToInt(mTargetClearTime % 60F);

            mTimerString = $"{limitMinutes:D1}:{limitSeconds:D2}";
			mCurrentTime = mTargetClearTime;

            //임시
            EventManager.Inst.ActiveEvent(EventKeys.TIMER_SETTING_COMPLETE);

           // UIManager.Instance?.UpdateLevel(CurrentLevel);
			UIManager.Instance?.UpdateScore(mCurrentScore);
			UIManager.Instance?.RefreshAllItemButtons();
			OnScoreChanged?.Invoke(mCurrentScore);
			OnComboChanged?.Invoke(0);		

      		await WaitUntill(() => LoadingAnimComplete);

			if (DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
			{
				tutorialComplete = true;
			}

			await WaitUntill(() => tutorialComplete);

			Debug.Log("게임 시작");

            CurrentState = EGameState.Playing;
		}
		private IEnumerator Co_BuildTest_ToMain()
		{
			yield return new WaitForSeconds(5f);
			SceneTransister.Inst.TransistScene("MainScene");
		}
		public void RestartLevel()
		{
			mbIsRetry = true;
			Debug.Log($"[GameManager] RestartLevel - Level {CurrentLevel}");
			AudioManager.Inst.SetBGMVolume(1f);
			EventManager.Inst.ActiveEvent(EventKeys.RESTART_LEVEL);

			mCurrentReviveCount = 0;
			mbIsTimeOut = false;

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
			DailyPuzzleManager.Inst?.ExitDailyMode();
			//AudioEvent.Play(EAudioKey.BGM_Main);
			
			AudioManager.Inst.SetBGMVolume(1f);

			Time.timeScale = 1f;
			if (TransitionManager.Instance != null)
			{
				TransitionManager.Instance.LoadScene("MainScene");
			}
			else
			{
				SceneTransister.Inst.TransistScene("MainScene");
			}
			Destroy(Instance);
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
		public void IncreaseBonusGold()
		{
			CoreContainer.RewardContainer.AddGold(1);
		}
		public void IncreaseBonusGoldWithMatch()
		{
			CoreContainer.RewardContainer.AddGold(3);
		}
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
		}

		#endregion

		#region Game State
		public string GetCurrentTimeString()
		{
			return mTimerString;
        }
		public float GetCurrentTimeClamped()
		{
			return mCurrentTime / mTargetClearTime;
		}
		//시간이 0에 도달했을 때의 처리.
		//슬롯에 타일이 들어오는 중이거나 매치/재정렬이 진행 중이면(=마지막 순간에 매치가 성립될 수 있으면)
		//결과가 확정될 때까지 패배 처리를 보류한다.
		//진행이 끝난 뒤 보드/슬롯이 모두 비어 클리어면 승리 처리를 하고, 타일이 남아 있으면 타임오버 패배로 처리한다.
		private void HandleTimeOver()
		{
			//슬롯 이동/매치 연출이 진행 중이면 이번 프레임은 대기 (승패 판정 보류)
			if (mSlotManager != null && mSlotManager.HasPendingSlotResolution)
			{
				return;
			}

			bool bBoardEmpty = mBoardManager == null || !mBoardManager.HasRemainingTiles();
			bool bSlotEmpty = mSlotManager == null || mSlotManager.CurrentTileCount == 0;

			if (bBoardEmpty && bSlotEmpty)
			{
				LevelClear();
				return;
			}

			mbIsTimeOut = true;
			OnGameOver();
		}
		public void OnGameOver()
		{
			if (CurrentState == EGameState.GameOver || CurrentState == EGameState.GameClear)
			{
				return;
			}
			
			AudioManager.Inst.SetBGMVolume(0.2f);

			Debug.Log("[GameManager] Game Over!");

			CurrentState = EGameState.GameOver;

			ItemManager.Inst.SaveItemCountsToServer();
			UIManager.Instance?.DisableItemButtons();
			EffectManager.Instance?.PlayGameOverEffect();
			AudioEvent.Play(EAudioKey.SFX_StageLosed);

			//사망 횟수. 사유를 같이 남겨서 시간초과/슬롯가득을 구분할 수 있게 한다.
			FirebaseAnalyticsService.LogStageFail(CurrentLevel, mbIsTimeOut ? "time_out" : "slot_full");

			if(mbIsTimeOut)
			{
				EventManager.Inst.ActiveEvent(EventKeys.GAME_OVER_TIME_OUT);
			}
			else
			{
				EventManager.Inst.ActiveEvent(EventKeys.GAME_OVER_SLOT_FULL);
			}
		}

		private void OnContinueGame()
		{
			Debug.Log("[GameManager] Continue game - Revive");

			AudioManager.Inst.SetBGMVolume(1f);
			
			CurrentState = EGameState.Playing;

			mSlotManager?.ResumeGame();

			mSlotManager?.RemoveSlotLastIndexTileToBoard();
			mSlotManager?.RemoveSlotLastIndexTileToBoard();

			UIManager.Instance?.UpdateItemButtonStates();
		}
		public void ContinueGame()
		{
			if(mbIsTimeOut)
			{
				mElapsedTime = 0;
				//부활 시 타이머를 새로 주므로 플레이시간도 초기화 → 별점이 부활 타이머 기준으로 계산됨
				mTotalPlayTime = 0;
				mTargetClearTime = 40;

				AudioManager.Inst.SetBGMVolume(1f);

				mSlotManager?.ResumeGame();

				CurrentState = EGameState.Playing;

				mbIsTimeOut = false;
			}
			else
			{
				if(mCurrentTime < 20)
				{
					mElapsedTime = 0;
					//부활 시 타이머를 새로 주므로 플레이시간도 초기화
					mTotalPlayTime = 0;
					mTargetClearTime = 30;
				}
				AudioManager.Inst.SetBGMVolume(1f);

				mSlotManager?.ResumeGame();
				mSlotManager?.RemoveAllTile();

				CurrentState = EGameState.Playing;
			}
		}
		public void LevelClear()
		{
			if (CurrentState == EGameState.GameClear || CurrentState == EGameState.GameOver)
			{
				return;
			}
			StartCoroutine(LevelClearCoroutine());
		}

		private IEnumerator LevelClearCoroutine()
		{
			CurrentState = EGameState.GameClear;

			AudioManager.Inst.SetBGMVolume(0.2f);
			//클리어 횟수. 별/플레이시간을 같이 남겨 난이도 분석에 쓴다.
			FirebaseAnalyticsService.LogStageClear(CurrentLevel, StarCount, mTotalPlayTime);

			PlayerDataManager.Inst.AddGold(CoreContainer.RewardContainer.Gold);
			PlayerDataManager.Inst.AddGemCount(CoreContainer.RewardContainer.Gem);
			ItemManager.Inst.SaveItemCountsToServer();
			UIManager.Instance?.DisableItemButtons();
			//클리어 즉시 HUD 입력 차단(슬롯구매/설정/상점/아이템). 팝업 뜨기 전 창에서의 오클릭 방지.
			IngameViewRef?.SetHudInteractable(false);

			yield return new WaitForSeconds(0.5F);

			//EffectManager.Instance?.PlayClearEffect();

			mStarCount = CalculateStars();
      
			bool bIsDailyMode = DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive;
			if (bIsDailyMode)
			{
				DailyPuzzleManager.Inst.OnDailyClear();
			}
			else
			{
				SaveLevelProgress(CurrentLevel, mStarCount);
			}

			yield return new WaitForSeconds(0.5F);

			EventManager.Inst.ActiveEvent(EventKeys.LEVEL_CLEAR);
			// VictoryPopup 표시
      
			if (mVictoryPopup != null)
			{
				bool bHasNext = !bIsDailyMode && HasNextLevel();
				Debug.Log($"[GameManager] Showing VictoryPopup - Level: {CurrentLevel}, HasNext: {bHasNext}");
				mVictoryPopup.Show(CurrentLevel, mElapsedTime, mStarCount, bHasNext);
			}
			else
			{
				Debug.LogWarning("[GameManager] VictoryPopup is null!");
				UIManager.Instance?.ShowLevelClearPanel(mStarCount);
			}
		}

		private int CalculateStars()
		{
			// if (mStarConfig == null)
			// {
			// 	Debug.LogWarning("[GameManager] StarConfig is null, defaulting to 1 star");
			// 	return 1;
			// }

			// if (mElapsedTime <= mTargetClearTime)
			// {
			// 	return 3;
			// }
			// else if (mElapsedTime <= mTargetClearTime * mStarConfig.Star2TimeRatio)
			// {
			// 	return 2;
			// }
			// else
			// {
			// 	return 1;
			// }
			//별점은 현재 타이머(mTargetClearTime) 기준으로 남은시간을 판정한다.
			//부활하면 mTargetClearTime이 40/30초로 바뀌므로 부활 타이머의 남은시간 기준으로 계산되고,
			//부활을 안 하면 mTargetClearTime == 스테이지 제한시간이라 기존과 동일하다.
			float star3 = mTargetClearTime - mStageScoreData.Star3;
			float star2 = mTargetClearTime - mStageScoreData.Star2;
			if(mTotalPlayTime <= star3)
			{
				return 3;
			}
			else if(mTotalPlayTime <= star2)
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
			//AudioEvent.Pause();
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

		//인게임 HUD 입력 차단용 IngameView 참조 (지연 캐싱)
		private IngameView mIngameViewCache;
		private IngameView IngameViewRef => mIngameViewCache != null ? mIngameViewCache : (mIngameViewCache = FindObjectOfType<IngameView>(true));

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
			if(mbIsChampionsMode)
			{
				Debug.Log($"[GameManager] ClearChampionsLevel Progress, Next Level : {PlayerDataManager.Inst.ChampionsLevel + 1}");
				PlayerDataManager.Inst.ClearChampionsStage();

				//로컬 저장 후 서버에도 저장 (로그인 보장 포함, 오프라인이면 조용히 스킵)
				_ = ServerSyncService.SaveToServer();
				return;
			}

			Debug.Log($"[GameManager] SaveLevelProgress - Level: {level}, Stars: {stars}");
			PlayerDataManager.Inst.ClearStage(level, stars);
			Debug.Log($"[GameManager] Saved - NextStage: {PlayerDataManager.Inst.CurrentStage}");

			//로컬 저장 후 서버에도 저장 (로그인 보장 포함, 오프라인이면 조용히 스킵)
			_ = ServerSyncService.SaveToServer();
		}

		/// <summary>
		/// 시작 시 진행 상황 로드
		/// </summary>
		private void LoadProgress()
		{
			if (DailyPuzzleManager.Inst != null && DailyPuzzleManager.Inst.IsActive)
			{
				return;
			}

			if(PlayerDataManager.Inst.IsChampionsActive)
			{
				mbIsChampionsMode = true;
				mStartLevel = PlayerDataManager.Inst.ChampionsLevel % CoreData.CHAMPIONS_INTERVAL;
				if(mStartLevel <= 0)
				{
					mStartLevel = 1;
				}
				return;
			}

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
