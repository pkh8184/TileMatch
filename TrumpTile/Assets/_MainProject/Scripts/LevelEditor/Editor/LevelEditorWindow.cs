#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using TrumpTile.Core;

namespace TileMatch.LevelEditor
{
	public enum EEditorTool { Select, Paint, Erase, Fill, Eyedropper }

	public class LevelSnapshot
	{
		public string actionName;
		public List<TilePlacement> tilePlacements = new List<TilePlacement>();
	}

	public class LevelEditorWindow : EditorWindow
	{
		// 현재 편집 중인 레벨
		private LevelData mCurrentLevel;

		// 에디터 상태
		private EEditorTool mCurrentTool = EEditorTool.Select;
		private int mCurrentLayer = 0;
		private string mSelectedTileType = "";
		private bool mShowAllLayers = true;
		private float mLayerOpacity = 0.5F;

		// 뷰 설정
		private Vector2 mScrollPosition;
		private float mZoomLevel = 1F;
		private Vector2 mPanOffset = Vector2.zero;

		// 그리드 설정
		private float mCellSize = 40F;
		private float mCellPadding = 2F;

		// 선택 상태
		private List<TilePlacement> mSelectedTiles;
		private Vector2 mDragStart;
		private Rect mSelectionRect;

		// UI 패널 상태
		private bool mShowLevelSettings = true;
		private bool mShowTileTypes = true;
		private bool mShowStatistics = true;

		// 히스토리
		private List<LevelSnapshot> mUndoHistory;
		private List<LevelSnapshot> mRedoHistory;
		private const int MAX_HISTORY_SIZE = 50;

		// 타일 프리셋
		private List<TileTypeConfig> mTileTypePresets;

		// 색상
		private static readonly Color[] LayerColors = new Color[]
		{
			new Color(0.2F, 0.6F, 1F, 1F),
			new Color(0.2F, 0.8F, 0.4F, 1F),
			new Color(1F, 0.8F, 0.2F, 1F),
			new Color(1F, 0.4F, 0.4F, 1F),
			new Color(0.8F, 0.4F, 1F, 1F),
		};

		[MenuItem("Tools/Tile Match/Level Editor %#l")]
		public static void OpenWindow()
		{
			LevelEditorWindow window = GetWindow<LevelEditorWindow>();
			window.titleContent = new GUIContent("Level Editor");
			window.minSize = new Vector2(900, 600);
			window.Show();
		}

		private void OnEnable()
		{
			mSelectedTiles = new List<TilePlacement>();
			mUndoHistory = new List<LevelSnapshot>();
			mRedoHistory = new List<LevelSnapshot>();
			InitializeTilePresets();
		}

		private void InitializeTilePresets()
		{
			mTileTypePresets = new List<TileTypeConfig>();

			foreach (ECardSuit suit in System.Enum.GetValues(typeof(ECardSuit)))
			{
				foreach (ECardRank rank in System.Enum.GetValues(typeof(ECardRank)))
				{
					mTileTypePresets.Add(new TileTypeConfig
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
			if (mSelectedTiles == null) mSelectedTiles = new List<TilePlacement>();
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();
			if (mTileTypePresets == null) InitializeTilePresets();

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
			GUI.enabled = mUndoHistory != null && mUndoHistory.Count > 0;
			if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(50)))
				Undo();

			GUI.enabled = mRedoHistory != null && mRedoHistory.Count > 0;
			if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(50)))
				Redo();

			GUI.enabled = true;

			GUILayout.Space(20);

			// 도구 선택
			GUILayout.Label("Tool:", GUILayout.Width(35));

			if (GUILayout.Toggle(mCurrentTool == EEditorTool.Select, "Select", EditorStyles.toolbarButton, GUILayout.Width(50)))
				mCurrentTool = EEditorTool.Select;
			if (GUILayout.Toggle(mCurrentTool == EEditorTool.Paint, "Paint", EditorStyles.toolbarButton, GUILayout.Width(50)))
				mCurrentTool = EEditorTool.Paint;
			if (GUILayout.Toggle(mCurrentTool == EEditorTool.Erase, "Erase", EditorStyles.toolbarButton, GUILayout.Width(50)))
				mCurrentTool = EEditorTool.Erase;

			GUILayout.Space(20);

			// 레이어 선택
			GUILayout.Label("Layer:", GUILayout.Width(40));
			int maxLayers = mCurrentLevel != null ? Mathf.Min(mCurrentLevel.maxLayers, 5) : 4;
			for (int i = 0; i < maxLayers; i++)
			{
				GUI.backgroundColor = mCurrentLayer == i ? LayerColors[i] : Color.white;
				if (GUILayout.Button(i.ToString(), EditorStyles.toolbarButton, GUILayout.Width(25)))
					mCurrentLayer = i;
			}
			GUI.backgroundColor = Color.white;

			GUILayout.Space(10);
			mShowAllLayers = GUILayout.Toggle(mShowAllLayers, "Show All", EditorStyles.toolbarButton, GUILayout.Width(70));

			GUILayout.FlexibleSpace();

			// 줌
			GUILayout.Label("Zoom:", GUILayout.Width(40));
			mZoomLevel = GUILayout.HorizontalSlider(mZoomLevel, 0.5F, 2F, GUILayout.Width(80));
			GUILayout.Label($"{mZoomLevel:F1}x", GUILayout.Width(35));

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Left Panel

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(250));
			mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition);

			DrawLevelSettings();
			DrawStatistics();

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private void DrawLevelSettings()
		{
			mShowLevelSettings = EditorGUILayout.Foldout(mShowLevelSettings, "Level Settings", true);
			if (!mShowLevelSettings) return;

			EditorGUILayout.BeginVertical("box");

			if (mCurrentLevel != null)
			{
				EditorGUI.BeginChangeCheck();

				mCurrentLevel.levelNumber = EditorGUILayout.IntField("Level Number", mCurrentLevel.levelNumber);
				mCurrentLevel.levelName = EditorGUILayout.TextField("Level Name", mCurrentLevel.levelName);
				mCurrentLevel.difficulty = (ELevelDifficulty)EditorGUILayout.EnumPopup("Difficulty", mCurrentLevel.difficulty);

				EditorGUILayout.Space(5);
				mCurrentLevel.boardWidth = EditorGUILayout.IntSlider("Width", mCurrentLevel.boardWidth, 4, 12);
				mCurrentLevel.boardHeight = EditorGUILayout.IntSlider("Height", mCurrentLevel.boardHeight, 4, 12);
				mCurrentLevel.maxLayers = EditorGUILayout.IntSlider("Layers", mCurrentLevel.maxLayers, 1, 5);

				EditorGUILayout.Space(5);
				mCurrentLevel.slotCount = EditorGUILayout.IntSlider("Slot Count", mCurrentLevel.slotCount, 5, 10);
				mCurrentLevel.matchCount = EditorGUILayout.IntSlider("Match Count", mCurrentLevel.matchCount, 3, 4);

				if (EditorGUI.EndChangeCheck())
				{
					EditorUtility.SetDirty(mCurrentLevel);
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
			if (mCurrentLevel == null) return;

			mShowStatistics = EditorGUILayout.Foldout(mShowStatistics, "Statistics", true);
			if (!mShowStatistics) return;

			EditorGUILayout.BeginVertical("box");

			int totalTiles = mCurrentLevel.tilePlacements != null ? mCurrentLevel.tilePlacements.Count : 0;
			EditorGUILayout.LabelField("Total Tiles:", totalTiles.ToString());

			// 레이어별 타일 수
			if (mCurrentLevel.tilePlacements != null)
			{
				for (int i = 0; i < mCurrentLevel.maxLayers && i < LayerColors.Length; i++)
				{
					int count = mCurrentLevel.tilePlacements.Count(t => t != null && t.layer == i);
					EditorGUILayout.LabelField($"  Layer {i}:", count.ToString());
				}
			}

			// 유효성 검사
			EditorGUILayout.Space(5);
			string errorMsg;
			if (mCurrentLevel.Validate(out errorMsg))
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
			EditorGUI.DrawRect(gridArea, new Color(0.15F, 0.15F, 0.15F));

			if (mCurrentLevel == null)
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
			if (mCurrentLevel == null) return;

			float scaledCellSize = mCellSize * mZoomLevel;
			float gridWidth = mCurrentLevel.boardWidth * scaledCellSize;
			float gridHeight = mCurrentLevel.boardHeight * scaledCellSize;

			Vector2 gridOrigin = new Vector2(
				area.x + (area.width - gridWidth) / 2 + mPanOffset.x,
				area.y + (area.height - gridHeight) / 2 + mPanOffset.y
			);

			// 그리드 배경
			Rect gridBounds = new Rect(gridOrigin.x, gridOrigin.y, gridWidth, gridHeight);
			EditorGUI.DrawRect(gridBounds, new Color(0.25F, 0.25F, 0.28F));

			// 셀 그리기
			for (int x = 0; x < mCurrentLevel.boardWidth; x++)
			{
				for (int y = 0; y < mCurrentLevel.boardHeight; y++)
				{
					Rect cellRect = new Rect(
						gridOrigin.x + x * scaledCellSize + mCellPadding,
						gridOrigin.y + (mCurrentLevel.boardHeight - 1 - y) * scaledCellSize + mCellPadding,
						scaledCellSize - mCellPadding * 2,
						scaledCellSize - mCellPadding * 2
					);

					Color cellColor = (x + y) % 2 == 0 ?
						new Color(0.3F, 0.3F, 0.33F) :
						new Color(0.28F, 0.28F, 0.31F);

					EditorGUI.DrawRect(cellRect, cellColor);
				}
			}
		}

		private void DrawTiles(Rect area)
		{
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return;

			float scaledCellSize = mCellSize * mZoomLevel;
			Vector2 gridOrigin = GetGridOrigin(area);

			foreach (TilePlacement tile in mCurrentLevel.tilePlacements)
			{
				if (tile == null) continue;

				if (!mShowAllLayers && tile.layer != mCurrentLayer)
					continue;

				float alpha = (mShowAllLayers && tile.layer != mCurrentLayer) ? mLayerOpacity : 1F;
				DrawTile(tile, gridOrigin, scaledCellSize, alpha);
			}
		}

		private void DrawTile(TilePlacement tile, Vector2 gridOrigin, float scaledCellSize, float alpha)
		{
			if (tile == null || mCurrentLevel == null) return;

			float layerOffset = tile.layer * 3F * mZoomLevel;

			Rect tileRect = new Rect(
				gridOrigin.x + tile.gridX * scaledCellSize + mCellPadding + layerOffset,
				gridOrigin.y + (mCurrentLevel.boardHeight - 1 - tile.gridY) * scaledCellSize + mCellPadding - layerOffset,
				scaledCellSize - mCellPadding * 2,
				scaledCellSize - mCellPadding * 2
			);

			// 레이어 색상
			int colorIndex = Mathf.Clamp(tile.layer, 0, LayerColors.Length - 1);
			Color tileColor = LayerColors[colorIndex];
			tileColor.a = alpha;

			// 선택 하이라이트
			if (mSelectedTiles != null && mSelectedTiles.Contains(tile))
			{
				Rect highlight = new Rect(tileRect.x - 2, tileRect.y - 2, tileRect.width + 4, tileRect.height + 4);
				EditorGUI.DrawRect(highlight, new Color(1F, 0.8F, 0.2F, alpha));
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
						fontSize = Mathf.RoundToInt(10 * mZoomLevel),
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
			if (mCurrentLevel == null) return Vector2.zero;

			float scaledCellSize = mCellSize * mZoomLevel;
			float gridWidth = mCurrentLevel.boardWidth * scaledCellSize;
			float gridHeight = mCurrentLevel.boardHeight * scaledCellSize;

			return new Vector2(
				area.x + (area.width - gridWidth) / 2 + mPanOffset.x,
				area.y + (area.height - gridHeight) / 2 + mPanOffset.y
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
				if (mCurrentTool == EEditorTool.Paint)
					PaintTile(gridPos);
				else if (mCurrentTool == EEditorTool.Erase)
					EraseTile(gridPos);
				Repaint();
			}
			else if (e.type == EventType.MouseDrag && e.button == 2)
			{
				mPanOffset += e.delta;
				Repaint();
			}
			else if (e.type == EventType.ScrollWheel)
			{
				mZoomLevel = Mathf.Clamp(mZoomLevel - e.delta.y * 0.05F, 0.5F, 2F);
				Repaint();
				e.Use();
			}
		}

		private Vector2Int GetGridPosition(Vector2 mousePos, Rect area)
		{
			if (mCurrentLevel == null) return Vector2Int.zero;

			Vector2 gridOrigin = GetGridOrigin(area);
			float scaledCellSize = mCellSize * mZoomLevel;

			int x = Mathf.FloorToInt((mousePos.x - gridOrigin.x) / scaledCellSize);
			int y = mCurrentLevel.boardHeight - 1 - Mathf.FloorToInt((mousePos.y - gridOrigin.y) / scaledCellSize);

			return new Vector2Int(x, y);
		}

		private bool IsValidGridPosition(Vector2Int pos)
		{
			return mCurrentLevel != null &&
				   pos.x >= 0 && pos.x < mCurrentLevel.boardWidth &&
				   pos.y >= 0 && pos.y < mCurrentLevel.boardHeight;
		}

		private void HandleLeftClick(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos)) return;

			switch (mCurrentTool)
			{
				case EEditorTool.Select:
					TilePlacement tile = GetTileAt(gridPos, mCurrentLayer);
					if (mSelectedTiles == null) mSelectedTiles = new List<TilePlacement>();
					mSelectedTiles.Clear();
					if (tile != null)
						mSelectedTiles.Add(tile);
					break;

				case EEditorTool.Paint:
					RecordUndo("Paint");
					PaintTile(gridPos);
					break;

				case EEditorTool.Erase:
					RecordUndo("Erase");
					EraseTile(gridPos);
					break;
			}

			Repaint();
		}

		private TilePlacement GetTileAt(Vector2Int pos, int layer)
		{
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return null;
			return mCurrentLevel.tilePlacements.FirstOrDefault(t =>
				t != null && t.gridX == pos.x && t.gridY == pos.y && t.layer == layer);
		}

		#endregion

		#region Right Panel

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(180));

			EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

			mShowTileTypes = EditorGUILayout.Foldout(mShowTileTypes, "Card Tiles", true);
			if (mShowTileTypes)
			{
				DrawTilePalette();
			}

			EditorGUILayout.Space(10);

			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

			GUI.enabled = mCurrentLevel != null;

			if (GUILayout.Button("Auto Fill Random"))
			{
				RecordUndo("Auto Fill");
				AutoFillRandom();
			}

			if (GUILayout.Button("Clear Layer " + mCurrentLayer))
			{
				RecordUndo("Clear Layer");
				ClearLayer(mCurrentLayer);
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
			if (mTileTypePresets == null) return;

			float buttonSize = 35F;
			int columns = 4;

			foreach (ECardSuit suit in System.Enum.GetValues(typeof(ECardSuit)))
			{
				string suitSymbol = suit switch
				{
					ECardSuit.Spade => "♠",
					ECardSuit.Heart => "♥",
					ECardSuit.Diamond => "♦",
					ECardSuit.Club => "♣",
					_ => "?"
				};

				EditorGUILayout.LabelField($"{suitSymbol} {suit}", EditorStyles.miniLabel);

				int count = 0;
				EditorGUILayout.BeginHorizontal();

				foreach (ECardRank rank in System.Enum.GetValues(typeof(ECardRank)))
				{
					if (count > 0 && count % columns == 0)
					{
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.BeginHorizontal();
					}

					string typeId = $"{suit}_{rank}";
					string rankStr = rank switch
					{
						ECardRank.Ace => "A",
						ECardRank.Jack => "J",
						ECardRank.Queen => "Q",
						ECardRank.King => "K",
						_ => ((int)rank).ToString()
					};

					GUI.backgroundColor = mSelectedTileType == typeId ? Color.yellow : Color.white;
					if (GUILayout.Button(rankStr, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
					{
						mSelectedTileType = typeId;
						mCurrentTool = EEditorTool.Paint;
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

			GUILayout.Label($"Tool: {mCurrentTool}");
			GUILayout.Label($"Layer: {mCurrentLayer}");
			GUILayout.Label($"Selected: {(mSelectedTiles != null ? mSelectedTiles.Count : 0)}");

			if (!string.IsNullOrEmpty(mSelectedTileType))
				GUILayout.Label($"Tile: {mSelectedTileType}");

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#region Tile Operations

		private void PaintTile(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos) || string.IsNullOrEmpty(mSelectedTileType)) return;
			if (mCurrentLevel == null) return;
			if (mCurrentLevel.tilePlacements == null)
				mCurrentLevel.tilePlacements = new List<TilePlacement>();

			TilePlacement existing = GetTileAt(gridPos, mCurrentLayer);
			if (existing != null)
				mCurrentLevel.tilePlacements.Remove(existing);

			mCurrentLevel.tilePlacements.Add(new TilePlacement(gridPos.x, gridPos.y, mCurrentLayer, mSelectedTileType));
			EditorUtility.SetDirty(mCurrentLevel);
		}

		private void EraseTile(Vector2Int gridPos)
		{
			if (!IsValidGridPosition(gridPos)) return;
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return;

			TilePlacement tile = GetTileAt(gridPos, mCurrentLayer);
			if (tile != null)
			{
				mCurrentLevel.tilePlacements.Remove(tile);
				if (mSelectedTiles != null)
					mSelectedTiles.Remove(tile);
				EditorUtility.SetDirty(mCurrentLevel);
			}
		}

		private void AutoFillRandom()
		{
			if (mCurrentLevel == null) return;
			if (mTileTypePresets == null || mTileTypePresets.Count == 0) return;
			if (mCurrentLevel.tilePlacements == null)
				mCurrentLevel.tilePlacements = new List<TilePlacement>();

			mCurrentLevel.tilePlacements.Clear();

			int totalCells = mCurrentLevel.boardWidth * mCurrentLevel.boardHeight * mCurrentLevel.maxLayers / 2;
			totalCells = (totalCells / mCurrentLevel.matchCount) * mCurrentLevel.matchCount;

			List<string> tilesToPlace = new List<string>();
			for (int i = 0; i < totalCells / mCurrentLevel.matchCount; i++)
			{
				TileTypeConfig randomType = mTileTypePresets[Random.Range(0, mTileTypePresets.Count)];
				for (int j = 0; j < mCurrentLevel.matchCount; j++)
					tilesToPlace.Add(randomType.typeId);
			}

			// Shuffle
			for (int i = tilesToPlace.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				(tilesToPlace[i], tilesToPlace[j]) = (tilesToPlace[j], tilesToPlace[i]);
			}

			int index = 0;
			for (int layer = 0; layer < mCurrentLevel.maxLayers && index < tilesToPlace.Count; layer++)
			{
				int margin = layer;
				for (int x = margin; x < mCurrentLevel.boardWidth - margin && index < tilesToPlace.Count; x++)
				{
					for (int y = margin; y < mCurrentLevel.boardHeight - margin && index < tilesToPlace.Count; y++)
					{
						if (layer > 0 && (x + y) % 2 != 0) continue;
						mCurrentLevel.tilePlacements.Add(new TilePlacement(x, y, layer, tilesToPlace[index++]));
					}
				}
			}

			EditorUtility.SetDirty(mCurrentLevel);
			Repaint();
		}

		private void ClearLayer(int layer)
		{
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return;
			mCurrentLevel.tilePlacements.RemoveAll(t => t != null && t.layer == layer);
			if (mSelectedTiles != null) mSelectedTiles.Clear();
			EditorUtility.SetDirty(mCurrentLevel);
			Repaint();
		}

		private void ClearAllTiles()
		{
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return;
			mCurrentLevel.tilePlacements.Clear();
			if (mSelectedTiles != null) mSelectedTiles.Clear();
			EditorUtility.SetDirty(mCurrentLevel);
			Repaint();
		}

		#endregion

		#region File Operations

		private void CreateNewLevel()
		{
			string path = EditorUtility.SaveFilePanelInProject("Create New Level", "Level_001", "asset", "Save level asset");
			if (string.IsNullOrEmpty(path)) return;

			mCurrentLevel = ScriptableObject.CreateInstance<LevelData>();
			mCurrentLevel.levelName = System.IO.Path.GetFileNameWithoutExtension(path);
			mCurrentLevel.tilePlacements = new List<TilePlacement>();

			AssetDatabase.CreateAsset(mCurrentLevel, path);
			AssetDatabase.SaveAssets();

			if (mSelectedTiles != null) mSelectedTiles.Clear();
			if (mUndoHistory != null) mUndoHistory.Clear();
			if (mRedoHistory != null) mRedoHistory.Clear();
		}

		private void OpenLevel()
		{
			string path = EditorUtility.OpenFilePanel("Open Level", "Assets", "asset");
			if (string.IsNullOrEmpty(path)) return;

			path = "Assets" + path.Substring(Application.dataPath.Length);
			mCurrentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);

			if (mSelectedTiles != null) mSelectedTiles.Clear();
			if (mUndoHistory != null) mUndoHistory.Clear();
			if (mRedoHistory != null) mRedoHistory.Clear();
		}

		private void SaveCurrentLevel()
		{
			if (mCurrentLevel == null) return;
			EditorUtility.SetDirty(mCurrentLevel);
			AssetDatabase.SaveAssets();
		}

		#endregion

		#region Undo/Redo

		private void RecordUndo(string actionName)
		{
			if (mCurrentLevel == null || mCurrentLevel.tilePlacements == null) return;
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();

			LevelSnapshot snapshot = new LevelSnapshot
			{
				actionName = actionName,
				tilePlacements = mCurrentLevel.tilePlacements.Select(t => new TilePlacement
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

			mUndoHistory.Add(snapshot);
			if (mUndoHistory.Count > MAX_HISTORY_SIZE)
				mUndoHistory.RemoveAt(0);

			mRedoHistory.Clear();
		}

		private void Undo()
		{
			if (mUndoHistory == null || mUndoHistory.Count == 0 || mCurrentLevel == null) return;
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();

			// Save current state to redo
			mRedoHistory.Add(new LevelSnapshot
			{
				tilePlacements = new List<TilePlacement>(mCurrentLevel.tilePlacements ?? new List<TilePlacement>())
			});

			LevelSnapshot prev = mUndoHistory[mUndoHistory.Count - 1];
			mUndoHistory.RemoveAt(mUndoHistory.Count - 1);

			mCurrentLevel.tilePlacements = new List<TilePlacement>(prev.tilePlacements);
			if (mSelectedTiles != null) mSelectedTiles.Clear();

			EditorUtility.SetDirty(mCurrentLevel);
			Repaint();
		}

		private void Redo()
		{
			if (mRedoHistory == null || mRedoHistory.Count == 0 || mCurrentLevel == null) return;
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();

			// Save current state to undo
			mUndoHistory.Add(new LevelSnapshot
			{
				tilePlacements = new List<TilePlacement>(mCurrentLevel.tilePlacements ?? new List<TilePlacement>())
			});

			LevelSnapshot next = mRedoHistory[mRedoHistory.Count - 1];
			mRedoHistory.RemoveAt(mRedoHistory.Count - 1);

			mCurrentLevel.tilePlacements = new List<TilePlacement>(next.tilePlacements);
			if (mSelectedTiles != null) mSelectedTiles.Clear();

			EditorUtility.SetDirty(mCurrentLevel);
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
				if (mCurrentLevel == null || layer < mCurrentLevel.maxLayers)
					mCurrentLayer = layer;
				e.Use();
			}
		}

		#endregion
	}
}
#endif
