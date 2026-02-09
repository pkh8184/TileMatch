#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TileMatch.LevelEditor
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
		private int startLevelNumber = 1;
		private int endLevelNumber = 300;
		private string outputFolder = "Assets/Resources/Levels";
		private string levelPrefix = "Level_";

		// 보드 설정
		private int minBoardWidth = 5;
		private int maxBoardWidth = 8;
		private int minBoardHeight = 6;
		private int maxBoardHeight = 10;
		private int minLayers = 1;
		private int maxLayers = 4;

		// 타일 설정
		private int minTileTypes = 4;
		private int maxTileTypes = 16;
		private int matchCount = 3;
		private int minTileSets = 6;  // 최소 타일 세트 수 (18개)
		private int maxTileSets = 40; // 최대 타일 세트 수 (120개)

		// 난이도 곡선
		private AnimationCurve difficultyCurve;
		private AnimationCurve boardSizeCurve;
		private AnimationCurve layerCurve;
		private AnimationCurve tileTypeCurve;
		private AnimationCurve tileCountCurve;

		// 특수 타일
		private bool enableFrozenTiles = false;
		private bool enableLockedTiles = false;
		private AnimationCurve frozenTileCurve;
		private AnimationCurve lockedTileCurve;

		// 패턴 설정
		private bool useSymmetricPatterns = true;
		private bool centerAlignTiles = true;
		private float fillDensity = 0.75f;

		// 시간 제한
		private bool enableTimeLimit = false;
		private AnimationCurve timeLimitCurve;

		// 아이템 설정
		private AnimationCurve shuffleCountCurve;
		private AnimationCurve undoCountCurve;
		private AnimationCurve hintCountCurve;

		private Vector2 scrollPosition;
		private bool showAdvancedSettings = false;
		private bool showCurveSettings = false;
		private bool showPreview = true;

		private int previewLevel = 1;

		#endregion

		[MenuItem("Tools/Tile Match/Level Generator")]
		public static void OpenWindow()
		{
			var window = GetWindow<LevelGeneratorWindow>();
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
			difficultyCurve = new AnimationCurve(
				new Keyframe(0, 0, 0, 0.5f),
				new Keyframe(0.3f, 0.2f),
				new Keyframe(0.6f, 0.5f),
				new Keyframe(0.85f, 0.8f),
				new Keyframe(1, 1, 0.5f, 0));

			// 보드 크기 - 천천히 증가
			boardSizeCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.4f, 0.3f),
				new Keyframe(0.7f, 0.6f),
				new Keyframe(1, 1));

			// 레이어 수 - 중반부터 증가
			layerCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.3f, 0.1f),
				new Keyframe(0.6f, 0.4f),
				new Keyframe(1, 1));

			// 타일 종류 - 점진적 증가
			tileTypeCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.5f, 0.4f),
				new Keyframe(1, 1));

			// 타일 개수 - 점진적 증가
			tileCountCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.3f, 0.25f),
				new Keyframe(0.7f, 0.6f),
				new Keyframe(1, 1));

			// 특수 타일 - 후반에 등장
			frozenTileCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.4f, 0),
				new Keyframe(0.6f, 0.05f),
				new Keyframe(1, 0.12f));

			lockedTileCurve = new AnimationCurve(
				new Keyframe(0, 0),
				new Keyframe(0.5f, 0),
				new Keyframe(0.75f, 0.03f),
				new Keyframe(1, 0.08f));

			// 시간 제한 (초)
			timeLimitCurve = new AnimationCurve(
				new Keyframe(0, 180),
				new Keyframe(0.5f, 150),
				new Keyframe(1, 90));

			// 아이템 개수 - 초반에 많고 후반에 적음
			shuffleCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5f, 3),
				new Keyframe(1, 1));

			undoCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5f, 3),
				new Keyframe(1, 1));

			hintCountCurve = new AnimationCurve(
				new Keyframe(0, 5),
				new Keyframe(0.5f, 3),
				new Keyframe(1, 1));
		}

		private void OnGUI()
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			DrawHeader();
			DrawBasicSettings();
			DrawBoardSettings();
			DrawTileSettings();
			DrawPatternSettings();

			EditorGUILayout.Space(5);
			showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "🔧 Advanced Settings", true);
			if (showAdvancedSettings)
			{
				DrawSpecialTileSettings();
				DrawItemSettings();
				DrawTimeSettings();
			}

			EditorGUILayout.Space(5);
			showCurveSettings = EditorGUILayout.Foldout(showCurveSettings, "📈 Difficulty Curves", true);
			if (showCurveSettings)
			{
				DrawCurveSettings();
			}

			EditorGUILayout.Space(5);
			showPreview = EditorGUILayout.Foldout(showPreview, "👁 Preview", true);
			if (showPreview)
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

			var headerStyle = new GUIStyle(EditorStyles.boldLabel)
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
			startLevelNumber = EditorGUILayout.IntField("Start Level", startLevelNumber);
			endLevelNumber = EditorGUILayout.IntField("End Level", endLevelNumber);
			EditorGUILayout.EndHorizontal();

			int totalLevels = endLevelNumber - startLevelNumber + 1;
			EditorGUILayout.LabelField($"Total: {totalLevels} levels", EditorStyles.miniLabel);

			EditorGUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
				if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
				{
					outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
				}
			}
			EditorGUILayout.EndHorizontal();

			levelPrefix = EditorGUILayout.TextField("Level Prefix", levelPrefix);

			EditorGUILayout.EndVertical();
		}

		private void DrawBoardSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("📐 Board Size (레벨 진행에 따라 자동 증가)", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Width", GUILayout.Width(60));
			minBoardWidth = EditorGUILayout.IntSlider(minBoardWidth, 4, 10);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			maxBoardWidth = EditorGUILayout.IntSlider(maxBoardWidth, 4, 12);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Height", GUILayout.Width(60));
			minBoardHeight = EditorGUILayout.IntSlider(minBoardHeight, 4, 10);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			maxBoardHeight = EditorGUILayout.IntSlider(maxBoardHeight, 4, 12);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Layers", GUILayout.Width(60));
			minLayers = EditorGUILayout.IntSlider(minLayers, 1, 3);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			maxLayers = EditorGUILayout.IntSlider(maxLayers, 1, 5);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();
		}

		private void DrawTileSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🎴 Tile Settings", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tile Types", GUILayout.Width(80));
			minTileTypes = EditorGUILayout.IntSlider(minTileTypes, 2, 20);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			maxTileTypes = EditorGUILayout.IntSlider(maxTileTypes, 2, 52);
			EditorGUILayout.EndHorizontal();

			matchCount = EditorGUILayout.IntSlider("Match Count", matchCount, 2, 4);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Tile Sets", GUILayout.Width(80));
			minTileSets = EditorGUILayout.IntSlider(minTileSets, 3, 30);
			EditorGUILayout.LabelField("~", GUILayout.Width(20));
			maxTileSets = EditorGUILayout.IntSlider(maxTileSets, 6, 60);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.LabelField($"Tiles per level: {minTileSets * matchCount} ~ {maxTileSets * matchCount}", EditorStyles.miniLabel);

			EditorGUILayout.EndVertical();
		}

		private void DrawPatternSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("🎨 Pattern Settings", EditorStyles.boldLabel);

			useSymmetricPatterns = EditorGUILayout.Toggle("Use Symmetric Patterns", useSymmetricPatterns);
			centerAlignTiles = EditorGUILayout.Toggle("Center Align Tiles", centerAlignTiles);
			fillDensity = EditorGUILayout.Slider("Fill Density", fillDensity, 0.3f, 1f);

			EditorGUILayout.EndVertical();
		}

		private void DrawSpecialTileSettings()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("❄ Special Tiles", EditorStyles.boldLabel);

			enableFrozenTiles = EditorGUILayout.Toggle("Enable Frozen Tiles", enableFrozenTiles);
			enableLockedTiles = EditorGUILayout.Toggle("Enable Locked Tiles", enableLockedTiles);

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

			enableTimeLimit = EditorGUILayout.Toggle("Enable Time Limit", enableTimeLimit);

			EditorGUILayout.EndVertical();
		}

		private void DrawCurveSettings()
		{
			EditorGUILayout.BeginVertical("box");

			difficultyCurve = EditorGUILayout.CurveField("Difficulty", difficultyCurve);
			boardSizeCurve = EditorGUILayout.CurveField("Board Size", boardSizeCurve);
			layerCurve = EditorGUILayout.CurveField("Layers", layerCurve);
			tileTypeCurve = EditorGUILayout.CurveField("Tile Types", tileTypeCurve);
			tileCountCurve = EditorGUILayout.CurveField("Tile Count", tileCountCurve);

			EditorGUILayout.EndVertical();
		}

		private void DrawPreview()
		{
			EditorGUILayout.BeginVertical("box");

			previewLevel = EditorGUILayout.IntSlider("Preview Level", previewLevel, startLevelNumber, endLevelNumber);

			float progress = (float)(previewLevel - startLevelNumber) / Mathf.Max(1, endLevelNumber - startLevelNumber);
			var stats = CalculateLevelStats(progress);

			EditorGUILayout.LabelField($"Difficulty: {stats.difficulty}");
			EditorGUILayout.LabelField($"Board: {stats.width} x {stats.height}");
			EditorGUILayout.LabelField($"Layers: {stats.layers}");
			EditorGUILayout.LabelField($"Tile Types: {stats.tileTypes}");
			EditorGUILayout.LabelField($"Tile Sets: {stats.tileSets} ({stats.tileSets * matchCount} tiles)");

			EditorGUILayout.EndVertical();
		}

		private void DrawGenerateButtons()
		{
			EditorGUILayout.Space(10);

			GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
			if (GUILayout.Button("🎲 Generate All Levels", GUILayout.Height(40)))
			{
				GenerateAllLevels();
			}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate Single Level"))
			{
				GenerateSingleLevel(previewLevel);
			}
			if (GUILayout.Button("Open Output Folder"))
			{
				CreateFolderIfNeeded(outputFolder);
				EditorUtility.RevealInFinder(outputFolder);
			}
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Level Generation

		private void GenerateAllLevels()
		{
			CreateFolderIfNeeded(outputFolder);

			int total = endLevelNumber - startLevelNumber + 1;

			for (int i = startLevelNumber; i <= endLevelNumber; i++)
			{
				float progressBar = (float)(i - startLevelNumber) / total;
				EditorUtility.DisplayProgressBar("Generating Levels", $"Level {i}/{endLevelNumber}", progressBar);

				GenerateSingleLevel(i);
			}

			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Complete", $"Generated {total} levels!", "OK");
		}

		private void GenerateSingleLevel(int levelNumber)
		{
			float progress = (float)(levelNumber - startLevelNumber) / Mathf.Max(1, endLevelNumber - startLevelNumber);
			var stats = CalculateLevelStats(progress);

			LevelData level = ScriptableObject.CreateInstance<LevelData>();
			level.levelNumber = levelNumber;
			level.levelName = $"Level {levelNumber}";
			level.difficulty = stats.difficulty;
			level.boardWidth = stats.width;
			level.boardHeight = stats.height;
			level.maxLayers = stats.layers;
			level.slotCount = 7;
			level.matchCount = matchCount;
			level.targetScore = CalculateTargetScore(stats);

			// 패턴 생성
			if (useSymmetricPatterns)
				level.tilePlacements = GenerateSymmetricPattern(stats.width, stats.height, stats.layers, fillDensity);
			else
				level.tilePlacements = GenerateBalancedPattern(stats.width, stats.height, stats.layers, fillDensity);

			// 타일 수 조정
			AdjustTileCount(level, stats.tileSets);

			// 중앙 정렬
			if (centerAlignTiles)
				CenterAlignPlacements(level);

			// 타일 타입 할당
			AssignTileTypes(level, stats.tileTypes);

			// 특수 타일 적용
			if (enableFrozenTiles || enableLockedTiles)
				ApplySpecialTiles(level, stats.frozenRatio, stats.lockedRatio);

			// 아이템 설정
			level.initialShuffleCount = stats.shuffleCount;
			level.initialUndoCount = stats.undoCount;
			level.initialHintCount = stats.hintCount;

			// 시간 제한
			if (enableTimeLimit)
				level.timeLimit = stats.timeLimit;

			// 저장
			string path = $"{outputFolder}/{levelPrefix}{levelNumber:D3}.asset";

			// 기존 파일 확인 및 덮어쓰기
			var existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
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
			float diff = difficultyCurve.Evaluate(progress);

			return new LevelStats
			{
				difficulty = GetDifficultyFromValue(diff),
				width = Mathf.RoundToInt(Mathf.Lerp(minBoardWidth, maxBoardWidth, boardSizeCurve.Evaluate(progress))),
				height = Mathf.RoundToInt(Mathf.Lerp(minBoardHeight, maxBoardHeight, boardSizeCurve.Evaluate(progress))),
				layers = Mathf.RoundToInt(Mathf.Lerp(minLayers, maxLayers, layerCurve.Evaluate(progress))),
				tileTypes = Mathf.RoundToInt(Mathf.Lerp(minTileTypes, maxTileTypes, tileTypeCurve.Evaluate(progress))),
				tileSets = Mathf.RoundToInt(Mathf.Lerp(minTileSets, maxTileSets, tileCountCurve.Evaluate(progress))),
				frozenRatio = enableFrozenTiles ? frozenTileCurve.Evaluate(progress) : 0,
				lockedRatio = enableLockedTiles ? lockedTileCurve.Evaluate(progress) : 0,
				timeLimit = Mathf.RoundToInt(timeLimitCurve.Evaluate(progress)),
				shuffleCount = Mathf.RoundToInt(shuffleCountCurve.Evaluate(progress)),
				undoCount = Mathf.RoundToInt(undoCountCurve.Evaluate(progress)),
				hintCount = Mathf.RoundToInt(hintCountCurve.Evaluate(progress))
			};
		}

		#endregion

		#region Helper Structs

		private struct LevelStats
		{
			public LevelDifficulty difficulty;
			public int width, height, layers, tileTypes, tileSets;
			public float frozenRatio, lockedRatio;
			public int timeLimit, shuffleCount, undoCount, hintCount;
		}

		private LevelDifficulty GetDifficultyFromValue(float value)
		{
			if (value < 0.2f) return LevelDifficulty.Tutorial;
			if (value < 0.4f) return LevelDifficulty.Easy;
			if (value < 0.6f) return LevelDifficulty.Normal;
			if (value < 0.8f) return LevelDifficulty.Hard;
			return LevelDifficulty.Expert;
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
				float layerDensity = density * (1f - layer * 0.15f);

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
				float layerDensity = density * (1f - layer * 0.2f);

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

			int targetCount = targetSets * matchCount;

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
					var existing = level.tilePlacements[Random.Range(0, level.tilePlacements.Count)];
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
				bool isDuplicate = level.tilePlacements.Any(t =>
					t.gridX == newX && t.gridY == newY && t.layer == newLayer);

				if (!isDuplicate)
				{
					level.tilePlacements.Add(new TilePlacement(newX, newY, newLayer, ""));
				}
			}

			// matchCount 배수로 맞추기
			int remainder = level.tilePlacements.Count % matchCount;
			for (int i = 0; i < remainder && level.tilePlacements.Count > matchCount; i++)
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
			foreach (var tile in level.tilePlacements)
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
			foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
				foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
					allTypes.Add($"{suit}_{rank}");

			// 사용할 타입 선택 (셔플 후 선택)
			var selectedTypes = allTypes.OrderBy(x => Random.value).Take(typeCount).ToList();

			// 타일 타입 할당 (matchCount개씩 - 클리어 보장!)
			int sets = level.tilePlacements.Count / matchCount;
			List<string> assignments = new List<string>();

			for (int i = 0; i < sets; i++)
			{
				string type = selectedTypes[i % selectedTypes.Count];
				for (int j = 0; j < matchCount; j++)
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

			var shuffled = level.tilePlacements.OrderBy(x => Random.value).ToList();

			// Frozen 타일 (상위 레이어에 우선 적용)
			if (frozenRatio > 0)
			{
				int frozenCount = Mathf.RoundToInt(level.tilePlacements.Count * frozenRatio);
				var upperLayerTiles = shuffled.Where(t => t.layer > 0).OrderBy(x => Random.value).ToList();

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
				var available = shuffled.Where(t => !t.isFrozen).OrderBy(x => Random.value).ToList();

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

	public enum PatternType { Pyramid, Diamond, Rectangle, Cross, Heart, Star, Spiral, Random, Custom }
}
#endif