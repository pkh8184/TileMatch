#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TrumpTile.GameMain.Core;
using TrumpTile.LevelEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrumpTile.LevelEditor.Editor
{
	public enum EEditorTool { Select, Paint, Erase, Fill, Eyedropper }

	public class LevelSnapshot
	{
		public string actionName;
		public List<LayerDataWrapper> layerListState;
	}

	public class LevelEditorWindow : EditorWindow
	{
		// 현재 편집 중인 레벨 클론
		private LevelData mCurrentLevelClone;
		// 저장시에 사용할 본래 레벨 참조
		private LevelData mCurrentLevel;

		// 현재 복사된 레벨
		private LevelData mCopiedLevel = null;

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
		private List<TileData> mTileDataPresets;
		private int[] mTileTypeCountArray;

		// 배경 요소 컨테이너
		private List<Sprite> mLevelBackgroundSpriteList;
		private List<Sprite> mTileBackgroundSpriteList;

		// 배경 요소 관리를 위한 에디터 변수
		private List<string> mLevelBackgroundSpriteNameList;
		private List<string> mTileBackgroundSpriteNameList;
		private int mCurrentLevelBackgroundIndex;
		private int mCurrentTileBackgroundIndex;

        private List<int> indexListForBoardSizeUpdate = new List<int>();
        // 색상
        //private static readonly Color[] LayerColors = new Color[]
        //{
        //	new Color(0.2F, 0.6F, 1F, 1F),
        //	new Color(0.2F, 0.8F, 0.4F, 1F),
        //	new Color(1F, 0.8F, 0.2F, 1F),
        //	new Color(1F, 0.4F, 0.4F, 1F),
        //	new Color(0.8F, 0.4F, 1F, 1F),
        //};

        // 오른쪽 패널(타일 카테고리) 스크롤 위치
        private Vector2 mRightPanelScrollPos;

		private List<string> mNotValidateTileTypeIdList = new List<string>();

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
			InitializeBackgroundSpriteList();
		}

		private void InitializeTilePresets()
		{
			mTileDataPresets = new List<TileData>();
			mTileTypeCountArray = new int[(int)ETileCartegory.Length];
            for (int i = 0; i < (int)ETileCartegory.Length; i++)
            {
                string[] guids = AssetDatabase.FindAssets("", new[] { $"Assets/_MainProject/SODatas/TileData/{(ETileCartegory)i}" });
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    TileData tileData = AssetDatabase.LoadAssetAtPath<TileData>(assetPath);
					mTileTypeCountArray[i]++;
					mTileDataPresets.Add(tileData);
                }
            }
		}
		private void InitializeBackgroundSpriteList()
		{
			mLevelBackgroundSpriteList = new List<Sprite>();
			mTileBackgroundSpriteList = new List<Sprite>();
			mLevelBackgroundSpriteNameList = new List<string>();
			mTileBackgroundSpriteNameList = new List<string>();

			string[] pathArray = { "Assets/_MainProject/Textures/UI/Background/Ingame", "Assets/_MainProject/Textures/UI/Background/Tile"};

            for (int i = 0; i < 2; i++)
			{
                string[] guids = AssetDatabase.FindAssets("", new[] { pathArray[i] });
                foreach (var item in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(item);

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

					if(i == 0)
					{
                        mLevelBackgroundSpriteList.Add(sprite);
						mLevelBackgroundSpriteNameList.Add(sprite.name);
                    }
					else
					{
						mTileBackgroundSpriteList.Add(sprite);
                        mTileBackgroundSpriteNameList.Add(sprite.name);
                    }
                }
            }
        }
		private void OnGUI()
		{
			// 리스트 null 체크
			if (mSelectedTiles == null) mSelectedTiles = new List<TilePlacement>();
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();
			if (mTileDataPresets == null) InitializeTilePresets();

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

			GUI.enabled = mCurrentLevelClone != null;
            if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(50)))
				CopyLevel();

			GUI.enabled = mCopiedLevel != null;
            if (GUILayout.Button("Paste", EditorStyles.toolbarButton, GUILayout.Width(50)))
				PasteLevel();


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
			if(mCurrentLevelClone != null && mCurrentLevelClone.layerList != null)
			{
                for (int i = 0; i < mCurrentLevelClone.layerList.Count; i++)
                {
                    GUI.backgroundColor = mCurrentLayer == i ? Color.green : Color.white;
                    if (GUILayout.Button(i.ToString(), EditorStyles.toolbarButton, GUILayout.Width(25)))
                        mCurrentLayer = i;
                }
            }
			GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField("비고 : Layer 0 = 최하단, 오름차순");

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

			if (mCurrentLevelClone != null)
			{
				EditorGUI.BeginChangeCheck();

				mCurrentLevelClone.levelNumber = EditorGUILayout.IntField("Level Number", mCurrentLevelClone.levelNumber);
				mCurrentLevelClone.levelName = EditorGUILayout.TextField("Level Name", mCurrentLevelClone.levelName);

				EditorGUILayout.Space(5);
                EditorGUI.BeginChangeCheck();
                mCurrentLevelClone.boardWidth = EditorGUILayout.IntSlider("Width", mCurrentLevelClone.boardWidth, 4, 8);
                if (EditorGUI.EndChangeCheck())
                {
                    for(int i = 0; i < mCurrentLevelClone.layerList.Count; i++)
					{
						for(int j = 0; j < mCurrentLevelClone.layerList[i].tilePlacementList.Count; j++)
						{
							if(mCurrentLevelClone.layerList[i].tilePlacementList[j].gridX > GetMaxGridXIndexByLayer(i))
							{
								indexListForBoardSizeUpdate.Add(j);
							}
						}
						for(int j = indexListForBoardSizeUpdate.Count - 1; j >= 0; j--)
						{
							mCurrentLevelClone.layerList[i].tilePlacementList.RemoveAt(indexListForBoardSizeUpdate[j]);
                        }
						indexListForBoardSizeUpdate.Clear();
                    }
				}

				EditorGUI.BeginChangeCheck();
                mCurrentLevelClone.boardHeight = EditorGUILayout.IntSlider("Height", mCurrentLevelClone.boardHeight, 4, 8);
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < mCurrentLevelClone.layerList.Count; i++)
                    {
                        for (int j = 0; j < mCurrentLevelClone.layerList[i].tilePlacementList.Count; j++)
                        {
                            if (mCurrentLevelClone.layerList[i].tilePlacementList[j].gridY > GetMaxGridYIndexByLayer(i))
                            {
                                indexListForBoardSizeUpdate.Add(j);
                            }
                        }
                        for (int j = indexListForBoardSizeUpdate.Count - 1; j >= 0; j--)
                        {
                            mCurrentLevelClone.layerList[i].tilePlacementList.RemoveAt(indexListForBoardSizeUpdate[j]);
                        }
                        indexListForBoardSizeUpdate.Clear();
                    }
                }

                EditorGUI.BeginChangeCheck();
                mCurrentLevelClone.maxLayers = EditorGUILayout.IntSlider("Layers", mCurrentLevelClone.maxLayers, 1, 999);
                if (EditorGUI.EndChangeCheck())
                {
                    while (mCurrentLevelClone.layerList.Count > mCurrentLevelClone.maxLayers)
                        mCurrentLevelClone.layerList.RemoveAt(mCurrentLevelClone.layerList.Count - 1);

                    while (mCurrentLevelClone.layerList.Count < mCurrentLevelClone.maxLayers)
                        mCurrentLevelClone.layerList.Add(new LayerDataWrapper());
                }

				if (mLevelBackgroundSpriteList.Count > 0)
				{
					mCurrentLevelBackgroundIndex = EditorGUILayout.Popup(new GUIContent("Level Background"), mCurrentLevelBackgroundIndex, mLevelBackgroundSpriteNameList.ToArray());
					mCurrentLevelClone.levelBackgroundSprite = mLevelBackgroundSpriteList[mCurrentLevelBackgroundIndex];
				}
				if(mTileBackgroundSpriteList.Count > 0)
				{
                    mCurrentTileBackgroundIndex = EditorGUILayout.Popup(new GUIContent("Tile Background"), mCurrentTileBackgroundIndex, mTileBackgroundSpriteNameList.ToArray());
                    mCurrentLevelClone.tileBackgroundSprite = mTileBackgroundSpriteList[mCurrentTileBackgroundIndex];
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
			if (mCurrentLevelClone == null) return;

			mShowStatistics = EditorGUILayout.Foldout(mShowStatistics, "Statistics", true);
			if (!mShowStatistics) return;

			EditorGUILayout.BeginVertical("box");

			int totalTiles = mCurrentLevelClone.layerList != null ? mCurrentLevelClone.GetTileCount() : 0;
			EditorGUILayout.LabelField("Total Tiles:", totalTiles.ToString());

			// 레이어별 타일 수
			if (mCurrentLevelClone.layerList != null)
			{
				for (int i = 0; i < mCurrentLevelClone.layerList.Count; i++)
				{
					if(mCurrentLevelClone.layerList[i].tilePlacementList == null) continue;
					int count = mCurrentLevelClone.layerList[i].tilePlacementList.Count;
					EditorGUILayout.LabelField($"  Layer {i}:", count.ToString());
				}
			}

			// 유효성 검사
			EditorGUILayout.Space(5);
			string errorMsg;
			if (mCurrentLevelClone.Validate(out errorMsg, out mNotValidateTileTypeIdList))
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
            Rect gridArea = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,GUILayout.Width(position.width - 180 - 250 - 30),GUILayout.ExpandHeight(true));

            // 배경
            EditorGUI.DrawRect(gridArea, new Color(0.15F, 0.15F, 0.15F));

			if (mCurrentLevelClone == null)
			{
				GUI.Label(gridArea, "Create or Open a Level", new GUIStyle(EditorStyles.largeLabel)
				{
					alignment = TextAnchor.MiddleCenter
				});
				return;
			}

			DrawChangeLevelButton(gridArea);

            //그리드 영역 클립핑
            GUI.BeginClip(gridArea);


            //클립핑된 영역 내의 로컬 좌표계에서 실제 그리드, 타일 그리기
            Rect localArea = new Rect(0, 0, gridArea.width, gridArea.height);
            DrawGrid(localArea);
            DrawTiles(localArea);

            GUI.EndClip();


            HandleGridInput(gridArea);
		}
		private void DrawChangeLevelButton(Rect gridArea)
		{
            float btnW = 30f;
            float btnH = 60f;

            Rect leftBtnRect = new Rect(
                gridArea.x,
                gridArea.y + gridArea.height / 2f - btnH / 2f,
                btnW, btnH
            );

            Rect rightBtnRect = new Rect(
                gridArea.xMax - btnW,
                gridArea.y + gridArea.height / 2f - btnH / 2f,
                btnW, btnH
            );

            if (GUI.Button(leftBtnRect, "◀"))
            {
                if (mCurrentLevelClone.levelNumber == 1)
                {
                    string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_MainProject/SODatas/Levels" });

                    LevelData lastLevel = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[guids.Length - 1]));

                    mCurrentLevelClone = lastLevel.Clone();
                    Repaint();
                }
                else
                {
                    int prevNumber = mCurrentLevelClone.levelNumber - 1;
                    while (AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_MainProject/SODatas/Levels/Level_{prevNumber.ToString("D3")}.asset") == null)
                    {
                        prevNumber--;
                        if (prevNumber < 1)
                        {
                            Debug.LogError("[LevelEditor]불러올 수 있는 이전 레벨이 없습니다!");
                            break;
                        }
                    }
                    LevelData prevLevel = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_MainProject/SODatas/Levels/Level_{prevNumber.ToString("D3")}.asset");

                    mCurrentLevelClone = prevLevel.Clone();
                    Repaint();
                }
				mCurrentLayer = 0;
            }
            if (GUI.Button(rightBtnRect, "▶"))
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_MainProject/SODatas/Levels" });

                int maxLevel = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[guids.Length - 1])).levelNumber;

                if (mCurrentLevelClone.levelNumber == maxLevel)
                {
                    LevelData nextLevel = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[0]));

                    mCurrentLevelClone = nextLevel.Clone();
                    Repaint();
                }
                else
                {
                    int nextNumber = mCurrentLevelClone.levelNumber + 1;
                    while (AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_MainProject/SODatas/Levels/Level_{nextNumber.ToString("D3")}.asset") == null)
                    {
                        nextNumber++;
                        if (nextNumber > maxLevel)
                        {
                            Debug.LogError("[LevelEditor]불러올 수 있는 다음 레벨이 없습니다!");
                            break;
                        }
                    }
                    LevelData nextLevel = AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_MainProject/SODatas/Levels/Level_{nextNumber.ToString("D3")}.asset");

                    mCurrentLevelClone = nextLevel.Clone();
                    Repaint();
                }
                mCurrentLayer = 0;
            }
        }		
		private void DrawGrid(Rect area)
		{
			if (mCurrentLevelClone == null) return;

			float scaledCellSize = mCellSize * mZoomLevel;

			float gridWidth = GetGridWidthByLayer(scaledCellSize, mCurrentLayer);
			float gridHeight = GetGridHeightByLayer(scaledCellSize, mCurrentLayer);

            int currentLayerWidth = 0;
            int currentLayerHeight = 0;

            if (mCurrentLayer % 2 == 0)
			{
				currentLayerWidth = mCurrentLevelClone.boardWidth;
				currentLayerHeight = mCurrentLevelClone.boardHeight;
            }
			else
			{
				if (mCurrentLevelClone.boardWidth < 8)
				{
                    currentLayerWidth = mCurrentLevelClone.boardWidth + 1;
                }
				else
				{
                    currentLayerWidth = mCurrentLevelClone.boardWidth - 1;
                }
				if(mCurrentLevelClone.boardHeight < 8)
				{
					currentLayerHeight = mCurrentLevelClone.boardHeight + 1;
                }
				else
				{
					currentLayerHeight = mCurrentLevelClone.boardHeight - 1;
                }
            }
			Vector2 gridOrigin = new Vector2(
				area.x + (area.width - gridWidth) / 2 + mPanOffset.x,
				area.y + (area.height - gridHeight) / 2 + mPanOffset.y
			);

			// 그리드 배경
			Rect gridBounds = new Rect(gridOrigin.x, gridOrigin.y, gridWidth, gridHeight);
			EditorGUI.DrawRect(gridBounds, new Color(0.25F, 0.25F, 0.28F));

			if (mCurrentLayer % 2 == 0)
			{
				for (int x = 0; x < currentLayerWidth; x++)
				{
					for (int y = 0; y < currentLayerHeight; y++)
					{
						Rect cellRect = new Rect(
							gridOrigin.x + x * scaledCellSize + mCellPadding,
							gridOrigin.y + (currentLayerHeight - 1 - y) * scaledCellSize + mCellPadding,
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
			else
			{
                for (int x = 0; x < currentLayerWidth; x++)
                {
                    for (int y = 0; y < currentLayerHeight; y++)
                    {
                        Rect cellRect = new Rect(
                            gridOrigin.x + x * scaledCellSize + mCellPadding,
                            gridOrigin.y + (currentLayerHeight - 1 - y) * scaledCellSize + mCellPadding,
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
		}

		private void DrawTiles(Rect area)
		{
			//레이어별로 타일 그려주기
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return;

			float scaledCellSize = mCellSize * mZoomLevel;
        
            for (int i = 0; i < mCurrentLevelClone.layerList.Count; i++)
			{
				if (mCurrentLevelClone.layerList[i] == null) continue;
				if (mCurrentLevelClone.layerList[i].tilePlacementList == null) continue;
				if (i == mCurrentLayer) continue;

				float alpha = mShowAllLayers ? mLayerOpacity + (i * 0.1f) : 1F;

				Vector2 gridOrigin = GetGridOrigin(area, i);

                foreach (TilePlacement tile in mCurrentLevelClone.layerList[i].tilePlacementList)
				{
					DrawTile(tile, gridOrigin, scaledCellSize, alpha, i);
				}
			}
			if (mCurrentLevelClone.layerList.Count == 0) return;
            //if (mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList == null) return;

            foreach (TilePlacement tile in mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList)
            {
                DrawTile(tile, GetGridOrigin(area, mCurrentLayer), scaledCellSize, 1, mCurrentLayer);
            }
            //foreach (TilePlacement tile in mCurrentLevelClone.tilePlacements)
            //{
            //	if (tile == null) continue;

            //	if (!mShowAllLayers && tile.layer != mCurrentLayer)
            //		continue;

            //             Vector2 gridOrigin = GetGridOrigin(area, tile.layer);

            //             float alpha = (mShowAllLayers && tile.layer != mCurrentLayer) ? mLayerOpacity : 1F;
            //	DrawTile(tile, gridOrigin, scaledCellSize, alpha);
            //}
        }

		private void DrawTile(TilePlacement tile, Vector2 gridOrigin, float scaledCellSize, float alpha, int layer)
		{
			if (tile == null || mCurrentLevelClone == null) return;

            int layerHeight = layer % 2 == 0
							? mCurrentLevelClone.boardHeight
							: (mCurrentLevelClone.boardHeight < 8 ? mCurrentLevelClone.boardHeight + 1 : mCurrentLevelClone.boardHeight - 1);


            Rect tileRect = new Rect(
				gridOrigin.x + tile.gridX * scaledCellSize + mCellPadding,
                gridOrigin.y + (layerHeight - 1 - tile.gridY) * scaledCellSize + mCellPadding,
				scaledCellSize - mCellPadding * 2,
				scaledCellSize - mCellPadding * 2
			);

			// 레이어 색상
			Color tileColor = new Color(1f, 1f, 1f, alpha);

			// 선택 하이라이트
			if (mSelectedTiles != null && mSelectedTiles.Contains(tile))
			{
				Rect highlight = new Rect(tileRect.x - 2, tileRect.y - 2, tileRect.width + 4, tileRect.height + 4);
				EditorGUI.DrawRect(highlight, new Color(1F, 0.8F, 0.2F, alpha));
			}
			// 3의 배수가 아닌 타일 경고 하이라이트
			if(mNotValidateTileTypeIdList.Contains(tile.tileTypeId))
			{
                Rect highlight = new Rect(tileRect.x - 2, tileRect.y - 2, tileRect.width + 4, tileRect.height + 4);
                EditorGUI.DrawRect(highlight, new Color(1F, 0, 0, alpha));
            }
			
			Sprite sprite = mTileDataPresets.Find(x => x.tileTypeId == tile.tileTypeId).sprite;

			EditorGUI.DrawRect(tileRect, tileColor);
			Color origin = GUI.color;
			GUI.color = tileColor;
			GUI.DrawTexture(tileRect, sprite.texture);
			GUI.color = origin;

			// 타일 타입 표시
			//if (!string.IsNullOrEmpty(tile.tileTypeId))
			//{
			//	string[] parts = tile.tileTypeId.Split('_');
			//	if (parts.Length >= 2)
			//	{
			//		string display = GetShortDisplay(parts[0], parts[1]);
			//		GUI.Label(tileRect, display, new GUIStyle(EditorStyles.boldLabel)
			//		{
			//			alignment = TextAnchor.MiddleCenter,
			//			fontSize = Mathf.RoundToInt(10 * mZoomLevel),
			//			normal = { textColor = new Color(1, 1, 1, alpha) }
			//		});
			//	}
			//}
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

		private Vector2 GetGridOrigin(Rect area, int layer = 0)
		{
			if (mCurrentLevelClone == null) return Vector2.zero;

			float scaledCellSize = mCellSize * mZoomLevel;
			float gridWidth = GetGridWidthByLayer(scaledCellSize, layer);
            float gridHeight = GetGridHeightByLayer(scaledCellSize, layer);

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

			Vector2 gridPos = GetGridPosition(e.mousePosition, area);

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
            else if (e.type == EventType.MouseDown && e.button == 1)
            {
                HandleRightClick(gridPos);
                e.Use();
            }
			else if(e.type == EventType.MouseDrag && e.button == 1)
			{
                EraseTile(gridPos);
                Repaint();
            }
        }

		private Vector2 GetGridPosition(Vector2 mousePos, Rect area)
		{
			if (mCurrentLevelClone == null) return Vector2.zero;

			Vector2 gridOrigin = GetGridOrigin(area, mCurrentLayer);
			float scaledCellSize = mCellSize * mZoomLevel;

			float x = Mathf.FloorToInt((mousePos.x - gridOrigin.x) / scaledCellSize);
			float y = Mathf.FloorToInt((GetGridHeightByLayer(scaledCellSize, mCurrentLayer) + gridOrigin.y - mousePos.y) / scaledCellSize);

            return new Vector2(x, y);
		}

		private bool IsValidGridPosition(Vector2 pos)
		{
			return mCurrentLevelClone != null &&
				   pos.x >= 0 && pos.x <= GetMaxGridXIndexByLayer(mCurrentLayer) &&
				   pos.y >= 0 && pos.y <= GetMaxGridYIndexByLayer(mCurrentLayer);
		}

		private void HandleLeftClick(Vector2 gridPos)
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
        private void HandleRightClick(Vector2 gridPos)
        {
            if (!IsValidGridPosition(gridPos)) return;

            RecordUndo("Erase");
            EraseTile(gridPos);

            Repaint();
        }
        private TilePlacement GetTileAt(Vector2 pos, int layer)
		{
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return null;
			if (mCurrentLevelClone.layerList[layer] == null || mCurrentLevelClone.layerList[layer].tilePlacementList == null) return null;
			return mCurrentLevelClone.layerList[layer].tilePlacementList.FirstOrDefault(t =>
				t != null && t.gridX == pos.x && t.gridY == pos.y);
		}
		private float GetGridWidthByLayer(float scaledCellSize, int layer = 0)
		{
			float gridWidth = 0;
            if (layer % 2 == 0)
            {
                gridWidth = mCurrentLevelClone.boardWidth * scaledCellSize;
            }
            else
            {
                if (mCurrentLevelClone.boardWidth < 8)
                {
                    gridWidth = (mCurrentLevelClone.boardWidth + 1) * scaledCellSize;
                }
                else
                {
                    gridWidth = (mCurrentLevelClone.boardWidth - 1) * scaledCellSize;
                }
            }
			return gridWidth;
        }
        private float GetGridHeightByLayer(float scaledCellSize, int layer = 0)
        {
            float gridHeight = 0;
            if (layer % 2 == 0)
            {
                gridHeight = mCurrentLevelClone.boardHeight * scaledCellSize;
            }
            else
            {
                if (mCurrentLevelClone.boardHeight < 8)
                {
                    gridHeight = (mCurrentLevelClone.boardHeight + 1) * scaledCellSize;
                }
                else
                {
                    gridHeight = (mCurrentLevelClone.boardHeight - 1) * scaledCellSize;
                }
            }
            return gridHeight;
        }
		private int GetMaxGridXIndexByLayer(int layer = 0)
		{
			if(layer % 2 ==0)
			{
				return mCurrentLevelClone.boardWidth - 1;
            }
			else
			{
				if (mCurrentLevelClone.boardWidth < 8)
				{
					return mCurrentLevelClone.boardWidth;
				}
				else
				{
					return mCurrentLevelClone.boardWidth - 1;
                }
            }
		}
        private int GetMaxGridYIndexByLayer(int layer = 0)
        {
            if (layer % 2 == 0)
            {
                return mCurrentLevelClone.boardHeight - 1;
            }
            else
            {
                if (mCurrentLevelClone.boardHeight < 8)
                {
                    return mCurrentLevelClone.boardHeight;
                }
                else
                {
                    return mCurrentLevelClone.boardHeight - 1;
                }
            }
        }
		private int GetGridSizeXByLayer(int layer = 0)
		{
            if (layer % 2 == 0)
            {
                return mCurrentLevelClone.boardWidth;
            }
            else
            {
                if (mCurrentLevelClone.boardWidth < 8)
                {
                    return mCurrentLevelClone.boardWidth + 1;
                }
                else
                {
                    return mCurrentLevelClone.boardWidth - 1;
                }
            }
        }
        private int GetGridSizeYByLayer(int layer = 0)
        {
            if (layer % 2 == 0)
            {
                return mCurrentLevelClone.boardHeight;
            }
            else
            {
                if (mCurrentLevelClone.boardHeight < 8)
                {
                    return mCurrentLevelClone.boardHeight + 1;
                }
                else
                {
                    return mCurrentLevelClone.boardHeight - 1;
                }
            }
        }
        #endregion

        #region Right Panel

        private void DrawRightPanel()
		{
			mRightPanelScrollPos = EditorGUILayout.BeginScrollView(mRightPanelScrollPos, GUILayout.Width(200), GUILayout.Height(position.height));

			EditorGUILayout.BeginVertical();

			EditorGUILayout.LabelField("Tile Palette", EditorStyles.boldLabel);

			mShowTileTypes = EditorGUILayout.Foldout(mShowTileTypes, "Tile Category", true);
			if (mShowTileTypes)
			{
				DrawTilePalette();
			}

			EditorGUILayout.Space(10);

			// Quick Actions
			EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

			GUI.enabled = mCurrentLevelClone != null;

			if (GUILayout.Button("Auto Fill Random Layer " + mCurrentLayer))
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

			EditorGUILayout.EndScrollView();
		}

		private void DrawTilePalette()
		{
			if (mTileDataPresets == null) return;

			float buttonSize = 35F;
			int columns = 4;
			int total = 0;
            for (int i = 0; i < (int)ETileCartegory.Length; i++)
			{
                EditorGUILayout.LabelField($"{(ETileCartegory)i}", EditorStyles.miniLabel);
                int count = 0;
                EditorGUILayout.BeginHorizontal();

				for(int j = 0; j < mTileTypeCountArray[i]; j++)
				{
                    if (count > 0 && count % columns == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
					GUI.backgroundColor = mSelectedTileType == mTileDataPresets[total].tileTypeId ? Color.yellow : Color.white;

					if (GUILayout.Button(mTileDataPresets[total].sprite.texture, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
					{
						mSelectedTileType = mTileDataPresets[total].tileTypeId;
						mCurrentTool = EEditorTool.Paint;
					}
					count++;
					total++;
				}

                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.Space(5);
            }
			//foreach (ECardSuit suit in System.Enum.GetValues(typeof(ECardSuit)))
			//{
			//	string suitSymbol = suit switch
			//	{
			//		ECardSuit.Spade => "♠",
			//		ECardSuit.Heart => "♥",
			//		ECardSuit.Diamond => "♦",
			//		ECardSuit.Club => "♣",
			//		_ => "?"
			//	};
   //             EditorGUILayout.LabelField($"{suitSymbol} {suit}", EditorStyles.miniLabel);

			//	int count = 0;
			//	EditorGUILayout.BeginHorizontal();

			//	foreach (ECardRank rank in System.Enum.GetValues(typeof(ECardRank)))
			//	{
			//		if (count > 0 && count % columns == 0)
			//		{
			//			EditorGUILayout.EndHorizontal();
			//			EditorGUILayout.BeginHorizontal();
			//		}

				
			//		string typeId = $"{suit}_{rank}";
			//		string rankStr = rank switch
			//		{
			//			ECardRank.Ace => "A",
			//			ECardRank.Jack => "J",
			//			ECardRank.Queen => "Q",
			//			ECardRank.King => "K",
			//			_ => ((int)rank).ToString()
			//		};

			//		GUI.backgroundColor = mSelectedTileType == typeId ? Color.yellow : Color.white;
			//		if (GUILayout.Button(rankStr, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
			//		{
			//			mSelectedTileType = typeId;
			//			mCurrentTool = EEditorTool.Paint;
			//		}

			//		count++;
			//	}
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

		private void PaintTile(Vector2 gridPos)
		{
			if (!IsValidGridPosition(gridPos) || string.IsNullOrEmpty(mSelectedTileType)) return;
			if (mCurrentLevelClone == null) return;
			if (mCurrentLevelClone.layerList == null)
				mCurrentLevelClone.layerList = new List<LayerDataWrapper>();

			TilePlacement existing = GetTileAt(gridPos, mCurrentLayer);
			if (existing != null)
				mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList.Remove(existing);

			mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList.Add(new TilePlacement(gridPos.x, gridPos.y, mCurrentLayer, mSelectedTileType));
		}

		private void EraseTile(Vector2 gridPos)
		{
			if (!IsValidGridPosition(gridPos)) return;
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return;

			TilePlacement tile = GetTileAt(gridPos, mCurrentLayer);
			if (tile != null)
			{
				mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList.Remove(tile);
				if (mSelectedTiles != null)
					mSelectedTiles.Remove(tile);
			}
		}

		private void AutoFillRandom()
		{
			if (mCurrentLevelClone == null) return;
			if (mTileDataPresets == null || mTileDataPresets.Count == 0) return;
			if (mCurrentLevelClone.layerList == null)
				mCurrentLevelClone.layerList = new List<LayerDataWrapper>();

			mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList.Clear();

			int currentLayerX = GetGridSizeXByLayer(mCurrentLayer);
			int currentLayerY = GetGridSizeYByLayer(mCurrentLayer);

			int totalCells = currentLayerX * currentLayerY;
			totalCells = (totalCells / mCurrentLevelClone.matchCount);

			List<string> tilesToPlace = new List<string>();
			for (int i = 0; i < totalCells; i++)
			{
				TileData randomType = mTileDataPresets[Random.Range(0, mTileDataPresets.Count)];
				for (int j = 0; j < mCurrentLevelClone.matchCount; j++)
					tilesToPlace.Add(randomType.tileTypeId);
			}

			// Shuffle
			for (int i = tilesToPlace.Count - 1; i > 0; i--)
			{
				int j = Random.Range(0, i + 1);
				(tilesToPlace[i], tilesToPlace[j]) = (tilesToPlace[j], tilesToPlace[i]);
			}

			for (int i = 0; i < tilesToPlace.Count; i++)
			{
				mCurrentLevelClone.layerList[mCurrentLayer].tilePlacementList.Add(new TilePlacement(i % currentLayerX, i / currentLayerX, mCurrentLayer, tilesToPlace[i]));
            }

			Repaint();
		}

		private void ClearLayer(int layer)
		{
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return;
			if (layer < 0 || layer >= mCurrentLevelClone.layerList.Count) return;
			mCurrentLevelClone.layerList[layer].tilePlacementList.Clear();
			if (mSelectedTiles != null) mSelectedTiles.Clear();
			Repaint();
		}

		private void ClearAllTiles()
		{
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return;
			foreach (var layer in mCurrentLevelClone.layerList)
			{
				layer.tilePlacementList.Clear();
			}
			if (mSelectedTiles != null) mSelectedTiles.Clear();
			Repaint();
		}

		#endregion

		#region File Operations

		private void CreateNewLevel()
		{
			string path = EditorUtility.SaveFilePanelInProject("Create New Level", "Level_", "asset", "Save level asset", "Assets/_MainProject/SODatas/Levels");
			if (string.IsNullOrEmpty(path)) return;

			mCurrentLevel = ScriptableObject.CreateInstance<LevelData>();
			mCurrentLevel.levelName = System.IO.Path.GetFileNameWithoutExtension(path);
			mCurrentLevel.layerList = new List<LayerDataWrapper>();

			LevelEditorUtilities.CreateLevelDataAsset(mCurrentLevel, path);

            mCurrentLevelClone = mCurrentLevel.Clone();
			mCurrentLevelClone.layerList.Add(new LayerDataWrapper());
			mCurrentLevelClone.levelNumber = int.Parse(mCurrentLevelClone.levelName.Split('_')[1]);

            if (mSelectedTiles != null) mSelectedTiles.Clear();
			if (mUndoHistory != null) mUndoHistory.Clear();
			if (mRedoHistory != null) mRedoHistory.Clear();
		}

		private void OpenLevel()
		{
			string path = EditorUtility.OpenFilePanel("Open Level", "Assets/_MainProject/SODatas/Levels", "asset");
			if (string.IsNullOrEmpty(path)) return;

			path = "Assets" + path.Substring(Application.dataPath.Length);
			mCurrentLevel = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            mCurrentLevelClone = mCurrentLevel.Clone();

            if (mSelectedTiles != null) mSelectedTiles.Clear();
			if (mUndoHistory != null) mUndoHistory.Clear();
			if (mRedoHistory != null) mRedoHistory.Clear();
		}

		private void SaveCurrentLevel()
		{
			if (mCurrentLevelClone == null) return;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(mCurrentLevelClone), mCurrentLevel);
            EditorUtility.SetDirty(mCurrentLevel);
			AssetDatabase.SaveAssets();
		}
		private void CopyLevel()
		{
			if (mCurrentLevelClone == null) return;

			mCopiedLevel = mCurrentLevelClone;
        }
		private void PasteLevel()
		{
			if(mCopiedLevel == null) return;

			mCurrentLevelClone = mCopiedLevel;

            if (mSelectedTiles != null) mSelectedTiles.Clear();
            if (mUndoHistory != null) mUndoHistory.Clear();
            if (mRedoHistory != null) mRedoHistory.Clear();
        }
		#endregion

		#region Undo/Redo

		private void RecordUndo(string actionName)
		{
			if (mCurrentLevelClone == null || mCurrentLevelClone.layerList == null) return;
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();

			LevelSnapshot snapshot = new LevelSnapshot
			{
				actionName = actionName,
				layerListState = mCurrentLevelClone.layerList.Select(layer => new LayerDataWrapper
				{
					tilePlacementList = layer.tilePlacementList.Select(t => new TilePlacement
					{
						gridX = t.gridX,
						gridY = t.gridY,
						tileTypeId = t.tileTypeId,
						isLocked = t.isLocked,
						isFrozen = t.isFrozen,
						frozenCount = t.frozenCount
					}).ToList()
				}).ToList()
			};

			mUndoHistory.Add(snapshot);
			if (mUndoHistory.Count > MAX_HISTORY_SIZE)
				mUndoHistory.RemoveAt(0);

			mRedoHistory.Clear();
		}

		private void Undo()
		{
			if (mUndoHistory == null || mUndoHistory.Count == 0 || mCurrentLevelClone == null) return;
			if (mRedoHistory == null) mRedoHistory = new List<LevelSnapshot>();

			// Save current state to redo
			mRedoHistory.Add(new LevelSnapshot
			{
				layerListState = new List<LayerDataWrapper>(mCurrentLevelClone.layerList ?? new List<LayerDataWrapper>())
			});

			LevelSnapshot prev = mUndoHistory[mUndoHistory.Count - 1];
			mUndoHistory.RemoveAt(mUndoHistory.Count - 1);

			mCurrentLevelClone.layerList = new List<LayerDataWrapper>(prev.layerListState);
			if (mSelectedTiles != null) mSelectedTiles.Clear();

			Repaint();
		}

		private void Redo()
		{
			if (mRedoHistory == null || mRedoHistory.Count == 0 || mCurrentLevelClone == null) return;
			if (mUndoHistory == null) mUndoHistory = new List<LevelSnapshot>();

			// Save current state to undo
			mUndoHistory.Add(new LevelSnapshot
			{
				layerListState = new List<LayerDataWrapper>(mCurrentLevelClone.layerList ?? new List<LayerDataWrapper>())
			});

			LevelSnapshot next = mRedoHistory[mRedoHistory.Count - 1];
			mRedoHistory.RemoveAt(mRedoHistory.Count - 1);

			mCurrentLevelClone.layerList = new List<LayerDataWrapper>(next.layerListState);
			if (mSelectedTiles != null) mSelectedTiles.Clear();

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
				if (mCurrentLevelClone == null || layer < mCurrentLevelClone.maxLayers)
					mCurrentLayer = layer;
				e.Use();
			}
			else if (e.keyCode == KeyCode.C && (e.control || e.command))
			{
				CopyLevel();
				e.Use();
			}
			else if (e.keyCode == KeyCode.V && (e.control || e.command))
			{
				PasteLevel();
				e.Use();
			}
		}

		#endregion
	}
}
#endif
