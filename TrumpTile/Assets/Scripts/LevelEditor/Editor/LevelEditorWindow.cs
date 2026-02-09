#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TileMatch.LevelEditor
{
	public enum EditorTool { Select, Paint, Erase, Fill, Eyedropper }

	public class LevelSnapshot
	{
		public string actionName;
		public List<TilePlacement> tilePlacements = new List<TilePlacement>();
	}

	public class LevelEditorWindow : EditorWindow
	{
		// 현재 편집 중인 레벨
		private LevelData currentLevel;

		// 에디터 상태
		private EditorTool currentTool = EditorTool.Select;
		private int currentLayer = 0;
		private string selectedTileType = "";
		private bool showAllLayers = true;
		private float layerOpacity = 0.5f;

		// 뷰 설정
		private Vector2 scrollPosition;
		private float zoomLevel = 1f;
		private Vector2 panOffset = Vector2.zero;

		// 그리드 설정
		private float cellSize = 40f;
		private float cellPadding = 2f;

		// 선택 상태
		private List<TilePlacement> selectedTiles;
		private Vector2 dragStart;
		private Rect selectionRect;

		// UI 패널 상태
		private bool showLevelSettings = true;
		private bool showTileTypes = true;
		private bool showStatistics = true;

		// 히스토리
		private List<LevelSnapshot> undoHistory;
		private List<LevelSnapshot> redoHistory;
		private const int MaxHistorySize = 50;

		// 타일 프리셋
		private List<TileTypeConfig> tileTypePresets;

		// 색상
		private static readonly Color[] LayerColors = new Color[]
		{
			new Color(0.2f, 0.6f, 1f, 1f),
			new Color(0.2f, 0.8f, 0.4f, 1f),
			new Color(1f, 0.8f, 0.2f, 1f),
			new Color(1f, 0.4f, 0.4f, 1f),
			new Color(0.8f, 0.4f, 1f, 1f),
		};

		[MenuItem("Tools/Tile Match/Level Editor %#l")]
		public static void OpenWindow()
		{
			var window = GetWindow<LevelEditorWindow>();
			window.titleContent = new GUIContent("Level Editor");
			window.minSize = new Vector2(900, 600);
			window.Show();
		}

		private void OnEnable()
		{
			selectedTiles = new List<TilePlacement>();
			undoHistory = new List<LevelSnapshot>();
			redoHistory = new List<LevelSnapshot>();
			InitializeTilePresets();
		}

		private void InitializeTilePresets()
		{
			tileTypePresets = new List<TileTypeConfig>();

			foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
			{
				foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
				{
					tileTypePresets.Add(new TileTypeConfig
					{
						typeId = $"{suit}_{rank}",
						suit = suit,
						rank = rank,
						weight = 1
					});
				}
			}
		}

		private void OnGUI()
		{
			// 리스트 null 체크
			if (selectedTiles == null) selectedTiles = new List<TilePlacement>();
			if (undoHistory == null) undoHistory = new List<LevelSnapshot>();
			if (redoHistory == null) redoHistory = new List<LevelSnapshot>();
			if (tileTypePresets == null) InitializeTilePresets();

			EditorGUILayout.BeginVertical();

			DrawToolbar();

			EditorGUILayout.BeginHorizontal();

			DrawLeftPanel();
			DrawGridEditor();
			DrawRightPanel();

			EditorGUILayout.EndHorizontal();

			DrawStatusBar();

			EditorGUILayout.EndVertical();

			HandleKeyboardShortcuts();
		}

		#region Toolbar

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
				CreateNewLevel();

			if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50)))
				OpenLevel();

			if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
				SaveCurrentLevel();

			GUILayout.Space(20);

			// Undo/Redo
			GUI.enabled = undoHistory != null && undoHistory.Count > 0;
			if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(50)))
				Undo();

			GUI.enabled = redoHistory != null && redoHistory.Count > 0;
			if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(50)))
				Redo();

			GUI.enabled = true;

			GUILayout.Space(20);

			// 도구 선택
			GUILayout.Label("Tool:", GUILayout.Width(35));

			if (GUILayout.Toggle(currentTool == EditorTool.Select, "Select", EditorStyles.toolbarButton, GUILayout.Width(50)))
				currentTool = EditorTool.Select;
			if (GUILayout.Toggle(currentTool == EditorTool.Paint, "Paint", EditorStyles.toolbarButton, GUILayout.Width(50)))
				currentTool = EditorTool.Paint;
			if (GUILayout.Toggle(currentTool == EditorTool.Erase, "Erase", EditorStyles.toolbarButton, GUILayout.Width(50)))
				currentTool = EditorTool.Erase;

			GUILayout.Space(20);

			// 레이어 선택
			GUILayout.Label("Layer:", GUILayout.Width(40));
			int maxLayers = currentLevel != null ? Mathf.Min(currentLevel.maxLayers, 5) : 4;
			for (int i = 0; i < maxLayers; i++)
			{
				GUI.backgroundColor = currentLayer == i ? LayerColors[i] : Color.white;
				if (GUILayout.Button(i.ToString(), EditorStyles.toolbarButton, GUILayout.Width(25)))
					currentLayer = i;
			}
			GUI.backgroundColor = Color.white;

			GUILayout.Space(10);
			showAllLayers = GUILayout.Toggle(showAllLayers, "Show All", EditorStyles.toolbarButton, GUILayout.Width(70));

			GUILayout.FlexibleSpace();

			// 줌
			GUILayout.Label("Zoom:", GUILayout.Width(40));
			zoomLevel = GUILayout.HorizontalSlider(zoomLevel, 0.5f, 2f, GUILayout.Width(80));
			GUILayout.Label($"{zoomLevel:F1}x", GUILayout.Width(35));

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Left Panel

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(250));
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			DrawLevelSettings();
			DrawStatistics();

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private void DrawLevelSettings()
		{
			showLevelSettings = EditorGUILayout.Foldout(showLevelSettings, "Level Settings", true);
			if (!showLevelSettings) return;

			EditorGUILayout.BeginVertical("box");

			if (currentLevel != null)
			{
				EditorGUI.BeginChangeCheck();

				currentLevel.levelNumber = EditorGUILayout.IntField("Level Number", currentLevel.levelNumber);
				currentLevel.levelName = EditorGUILayout.TextField("Level Name", currentLevel.levelName);
				currentLevel.difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Difficulty", currentLevel.difficulty);

				EditorGUILayout.Space(5);
				currentLevel.boardWidth = EditorGUILayout.IntSlider("Width", currentLevel.boardWidth, 4, 12);
				currentLevel.boardHeight = EditorGUILayout.IntSlider("Height", currentLevel.boardHeight, 4, 12);
				currentLevel.maxLayers = EditorGUILayout.IntSlider("Layers", currentLevel.maxLayers, 1, 5);

				EditorGUILayout.Space(5);
				currentLevel.slotCount = EditorGUILayout.IntSlider("Slot Count", currentLevel.slotCount, 5, 10);
				currentLevel.matchCount = EditorGUILayout.IntSlider("Match Count", currentLevel.matchCount, 3, 4);

				if (EditorGUI.EndChangeCheck())
				{
					EditorUtility.SetDirty(currentLevel);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("No level loaded.", MessageType.Info);
				if (GUILayout.Button("Create New Level"))
					CreateNewLevel();
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawStatistics()
		{
			if (currentLevel == null) return;

			showStatistics = EditorGUILayout.Foldout(showStatistics, "Statistics", true);
			if (!showStatistics) return;

			EditorGUILayout.BeginVertical("box");

			int totalTiles = currentLevel.tilePlacements != null ? currentLevel.tilePlacements.Count : 0;
			EditorGUILayout.LabelField("Total Tiles:", totalTiles.ToString());

			// 레이어별 타일 수
			if (currentLevel.tilePlacements != null)
			{
				for (int i = 0; i < currentLevel.maxLayers && i < LayerColors.Length; i++)
				{
					int count = currentLevel.tilePlacements.Count(t => t != null && t.layer == i);
					EditorGUILayout.LabelField($"  Layer {i}:", count.ToString());
				}
			}

			// 유효성 검사
			EditorGUILayout.Space(5);
			string errorMsg;
			if (currentLevel.Validate(out errorMsg))
			{
				EditorGUILayout.HelpBox("Level is valid", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox(errorMsg, MessageType.Warning);
			}

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Grid Editor

		private void DrawGridEditor()
		{
			Rect gridArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
				GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			// 배경
			EditorGUI.DrawRect(gridArea, new Color(0.15f, 0.15f, 0.15f));

			if (currentLevel == null)
			{
				GUI.Label(gridArea, "Create or Open a Level", new GUIStyle(EditorStyles.largeLabel)
				{
					alignment = TextAnchor.MiddleCenter
				});
				return;
			}

			DrawGrid(gridArea);
			DrawTiles(gridArea);
			HandleGridInput(gridArea);
		}

		private void DrawGrid(Rect area)
		{
			if (currentLevel == null) return;

			float scaledCellSize = cellSize * zoomLevel;
			float gridWidth = currentLevel.boardWidth * scaledCellSize;
			float gridHeight = currentLevel.boardHeight * scaledCellSize;

			Vector2 gridOrigin = new Vector2(
				area.x + (area.width - gridWidth) / 2 + panOffset.x,
				area.y + (area.height - gridHeight) / 2 + panOffset.y
			);

			// 그리드 배경
			Rect gridBounds = new Rect(gridOrigin.x, gridOrigin.y, gridWidth, gridHeight);
			EditorGUI.DrawRect(gridBounds, new Color(0.25f, 0.25f, 0.28f));

			// 셀 그리기
			for (int x = 0; x < currentLevel.boardWidth; x++)
			{
				for (int y = 0; y < currentLevel.boardHeight; y++)
				{
					Rect cellRect = new Rect(
						gridOrigin.x + x * scaledCellSize + cellPadding,
						gridOrigin.y + (currentLevel.boardHeight - 1 - y) * scaledCellSize + cellPadding,
						scaledCellSize - cellPadding * 2,
						scaledCellSize - cellPadding * 2
					);

					Color cellColor = (x + y) % 2 == 0 ?
						new Color(0.3f, 0.3f, 0.33f) :
						new Color(0.28f, 0.28f, 0.31f);

					EditorGUI.DrawRect(cellRect, cellColor);
				}
			}
		}

		private void DrawTiles(Rect area)
		{
			if (currentLevel == null || currentLevel.tilePlacements == null) return;

			float scaledCellSize = cellSize * zoomLevel;
			Vector2 gridOrigin = GetGridOrigin(area);

			foreach (var tile in currentLevel.tilePlacements)
			{
				if (tile == null) continue;

				if (!showAllLayers && tile.layer != currentLayer)
					continue;

				float alpha = (showAllLayers && tile.layer != currentLayer) ? layerOpacity : 1f;
				DrawTile(tile, gridOrigin, scaledCellSize, alpha);
			}
		}

		private void DrawTile(TilePlacement tile, Vector2 gridOrigin, float scaledCellSize, float alpha)
		{
			if (tile == null || currentLevel == null) return;

			float layerOffset = tile.layer * 3f * zoomLevel;

			Rect tileRect = new Rect(
				gridOrigin.x + tile.gridX * scaledCellSize + cellPadding + layerOffset,
				gridOrigin.y + (currentLevel.boardHeight - 1 - tile.gridY) * scaledCellSize + cellPadding - layerOffset,
				scaledCellSize - cellPadding * 2,
				scaledCellSize - cellPadding * 2
			);

			// 레이어 색상
			int colorIndex = Mathf.Clamp(tile.layer, 0, LayerColors.Length - 1);
			Color tileColor = LayerColors[colorIndex];
			tileColor.a = alpha;

			// 선택 하이라이트
			if (selectedTiles != null && selectedTiles.Contains(tile))
			{
				Rect highlight = new Rect(tileRect.x - 2, tileRect.y - 2, tileRect.width + 4, tileRect.height + 4);
				EditorGUI.DrawRect(highlight, new Color(1f, 0.8f, 0.2f, alpha));
			}

			EditorGUI.DrawRect(tileRect, tileColor);

			// 타일 타입 표시
			if (!string.IsNullOrEmpty(tile.tileTypeId))
			{
				string[] parts = tile.tileTypeId.Split('_');
				if (parts.Length >= 2)
				{
					string display = GetShortDisplay(parts[0], parts[1]);
					GUI.Label(tileRect, display, new GUIStyle(EditorStyles.boldLabel)
					{
						alignment = TextAnchor.MiddleCenter,
						fontSize = Mathf.RoundToInt(10 * zoomLevel),
						normal = { textColor = new Color(1, 1, 1, alpha) }
					});
				}
			}
		}

		private string GetShortDisplay(string suit, string rank)
		{
			string s = suit switch
			{
				"Spade" => "♠",
				"Heart" => "♥",
				"Diamond" => "♦",
				"Club" => "♣",
				_ => "?"
			};

			string r = rank switch
			{
				"Ace" => "A",
				"Jack" => "J",
				"Queen" => "Q",
				"King" => "K",
				_ => rank.Length > 0 ? rank.Substring(0, Mathf.Min(2, rank.Length)) : "?"
			};

			return $"{r}\n{s}";
		}

		private Vector2 GetGridOrigin(Rect area)
		{
			if (currentLevel == null) return Vector2.zero;

			float scaledCellSize = cellSize * zoomLevel;
			float gridWidth = currentLevel.boardWidth * scaledCellSize;
			float gridHeight = currentLevel.boardHeight * scaledCellSize;

			return new Vector2(
				area.x + (area.width - gridWidth) / 2 + panOffset.x,
				area.y + (area.height - gridHeight) / 2 + panOffset.y
			);
		}

		private void HandleGridInput(Rect area)
		{
			Event e = Event.current;
			if (e == null) return;

			if (!area.Contains(e.mousePosition)) return;

			Vector2Int gridPos = GetGridPosition(e.mousePosition, area);

			if (e.type == EventType.MouseDown && e.button == 0)
			{
				HandleLeftClick(gridPos);
				e.Use();
			}
			else if (e.type == EventType.MouseDrag && e.button == 0)
			{
				if (currentTool == EditorTool.Paint)
					PaintTile(gridPos);
				else if (currentTool == EditorTool.Erase)
					EraseTile(gridPos);
				Repaint();
			}
			else if (e.type == EventType.MouseDrag && e.button == 2)
			{
				panOffset += e.delta;
				Repaint();
			}
			else if (e.type == EventType.ScrollWheel)
			{
				zoomLevel = Mathf.Clamp(zoomLevel - e.delta.y * 0.05f, 0.5f, 2f);
				Repaint();
				e.Use();
			}
		}

		private Vector2Int GetGridPosition(Vector2 mousePos, Rect area)
		{
			if (currentLevel == null) return Vector2Int.zero;

			Vector2 gridOrigin = GetGridOrigin(area);
			float scaledCellSize = cellSize * zoomLevel;

			int x = Mathf.FloorToInt((mousePos.x - gridOrigin.x) / scaledCellSize);
			int y = currentLevel.boardHeight - 1 - Mathf.FloorToInt((mousePos.y - gridOrigin.y) / scaledCellSize);

			return new Vector2Int(x, y);
		}

		private bool IsValidGridPosition(Vector2Int pos)
		{
			return currentLevel != null &&
				   pos.x >= 0 && pos.x < currentLevel.boardWidth &&
				   pos.y >= 0 && pos.y < currentLevel.boardHeight;
		}

		private void HandleLeftClick(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos)) return;

			switch (currentTool)
			{
				case EditorTool.Select:
					var tile = GetTileAt(gridPos, currentLayer);
					if (selectedTiles == null) selectedTiles = new List<TilePlacement>();
					selectedTiles.Clear();
					if (tile != null)
						selectedTiles.Add(tile);
					break;

				case EditorTool.Paint:
					RecordUndo("Paint");
					PaintTile(gridPos);
					break;

				case EditorTool.Erase:
					RecordUndo("Erase");
					EraseTile(gridPos);
					break;
			}

			Repaint();
		}

		private TilePlacement GetTileAt(Vector2Int pos, int layer)
		{
			if (currentLevel == null || currentLevel.tilePlacements == null) return null;
			return currentLevel.tilePlacements.FirstOrDefault(t =>
				t != null && t.gridX == pos.x && t.gridY == pos.y && t.layer == layer);
		}

		#endregion

		#region Right Panel

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(180));

			EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

			showTileTypes = EditorGUILayout.Foldout(showTileTypes, "Card Tiles", true);
			if (showTileTypes)
			{
				DrawTilePalette();
			}

			EditorGUILayout.Space(10);

			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

			GUI.enabled = currentLevel != null;

			if (GUILayout.Button("Auto Fill Random"))
			{
				RecordUndo("Auto Fill");
				AutoFillRandom();
			}

			if (GUILayout.Button("Clear Layer " + currentLayer))
			{
				RecordUndo("Clear Layer");
				ClearLayer(currentLayer);
			}

			if (GUILayout.Button("Clear All"))
			{
				RecordUndo("Clear All");
				ClearAllTiles();
			}

			GUI.enabled = true;

			EditorGUILayout.EndVertical();
		}

		private void DrawTilePalette()
		{
			if (tileTypePresets == null) return;

			float buttonSize = 35f;
			int columns = 4;

			foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
			{
				string suitSymbol = suit switch
				{
					CardSuit.Spade => "♠",
					CardSuit.Heart => "♥",
					CardSuit.Diamond => "♦",
					CardSuit.Club => "♣",
					_ => "?"
				};

				EditorGUILayout.LabelField($"{suitSymbol} {suit}", EditorStyles.miniLabel);

				int count = 0;
				EditorGUILayout.BeginHorizontal();

				foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
				{
					if (count > 0 && count % columns == 0)
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
					}

					string typeId = $"{suit}_{rank}";
					string rankStr = rank switch
					{
						CardRank.Ace => "A",
						CardRank.Jack => "J",
						CardRank.Queen => "Q",
						CardRank.King => "K",
						_ => ((int)rank).ToString()
					};

					GUI.backgroundColor = selectedTileType == typeId ? Color.yellow : Color.white;
					if (GUILayout.Button(rankStr, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
					{
						selectedTileType = typeId;
						currentTool = EditorTool.Paint;
					}

					count++;
				}

				EditorGUILayout.EndHorizontal();
				GUI.backgroundColor = Color.white;
				EditorGUILayout.Space(5);
			}
		}

		#endregion

		#region Status Bar

		private void DrawStatusBar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			GUILayout.Label($"Tool: {currentTool}");
			GUILayout.Label($"Layer: {currentLayer}");
			GUILayout.Label($"Selected: {(selectedTiles != null ? selectedTiles.Count : 0)}");

			if (!string.IsNullOrEmpty(selectedTileType))
				GUILayout.Label($"Tile: {selectedTileType}");

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Tile Operations

		private void PaintTile(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos) || string.IsNullOrEmpty(selectedTileType)) return;
			if (currentLevel == null) return;
			if (currentLevel.tilePlacements == null)
				currentLevel.tilePlacements = new List<TilePlacement>();

			var existing = GetTileAt(gridPos, currentLayer);
			if (existing != null)
				currentLevel.tilePlacements.Remove(existing);

			currentLevel.tilePlacements.Add(new TilePlacement(gridPos.x, gridPos.y, currentLayer, selectedTileType));
			EditorUtility.SetDirty(currentLevel);
		}

		private void EraseTile(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos)) return;
			if (currentLevel == null || currentLevel.tilePlacements == null) return;

			var tile = GetTileAt(gridPos, currentLayer);
			if (tile != null)
			{
				currentLevel.tilePlacements.Remove(tile);
				if (selectedTiles != null)
					selectedTiles.Remove(tile);
				EditorUtility.SetDirty(currentLevel);
			}
		}

		private void AutoFillRandom()
		{
			if (currentLevel == null) return;
			if (tileTypePresets == null || tileTypePresets.Count == 0) return;
			if (currentLevel.tilePlacements == null)
				currentLevel.tilePlacements = new List<TilePlacement>();

			currentLevel.tilePlacements.Clear();

			int totalCells = currentLevel.boardWidth * currentLevel.boardHeight * currentLevel.maxLayers / 2;
			totalCells = (totalCells / currentLevel.matchCount) * currentLevel.matchCount;

			List<string> tilesToPlace = new List<string>();
			for (int i = 0; i < totalCells / currentLevel.matchCount; i++)
			{
				var randomType = tileTypePresets[Random.Range(0, tileTypePresets.Count)];
				for (int j = 0; j < currentLevel.matchCount; j++)
					tilesToPlace.Add(randomType.typeId);
			}

			// Shuffle
			for (int i = tilesToPlace.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				(tilesToPlace[i], tilesToPlace[j]) = (tilesToPlace[j], tilesToPlace[i]);
			}

			int index = 0;
			for (int layer = 0; layer < currentLevel.maxLayers && index < tilesToPlace.Count; layer++)
			{
				int margin = layer;
				for (int x = margin; x < currentLevel.boardWidth - margin && index < tilesToPlace.Count; x++)
				{
					for (int y = margin; y < currentLevel.boardHeight - margin && index < tilesToPlace.Count; y++)
					{
						if (layer > 0 && (x + y) % 2 != 0) continue;
						currentLevel.tilePlacements.Add(new TilePlacement(x, y, layer, tilesToPlace[index++]));
					}
				}
			}

			EditorUtility.SetDirty(currentLevel);
			Repaint();
		}

		private void ClearLayer(int layer)
		{
			if (currentLevel == null || currentLevel.tilePlacements == null) return;
			currentLevel.tilePlacements.RemoveAll(t => t != null && t.layer == layer);
			if (selectedTiles != null) selectedTiles.Clear();
			EditorUtility.SetDirty(currentLevel);
			Repaint();
		}

		private void ClearAllTiles()
		{
			if (currentLevel == null || currentLevel.tilePlacements == null) return;
			currentLevel.tilePlacements.Clear();
			if (selectedTiles != null) selectedTiles.Clear();
			EditorUtility.SetDirty(currentLevel);
			Repaint();
		}

		#endregion

		#region File Operations

		private void CreateNewLevel()
		{
			string path = EditorUtility.SaveFilePanelInProject("Create New Level", "Level_001", "asset", "Save level asset");
			if (string.IsNullOrEmpty(path)) return;

			currentLevel = ScriptableObject.CreateInstance<LevelData>();
			currentLevel.levelName = System.IO.Path.GetFileNameWithoutExtension(path);
			currentLevel.tilePlacements = new List<TilePlacement>();

			AssetDatabase.CreateAsset(currentLevel, path);
			AssetDatabase.SaveAssets();

			if (selectedTiles != null) selectedTiles.Clear();
			if (undoHistory != null) undoHistory.Clear();
			if (redoHistory != null) redoHistory.Clear();
		}

		private void OpenLevel()
		{
			string path = EditorUtility.OpenFilePanel("Open Level", "Assets", "asset");
			if (string.IsNullOrEmpty(path)) return;

			path = "Assets" + path.Substring(Application.dataPath.Length);
			currentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);

			if (selectedTiles != null) selectedTiles.Clear();
			if (undoHistory != null) undoHistory.Clear();
			if (redoHistory != null) redoHistory.Clear();
		}

		private void SaveCurrentLevel()
		{
			if (currentLevel == null) return;
			EditorUtility.SetDirty(currentLevel);
			AssetDatabase.SaveAssets();
		}

		#endregion

		#region Undo/Redo

		private void RecordUndo(string actionName)
		{
			if (currentLevel == null || currentLevel.tilePlacements == null) return;
			if (undoHistory == null) undoHistory = new List<LevelSnapshot>();
			if (redoHistory == null) redoHistory = new List<LevelSnapshot>();

			var snapshot = new LevelSnapshot
			{
				actionName = actionName,
				tilePlacements = currentLevel.tilePlacements.Select(t => new TilePlacement
				{
					gridX = t.gridX,
					gridY = t.gridY,
					layer = t.layer,
					tileTypeId = t.tileTypeId,
					isLocked = t.isLocked,
					isFrozen = t.isFrozen,
					frozenCount = t.frozenCount
				}).ToList()
			};

			undoHistory.Add(snapshot);
			if (undoHistory.Count > MaxHistorySize)
				undoHistory.RemoveAt(0);

			redoHistory.Clear();
		}

		private void Undo()
		{
			if (undoHistory == null || undoHistory.Count == 0 || currentLevel == null) return;
			if (redoHistory == null) redoHistory = new List<LevelSnapshot>();

			// Save current state to redo
			redoHistory.Add(new LevelSnapshot
			{
				tilePlacements = new List<TilePlacement>(currentLevel.tilePlacements ?? new List<TilePlacement>())
			});

			var prev = undoHistory[undoHistory.Count - 1];
			undoHistory.RemoveAt(undoHistory.Count - 1);

			currentLevel.tilePlacements = new List<TilePlacement>(prev.tilePlacements);
			if (selectedTiles != null) selectedTiles.Clear();

			EditorUtility.SetDirty(currentLevel);
			Repaint();
		}

		private void Redo()
		{
			if (redoHistory == null || redoHistory.Count == 0 || currentLevel == null) return;
			if (undoHistory == null) undoHistory = new List<LevelSnapshot>();

			// Save current state to undo
			undoHistory.Add(new LevelSnapshot
			{
				tilePlacements = new List<TilePlacement>(currentLevel.tilePlacements ?? new List<TilePlacement>())
			});

			var next = redoHistory[redoHistory.Count - 1];
			redoHistory.RemoveAt(redoHistory.Count - 1);

			currentLevel.tilePlacements = new List<TilePlacement>(next.tilePlacements);
			if (selectedTiles != null) selectedTiles.Clear();

			EditorUtility.SetDirty(currentLevel);
			Repaint();
		}

		#endregion

		#region Keyboard Shortcuts

		private void HandleKeyboardShortcuts()
		{
			Event e = Event.current;
			if (e == null || e.type != EventType.KeyDown) return;

			if (e.keyCode == KeyCode.Z && (e.control || e.command))
			{
				if (e.shift) Redo(); else Undo();
				e.Use();
			}
			else if (e.keyCode == KeyCode.Y && (e.control || e.command))
			{
				Redo();
				e.Use();
			}
			else if (e.keyCode == KeyCode.S && (e.control || e.command))
			{
				SaveCurrentLevel();
				e.Use();
			}
			else if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha5)
			{
				int layer = e.keyCode - KeyCode.Alpha1;
				if (currentLevel == null || layer < currentLevel.maxLayers)
					currentLayer = layer;
				e.Use();
			}
		}

		#endregion
	}
}
#endif