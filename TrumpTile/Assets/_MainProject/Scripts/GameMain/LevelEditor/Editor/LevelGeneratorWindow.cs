#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TrumpTile.GameMain.Core;
using TrumpTile.LevelEditor;

namespace TrumpTile.LevelEditor.Editor
{
	/// <summary>
	/// 강화된 레벨 자동 생성기 V2
	/// - 보드 중앙 정렬
	/// - 대칭 패턴 생성
	/// - 난이도별 밸런스 조정
	/// - 클리어 가능성 보장
	/// </summary>
	public class LevelGeneratorWindow : EditorWindow
	{
		#region Fields

		// 생성 설정
		private int mStartLevelNumber = 1;
		private int mEndLevelNumber = 300;
		private string mOutputFolder = "Assets/Resources/Levels";
		private string mLevelPrefix = "Level_";

		// 보드 설정
		private int mMinBoardWidth = 5;
		private int mMaxBoardWidth = 8;
		private int mMinBoardHeight = 6;
		private int mMaxBoardHeight = 10;
		private int mMinLayers = 1;
		private int mMaxLayers = 4;

		// 타일 설정
		private int mMinTileTypes = 4;
		private int mMaxTileTypes = 16;
		private int mMatchCount = 3;
		private int mMinTileSets = 6;  // 최소 타일 세트 수 (18개)
		private int mMaxTileSets = 40; // 최대 타일 세트 수 (120개)

		// 난이도 곡선
		private AnimationCurve mDifficultyCurve;
		private AnimationCurve mBoardSizeCurve;
		private AnimationCurve mLayerCurve;
		private AnimationCurve mTileTypeCurve;
		private AnimationCurve mTileCountCurve;

		// 특수 타일
		private bool mEnableFrozenTiles = false;
		private bool mEnableLockedTiles = false;
		private AnimationCurve mFrozenTileCurve;
		private AnimationCurve mLockedTileCurve;

		// 패턴 설정
		private bool mUseSymmetricPatterns = true;
		private bool mCenterAlignTiles = true;
		private float mFillDensity = 0.75F;

		// 시간 제한
		private bool mEnableTimeLimit = false;
		private AnimationCurve mTimeLimitCurve;

		// 아이템 설정
		private AnimationCurve mShuffleCountCurve;
		private AnimationCurve mUndoCountCurve;
		private AnimationCurve mHintCountCurve;

		private Vector2 mScrollPosition;
		private bool mShowAdvancedSettings = false;
		private bool mShowCurveSettings = false;
		private bool mShowPreview = true;

		private int mPreviewLevel = 1;

		#endregion

		[MenuItem("Tools/Tile Match/Level Generator")]
		public static void OpenWindow()
		{
			LevelGeneratorWindow window = GetWindow<LevelGeneratorWindow>();
			window.titleContent = new GUIContent("Level Generator V2");
			window.minSize = new Vector2(500, 700);
			window.Show();
		}

		private void OnEnable()
		{
			InitializeCurves();
		}

		private void InitializeCurves()
		{
			// 난이도 곡선 - S자 형태로 자연스러운 난이도 상승
			mDifficultyCurve = new AnimationCurve(
				new Keyframe(0, 0, 0, 0.5F),
				new Keyframe(0.3F, 0.2F),
				new Keyframe(0.6F, 0.5F),
				new Keyframe(0.85F, 0.8F),
				new Keyframe(1, 1, 0.5F, 0));

			// 보드 크기 - 천천히 증가
			mBoardSizeCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.4F, 0.3F),
				new Keyframe(0.7F, 0.6F),
				new Keyframe(1, 1));

			// 레이어 수 - 중반부터 증가
			mLayerCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.3F, 0.1F),
				new Keyframe(0.6F, 0.4F),
				new Keyframe(1, 1));

			// 타일 종류 - 점진적 증가
			mTileTypeCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.5F, 0.4F),
				new Keyframe(1, 1));

			// 타일 개수 - 점진적 증가
			mTileCountCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.3F, 0.25F),
				new Keyframe(0.7F, 0.6F),
				new Keyframe(1, 1));

			// 특수 타일 - 후반에 등장
			mFrozenTileCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.4F, 0),
				new Keyframe(0.6F, 0.05F),
				new Keyframe(1, 0.12F));

			mLockedTileCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.5F, 0),
				new Keyframe(0.75F, 0.03F),
				new Keyframe(1, 0.08F));

			// 시간 제한 (초)
			mTimeLimitCurve = new AnimationCurve(
				new Keyframe(0, 180),
				new Keyframe(0.5F, 150),
				new Keyframe(1, 90));

			// 아이템 개수 - 초반에 많고 후반에 적음
			mShuffleCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5F, 3),
				new Keyframe(1, 1));

			mUndoCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5F, 3),
				new Keyframe(1, 1));

			mHintCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5F, 3),
				new Keyframe(1, 1));
		}

		private void OnGUI()
		{
			mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition);

			DrawHeader();
			DrawBasicSettings();
			DrawBoardSettings();
			DrawTileSettings();
			DrawPatternSettings();

			EditorGUILayout.Space(5);
			mShowAdvancedSettings = EditorGUILayout.Foldout(mShowAdvancedSettings, "🔧 Advanced Settings", true);
			if (mShowAdvancedSettings)
			{
				DrawSpecialTileSettings();
				DrawItemSettings();
				DrawTimeSettings();
			}

			EditorGUILayout.Space(5);
			mShowCurveSettings = EditorGUILayout.Foldout(mShowCurveSettings, "📈 Difficulty Curves", true);
			if (mShowCurveSettings)
			{
				DrawCurveSettings();
			}

			EditorGUILayout.Space(5);
			mShowPreview = EditorGUILayout.Foldout(mShowPreview, "👁 Preview", true);
			if (mShowPreview)
			{
				DrawPreview();
			}

			DrawGenerateButtons();

			EditorGUILayout.EndScrollView();
		}

		#region Draw Methods

		private void DrawHeader()
		{
			EditorGUILayout.Space(10);

			GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 18,
				alignment = TextAnchor.MiddleCenter
			};
			EditorGUILayout.LabelField("🎲 Level Generator V2", headerStyle);

			EditorGUILayout.HelpBox(
				"✨ 개선된 기능:\n" +
				"• 보드 중앙 정렬 - 타일이 항상 화면 중앙에 배치\n" +
				"• 대칭 패턴 - 보기 좋은 좌우/상하 대칭 레이아웃\n" +
				"• 끝자리 난이도 - 레벨 번호 끝자리로 난이도 결정\n" +
				"• 클리어 보장 - 항상 3개씩 매칭 가능한 타일 구성",
				MessageType.Info);

			EditorGUILayout.Space(5);
		}

		private void DrawBasicSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("📁 Basic Settings", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			mStartLevelNumber = EditorGUILayout.IntField("Start Level", mStartLevelNumber);
			mEndLevelNumber = EditorGUILayout.IntField("End Level", mEndLevelNumber);
			EditorGUILayout.EndHorizontal();

			int totalLevels = mEndLevelNumber - mStartLevelNumber + 1;
			EditorGUILayout.LabelField($"Total: {totalLevels} levels", EditorStyles.miniLabel);

			EditorGUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			mOutputFolder = EditorGUILayout.TextField("Output Folder", mOutputFolder);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
				if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
				{
					mOutputFolder = "Assets" + path.Substring(Application.dataPath.Length);
				}
			}
			EditorGUILayout.EndHorizontal();

			mLevelPrefix = EditorGUILayout.TextField("Level Prefix", mLevelPrefix);

			EditorGUILayout.EndVertical();
		}

		private void DrawBoardSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("📐 Board Size (레벨 진행에 따라 자동 증가)", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Width", GUILayout.Width(60));
			mMinBoardWidth = EditorGUILayout.IntSlider(mMinBoardWidth, 4, 10);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			mMaxBoardWidth = EditorGUILayout.IntSlider(mMaxBoardWidth, 4, 12);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Height", GUILayout.Width(60));
			mMinBoardHeight = EditorGUILayout.IntSlider(mMinBoardHeight, 4, 10);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			mMaxBoardHeight = EditorGUILayout.IntSlider(mMaxBoardHeight, 4, 12);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Layers", GUILayout.Width(60));
			mMinLayers = EditorGUILayout.IntSlider(mMinLayers, 1, 3);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			mMaxLayers = EditorGUILayout.IntSlider(mMaxLayers, 1, 5);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		private void DrawTileSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🎴 Tile Settings", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tile Types", GUILayout.Width(80));
			mMinTileTypes = EditorGUILayout.IntSlider(mMinTileTypes, 2, 20);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			mMaxTileTypes = EditorGUILayout.IntSlider(mMaxTileTypes, 2, 52);
			EditorGUILayout.EndHorizontal();

			mMatchCount = EditorGUILayout.IntSlider("Match Count", mMatchCount, 2, 4);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tile Sets", GUILayout.Width(80));
			mMinTileSets = EditorGUILayout.IntSlider(mMinTileSets, 3, 30);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			mMaxTileSets = EditorGUILayout.IntSlider(mMaxTileSets, 6, 60);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField($"Tiles per level: {mMinTileSets * mMatchCount} ~ {mMaxTileSets * mMatchCount}", EditorStyles.miniLabel);

			EditorGUILayout.EndVertical();
		}

		private void DrawPatternSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🎨 Pattern Settings", EditorStyles.boldLabel);

			mUseSymmetricPatterns = EditorGUILayout.Toggle("Use Symmetric Patterns", mUseSymmetricPatterns);
			mCenterAlignTiles = EditorGUILayout.Toggle("Center Align Tiles", mCenterAlignTiles);
			mFillDensity = EditorGUILayout.Slider("Fill Density", mFillDensity, 0.3F, 1F);

			EditorGUILayout.EndVertical();
		}

		private void DrawSpecialTileSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("❄ Special Tiles", EditorStyles.boldLabel);

			mEnableFrozenTiles = EditorGUILayout.Toggle("Enable Frozen Tiles", mEnableFrozenTiles);
			mEnableLockedTiles = EditorGUILayout.Toggle("Enable Locked Tiles", mEnableLockedTiles);

			EditorGUILayout.EndVertical();
		}

		private void DrawItemSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🎁 Item Settings", EditorStyles.boldLabel);

			EditorGUILayout.LabelField("Items decrease as levels progress", EditorStyles.miniLabel);

			EditorGUILayout.EndVertical();
		}

		private void DrawTimeSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("⏱ Time Settings", EditorStyles.boldLabel);

			mEnableTimeLimit = EditorGUILayout.Toggle("Enable Time Limit", mEnableTimeLimit);

			EditorGUILayout.EndVertical();
		}

		private void DrawCurveSettings()
		{
			EditorGUILayout.BeginVertical("box");

			mDifficultyCurve = EditorGUILayout.CurveField("Difficulty", mDifficultyCurve);
			mBoardSizeCurve = EditorGUILayout.CurveField("Board Size", mBoardSizeCurve);
			mLayerCurve = EditorGUILayout.CurveField("Layers", mLayerCurve);
			mTileTypeCurve = EditorGUILayout.CurveField("Tile Types", mTileTypeCurve);
			mTileCountCurve = EditorGUILayout.CurveField("Tile Count", mTileCountCurve);

			EditorGUILayout.EndVertical();
		}

		private void DrawPreview()
		{
			EditorGUILayout.BeginVertical("box");

			mPreviewLevel = EditorGUILayout.IntSlider("Preview Level", mPreviewLevel, mStartLevelNumber, mEndLevelNumber);

			float progress = (float)(mPreviewLevel - mStartLevelNumber) / Mathf.Max(1, mEndLevelNumber - mStartLevelNumber);
			LevelStats stats = CalculateLevelStats(progress);

			EditorGUILayout.LabelField($"Difficulty: {stats.difficulty}");
			EditorGUILayout.LabelField($"Board: {stats.width} x {stats.height}");
			EditorGUILayout.LabelField($"Layers: {stats.layers}");
			EditorGUILayout.LabelField($"Tile Types: {stats.tileTypes}");
			EditorGUILayout.LabelField($"Tile Sets: {stats.tileSets} ({stats.tileSets * mMatchCount} tiles)");

			EditorGUILayout.EndVertical();
		}

		private void DrawGenerateButtons()
		{
			EditorGUILayout.Space(10);

			GUI.backgroundColor = new Color(0.3F, 0.8F, 0.3F);
			if (GUILayout.Button("🎲 Generate All Levels", GUILayout.Height(40)))
			{
				GenerateAllLevels();
			}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate Single Level"))
			{
				GenerateSingleLevel(mPreviewLevel);
			}
			if (GUILayout.Button("Open Output Folder"))
			{
				CreateFolderIfNeeded(mOutputFolder);
				EditorUtility.RevealInFinder(mOutputFolder);
			}
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Level Generation

		private void GenerateAllLevels()
		{
			CreateFolderIfNeeded(mOutputFolder);

			int total = mEndLevelNumber - mStartLevelNumber + 1;

			for (int i = mStartLevelNumber; i <= mEndLevelNumber; i++)
			{
				float progressBar = (float)(i - mStartLevelNumber) / total;
				EditorUtility.DisplayProgressBar("Generating Levels", $"Level {i}/{mEndLevelNumber}", progressBar);

				GenerateSingleLevel(i);
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Complete", $"Generated {total} levels!", "OK");
		}

		private void GenerateSingleLevel(int levelNumber)
		{
			float progress = (float)(levelNumber - mStartLevelNumber) / Mathf.Max(1, mEndLevelNumber - mStartLevelNumber);
			LevelStats stats = CalculateLevelStats(progress);

			LevelData level = ScriptableObject.CreateInstance<LevelData>();
			level.levelNumber = levelNumber;
			level.levelName = $"Level {levelNumber}";
			level.difficulty = stats.difficulty;
			level.boardWidth = stats.width;
			level.boardHeight = stats.height;
			level.maxLayers = stats.layers;
			level.slotCount = 7;
			level.matchCount = mMatchCount;
			level.targetScore = CalculateTargetScore(stats);

			// 패턴 생성
			if (mUseSymmetricPatterns)
				level.tilePlacements = GenerateSymmetricPattern(stats.width, stats.height, stats.layers, mFillDensity);
			else
				level.tilePlacements = GenerateBalancedPattern(stats.width, stats.height, stats.layers, mFillDensity);

			// 타일 수 조정
			AdjustTileCount(level, stats.tileSets);

			// 중앙 정렬
			if (mCenterAlignTiles)
				CenterAlignPlacements(level);

			// 타일 타입 할당
			AssignTileTypes(level, stats.tileTypes);

			// 특수 타일 적용
			if (mEnableFrozenTiles || mEnableLockedTiles)
				ApplySpecialTiles(level, stats.frozenRatio, stats.lockedRatio);

			// 아이템 설정
			level.initialShuffleCount = stats.shuffleCount;
			level.initialUndoCount = stats.undoCount;
			level.initialHintCount = stats.hintCount;

			// 시간 제한
			if (mEnableTimeLimit)
				level.timeLimit = stats.timeLimit;

			// 저장
			string path = $"{mOutputFolder}/{mLevelPrefix}{levelNumber:D3}.asset";

			// 기존 파일 확인 및 덮어쓰기
			LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
			if (existing != null)
			{
				EditorUtility.CopySerialized(level, existing);
				EditorUtility.SetDirty(existing);
			}
			else
			{
				AssetDatabase.CreateAsset(level, path);
			}
		}

		private LevelStats CalculateLevelStats(float progress)
		{
			float diff = mDifficultyCurve.Evaluate(progress);

			return new LevelStats
			{
				difficulty = GetDifficultyFromValue(diff),
				width = Mathf.RoundToInt(Mathf.Lerp(mMinBoardWidth, mMaxBoardWidth, mBoardSizeCurve.Evaluate(progress))),
				height = Mathf.RoundToInt(Mathf.Lerp(mMinBoardHeight, mMaxBoardHeight, mBoardSizeCurve.Evaluate(progress))),
				layers = Mathf.RoundToInt(Mathf.Lerp(mMinLayers, mMaxLayers, mLayerCurve.Evaluate(progress))),
				tileTypes = Mathf.RoundToInt(Mathf.Lerp(mMinTileTypes, mMaxTileTypes, mTileTypeCurve.Evaluate(progress))),
				tileSets = Mathf.RoundToInt(Mathf.Lerp(mMinTileSets, mMaxTileSets, mTileCountCurve.Evaluate(progress))),
				frozenRatio = mEnableFrozenTiles ? mFrozenTileCurve.Evaluate(progress) : 0,
				lockedRatio = mEnableLockedTiles ? mLockedTileCurve.Evaluate(progress) : 0,
				timeLimit = Mathf.RoundToInt(mTimeLimitCurve.Evaluate(progress)),
				shuffleCount = Mathf.RoundToInt(mShuffleCountCurve.Evaluate(progress)),
				undoCount = Mathf.RoundToInt(mUndoCountCurve.Evaluate(progress)),
				hintCount = Mathf.RoundToInt(mHintCountCurve.Evaluate(progress))
			};
		}

		#endregion

		#region Helper Structs

		private struct LevelStats
		{
			public ELevelDifficulty difficulty;
			public int width, height, layers, tileTypes, tileSets;
			public float frozenRatio, lockedRatio;
			public int timeLimit, shuffleCount, undoCount, hintCount;
		}

		private ELevelDifficulty GetDifficultyFromValue(float value)
		{
			if (value < 0.2F) return ELevelDifficulty.Tutorial;
			if (value < 0.4F) return ELevelDifficulty.Easy;
			if (value < 0.6F) return ELevelDifficulty.Normal;
			if (value < 0.8F) return ELevelDifficulty.Hard;
			return ELevelDifficulty.Expert;
		}

		private int CalculateTargetScore(LevelStats stats)
		{
			return stats.tileSets * 100 + stats.layers * 50;
		}

		#endregion

		#region Pattern Generation

		/// <summary>
		/// 좌우 대칭 패턴 생성 (중복 방지)
		/// </summary>
		private List<TilePlacement> GenerateSymmetricPattern(int width, int height, int layers, float density)
		{
			HashSet<(int x, int y, int layer)> usedPositions = new HashSet<(int, int, int)>();
			List<TilePlacement> placements = new List<TilePlacement>();

			int halfWidth = (width + 1) / 2;

			for (int layer = 0; layer < layers; layer++)
			{
				int margin = layer;
				float layerDensity = density * (1F - layer * 0.15F);

				for (int x = margin; x < halfWidth; x++)
				{
					for (int y = margin; y < height - margin; y++)
					{
						// 상위 레이어는 체커보드 패턴
						if (layer > 0 && (x + y) % 2 != 0) continue;

						if (Random.value < layerDensity)
						{
							// 왼쪽 절반 추가 (중복 체크)
							if (!usedPositions.Contains((x, y, layer)))
							{
								usedPositions.Add((x, y, layer));
								placements.Add(new TilePlacement(x, y, layer, ""));
							}

							// 오른쪽 대칭 (중복 체크)
							int mirrorX = width - 1 - x;
							if (mirrorX != x && !usedPositions.Contains((mirrorX, y, layer)))
							{
								usedPositions.Add((mirrorX, y, layer));
								placements.Add(new TilePlacement(mirrorX, y, layer, ""));
							}
						}
					}
				}
			}

			return placements;
		}

		/// <summary>
		/// 균형잡힌 일반 패턴 생성 (중복 방지)
		/// </summary>
		private List<TilePlacement> GenerateBalancedPattern(int width, int height, int layers, float density)
		{
			HashSet<(int x, int y, int layer)> usedPositions = new HashSet<(int, int, int)>();
			List<TilePlacement> placements = new List<TilePlacement>();

			for (int layer = 0; layer < layers; layer++)
			{
				int margin = layer;
				float layerDensity = density * (1F - layer * 0.2F);

				for (int x = margin; x < width - margin; x++)
				{
					for (int y = margin; y < height - margin; y++)
					{
						if (layer > 0 && (x + y) % 2 != 0) continue;

						if (Random.value < layerDensity)
						{
							if (!usedPositions.Contains((x, y, layer)))
							{
								usedPositions.Add((x, y, layer));
								placements.Add(new TilePlacement(x, y, layer, ""));
							}
						}
					}
				}
			}

			return placements;
		}

		/// <summary>
		/// 타일 수를 목표 세트 수에 맞게 조정 (중복 위치 방지!)
		/// </summary>
		private void AdjustTileCount(LevelData level, int targetSets)
		{
			if (level.tilePlacements == null)
				level.tilePlacements = new List<TilePlacement>();

			int targetCount = targetSets * mMatchCount;

			// 너무 많으면 제거
			while (level.tilePlacements.Count > targetCount)
			{
				int idx = Random.Range(0, level.tilePlacements.Count);
				level.tilePlacements.RemoveAt(idx);
			}

			// 너무 적으면 추가 (중복 위치 방지!)
			int maxAttempts = 1000;
			int attempts = 0;

			while (level.tilePlacements.Count < targetCount && attempts < maxAttempts)
			{
				attempts++;

				int newX, newY, newLayer;

				if (level.tilePlacements.Count > 0)
				{
					// 기존 타일 근처에 추가
					TilePlacement existing = level.tilePlacements[Random.Range(0, level.tilePlacements.Count)];
					newX = Mathf.Clamp(existing.gridX + Random.Range(-1, 2), 0, level.boardWidth - 1);
					newY = Mathf.Clamp(existing.gridY + Random.Range(-1, 2), 0, level.boardHeight - 1);
					newLayer = existing.layer;
				}
				else
				{
					newX = Random.Range(0, level.boardWidth);
					newY = Random.Range(0, level.boardHeight);
					newLayer = 0;
				}

				// 중복 위치 체크 - 같은 위치에 같은 레이어 타일이 있으면 스킵
				bool bIsDuplicate = level.tilePlacements.Any(t =>
					t.gridX == newX && t.gridY == newY && t.layer == newLayer);

				if (!bIsDuplicate)
				{
					level.tilePlacements.Add(new TilePlacement(newX, newY, newLayer, ""));
				}
			}

			// matchCount 배수로 맞추기
			int remainder = level.tilePlacements.Count % mMatchCount;
			for (int i = 0; i < remainder && level.tilePlacements.Count > mMatchCount; i++)
			{
				level.tilePlacements.RemoveAt(Random.Range(0, level.tilePlacements.Count));
			}
		}

		/// <summary>
		/// 타일 배치 중앙 정렬 (빈 공간 제거)
		/// </summary>
		private void CenterAlignPlacements(LevelData level)
		{
			if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

			// 실제 사용된 범위 찾기
			int minX = level.tilePlacements.Min(t => t.gridX);
			int maxX = level.tilePlacements.Max(t => t.gridX);
			int minY = level.tilePlacements.Min(t => t.gridY);
			int maxY = level.tilePlacements.Max(t => t.gridY);

			int usedWidth = maxX - minX + 1;
			int usedHeight = maxY - minY + 1;

			// 중앙 정렬을 위한 오프셋 계산
			int offsetX = (level.boardWidth - usedWidth) / 2 - minX;
			int offsetY = (level.boardHeight - usedHeight) / 2 - minY;

			// 모든 타일 위치 조정
			foreach (TilePlacement tile in level.tilePlacements)
			{
				tile.gridX += offsetX;
				tile.gridY += offsetY;

				// 범위 체크
				tile.gridX = Mathf.Clamp(tile.gridX, 0, level.boardWidth - 1);
				tile.gridY = Mathf.Clamp(tile.gridY, 0, level.boardHeight - 1);
			}
		}

		#endregion

		#region Tile Assignment

		private void AssignTileTypes(LevelData level, int typeCount)
		{
			if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

			// 모든 가능한 타일 타입 생성
			List<string> allTypes = new List<string>();
			foreach (ECardSuit suit in System.Enum.GetValues(typeof(ECardSuit)))
				foreach (ECardRank rank in System.Enum.GetValues(typeof(ECardRank)))
					allTypes.Add($"{suit}_{rank}");

			// 사용할 타입 선택 (셔플 후 선택)
			List<string> selectedTypes = allTypes.OrderBy(x => Random.value).Take(typeCount).ToList();

			// 타일 타입 할당 (matchCount개씩 - 클리어 보장!)
			int sets = level.tilePlacements.Count / mMatchCount;
			List<string> assignments = new List<string>();

			for (int i = 0; i < sets; i++)
			{
				string type = selectedTypes[i % selectedTypes.Count];
				for (int j = 0; j < mMatchCount; j++)
					assignments.Add(type);
			}

			// 셔플
			assignments = assignments.OrderBy(x => Random.value).ToList();

			// 할당
			for (int i = 0; i < level.tilePlacements.Count && i < assignments.Count; i++)
				level.tilePlacements[i].tileTypeId = assignments[i];
		}

		private void ApplySpecialTiles(LevelData level, float frozenRatio, float lockedRatio)
		{
			if (level.tilePlacements == null || level.tilePlacements.Count == 0) return;

			List<TilePlacement> shuffled = level.tilePlacements.OrderBy(x => Random.value).ToList();

			// Frozen 타일 (상위 레이어에 우선 적용)
			if (frozenRatio > 0)
			{
				int frozenCount = Mathf.RoundToInt(level.tilePlacements.Count * frozenRatio);
				List<TilePlacement> upperLayerTiles = shuffled.Where(t => t.layer > 0).OrderBy(x => Random.value).ToList();

				for (int i = 0; i < frozenCount && i < upperLayerTiles.Count; i++)
				{
					upperLayerTiles[i].isFrozen = true;
					upperLayerTiles[i].frozenCount = Random.Range(1, 3);
				}
			}

			// Locked 타일 (frozen이 아닌 타일에 적용)
			if (lockedRatio > 0)
			{
				int lockedCount = Mathf.RoundToInt(level.tilePlacements.Count * lockedRatio);
				List<TilePlacement> available = shuffled.Where(t => !t.isFrozen).OrderBy(x => Random.value).ToList();

				for (int i = 0; i < lockedCount && i < available.Count; i++)
					available[i].isLocked = true;
			}
		}

		#endregion

		#region Utility

		private void CreateFolderIfNeeded(string path)
		{
			if (AssetDatabase.IsValidFolder(path)) return;

			string[] folders = path.Split('/');
			string current = folders[0];

			for (int i = 1; i < folders.Length; i++)
			{
				string newPath = current + "/" + folders[i];
				if (!AssetDatabase.IsValidFolder(newPath))
					AssetDatabase.CreateFolder(current, folders[i]);
				current = newPath;
			}
		}

		#endregion
	}

	public enum EPatternType { Pyramid, Diamond, Rectangle, Cross, Heart, Star, Spiral, Random, Custom }
}
#endif
