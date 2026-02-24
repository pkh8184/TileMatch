#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TileMatch.LevelEditor
{
    /// <summary>
    /// 레벨 프리뷰 윈도우 - 3D 시점에서 레벨 미리보기
    /// </summary>
    public class LevelPreviewWindow : EditorWindow
    {
        private LevelData mLevelData;
        private Vector2 mRotationAngle = new Vector2(30F, -45F);
        private float mZoom = 5F;
        private Vector3 mPivotPoint = Vector3.zero;

        private bool mIsDragging = false;
        private Vector2 mLastMousePos;

        private PreviewRenderUtility mPreviewRenderUtility;
        private Material mTileMaterial;
        private Mesh mTileMesh;

        // 색상 설정
        private static readonly Color[] LayerColors = new Color[]
        {
            new Color(0.3F, 0.6F, 1F),
            new Color(0.3F, 0.9F, 0.5F),
            new Color(1F, 0.9F, 0.3F),
            new Color(1F, 0.5F, 0.5F),
            new Color(0.9F, 0.5F, 1F),
        };

        [MenuItem("Tools/Tile Match/Level Preview")]
        public static void OpenWindow()
        {
            LevelPreviewWindow window = GetWindow<LevelPreviewWindow>();
            window.titleContent = new GUIContent("Level Preview", EditorGUIUtility.IconContent("d_ViewToolOrbit").image);
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        public static void OpenWithLevel(LevelData level)
        {
            LevelPreviewWindow window = GetWindow<LevelPreviewWindow>();
            window.mLevelData = level;
            window.titleContent = new GUIContent("Level Preview");
            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            mPreviewRenderUtility = new PreviewRenderUtility();
            mPreviewRenderUtility.camera.fieldOfView = 30F;
            mPreviewRenderUtility.camera.nearClipPlane = 0.1F;
            mPreviewRenderUtility.camera.farClipPlane = 100F;
            mPreviewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            mPreviewRenderUtility.camera.backgroundColor = new Color(0.15F, 0.15F, 0.18F);

            mTileMesh = CreateTileMesh();
            mTileMaterial = new Material(Shader.Find("Standard"));
        }

        private void OnDisable()
        {
            if (mPreviewRenderUtility != null)
            {
                mPreviewRenderUtility.Cleanup();
                mPreviewRenderUtility = null;
            }

            if (mTileMesh != null)
                DestroyImmediate(mTileMesh);

            if (mTileMaterial != null)
                DestroyImmediate(mTileMaterial);
        }

        private void OnGUI()
        {
            DrawToolbar();

            Rect previewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (mLevelData != null)
            {
                Draw3DPreview(previewRect);
                HandleInput(previewRect);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.15F, 0.15F, 0.18F));
                GUI.Label(previewRect, "Drag a Level asset here or select from the editor",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            }

            HandleDragAndDrop(previewRect);
            DrawInfo();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                mLevelData = (LevelData)EditorGUILayout.ObjectField(mLevelData, typeof(LevelData), false, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck())
                {
                    ResetView();
                }

                GUILayout.Space(20);

                if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ResetView();
                }

                GUILayout.FlexibleSpace();

                GUILayout.Label($"Zoom: {mZoom:F1}", EditorStyles.toolbarButton, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Draw3DPreview(Rect rect)
        {
            if (mPreviewRenderUtility == null || mLevelData == null)
                return;

            mPreviewRenderUtility.BeginPreview(rect, GUIStyle.none);

            Quaternion rotation = Quaternion.Euler(mRotationAngle.x, mRotationAngle.y, 0);
            Vector3 cameraPos = rotation * new Vector3(0, 0, -mZoom) + mPivotPoint;
            mPreviewRenderUtility.camera.transform.position = cameraPos;
            mPreviewRenderUtility.camera.transform.LookAt(mPivotPoint);

            mPreviewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
            mPreviewRenderUtility.lights[0].intensity = 1F;

            DrawGridFloor();
            DrawTiles();

            mPreviewRenderUtility.camera.Render();

            Texture resultRender = mPreviewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, resultRender, ScaleMode.StretchToFill, false);
        }

        private void DrawGridFloor()
        {
            float width = mLevelData.boardWidth;
            float height = mLevelData.boardHeight;
            float offsetX = -width / 2F;
            float offsetZ = -height / 2F;

            Handles.color = new Color(0.4F, 0.4F, 0.4F, 0.5F);

            for (int x = 0; x <= mLevelData.boardWidth; x++)
            {
                Vector3 start = new Vector3(offsetX + x, 0, offsetZ);
                Vector3 end = new Vector3(offsetX + x, 0, offsetZ + height);
                Handles.DrawLine(start, end);
            }

            for (int z = 0; z <= mLevelData.boardHeight; z++)
            {
                Vector3 start = new Vector3(offsetX, 0, offsetZ + z);
                Vector3 end = new Vector3(offsetX + width, 0, offsetZ + z);
                Handles.DrawLine(start, end);
            }
        }

        private void DrawTiles()
        {
            if (mLevelData.tilePlacements == null)
                return;

            float offsetX = -mLevelData.boardWidth / 2F + 0.5F;
            float offsetZ = -mLevelData.boardHeight / 2F + 0.5F;
            float layerHeight = 0.3F;

            foreach (TilePlacement tile in mLevelData.tilePlacements)
            {
                Vector3 position = new Vector3(
                    offsetX + tile.gridX,
                    tile.layer * layerHeight + 0.15F,
                    offsetZ + tile.gridY
                );

                Color tileColor = LayerColors[Mathf.Min(tile.layer, LayerColors.Length - 1)];

                if (tile.isFrozen)
                    tileColor = Color.Lerp(tileColor, Color.cyan, 0.5F);
                if (tile.isLocked)
                    tileColor = Color.Lerp(tileColor, Color.gray, 0.5F);

                mTileMaterial.color = tileColor;

                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(0.9F, 0.2F, 0.9F));
                Graphics.DrawMesh(mTileMesh, matrix, mTileMaterial, 0, mPreviewRenderUtility.camera);
            }
        }

        private void HandleInput(Rect rect)
        {
            Event e = Event.current;

            if (!rect.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0 || e.button == 1)
                    {
                        mIsDragging = true;
                        mLastMousePos = e.mousePosition;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (mIsDragging)
                    {
                        Vector2 delta = e.mousePosition - mLastMousePos;

                        if (e.button == 0)
                        {
                            mRotationAngle.y += delta.x * 0.5F;
                            mRotationAngle.x += delta.y * 0.5F;
                            mRotationAngle.x = Mathf.Clamp(mRotationAngle.x, -89F, 89F);
                        }
                        else if (e.button == 1)
                        {
                            Quaternion rotation = Quaternion.Euler(mRotationAngle.x, mRotationAngle.y, 0);
                            Vector3 right = rotation * Vector3.right;
                            Vector3 up = rotation * Vector3.up;
                            mPivotPoint -= (right * delta.x + up * delta.y) * 0.01F * mZoom;
                        }

                        mLastMousePos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    mIsDragging = false;
                    e.Use();
                    break;

                case EventType.ScrollWheel:
                    mZoom += e.delta.y * 0.3F;
                    mZoom = Mathf.Clamp(mZoom, 2F, 20F);
                    Repaint();
                    e.Use();
                    break;
            }
        }

        private void HandleDragAndDrop(Rect rect)
        {
            Event e = Event.current;

            if (!rect.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.Length > 0 &&
                        DragAndDrop.objectReferences[0] is LevelData)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            mLevelData = DragAndDrop.objectReferences[0] as LevelData;
                            ResetView();
                        }

                        e.Use();
                    }
                    break;
            }
        }

        private void DrawInfo()
        {
            if (mLevelData == null) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label($"Level: {mLevelData.levelName}", EditorStyles.miniLabel);
                GUILayout.Label($"Size: {mLevelData.boardWidth}x{mLevelData.boardHeight}", EditorStyles.miniLabel);
                GUILayout.Label($"Layers: {mLevelData.maxLayers}", EditorStyles.miniLabel);
                GUILayout.Label($"Tiles: {mLevelData.tilePlacements?.Count ?? 0}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("LMB: Rotate | RMB: Pan | Scroll: Zoom", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ResetView()
        {
            mRotationAngle = new Vector2(30F, -45F);
            mZoom = 5F;
            mPivotPoint = Vector3.zero;
            Repaint();
        }

        private Mesh CreateTileMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[]
            {
                // Front
                new Vector3(-0.5F, -0.5F, 0.5F),
                new Vector3(0.5F, -0.5F, 0.5F),
                new Vector3(0.5F, 0.5F, 0.5F),
                new Vector3(-0.5F, 0.5F, 0.5F),
                // Back
                new Vector3(0.5F, -0.5F, -0.5F),
                new Vector3(-0.5F, -0.5F, -0.5F),
                new Vector3(-0.5F, 0.5F, -0.5F),
                new Vector3(0.5F, 0.5F, -0.5F),
                // Top
                new Vector3(-0.5F, 0.5F, 0.5F),
                new Vector3(0.5F, 0.5F, 0.5F),
                new Vector3(0.5F, 0.5F, -0.5F),
                new Vector3(-0.5F, 0.5F, -0.5F),
                // Bottom
                new Vector3(-0.5F, -0.5F, -0.5F),
                new Vector3(0.5F, -0.5F, -0.5F),
                new Vector3(0.5F, -0.5F, 0.5F),
                new Vector3(-0.5F, -0.5F, 0.5F),
                // Left
                new Vector3(-0.5F, -0.5F, -0.5F),
                new Vector3(-0.5F, -0.5F, 0.5F),
                new Vector3(-0.5F, 0.5F, 0.5F),
                new Vector3(-0.5F, 0.5F, -0.5F),
                // Right
                new Vector3(0.5F, -0.5F, 0.5F),
                new Vector3(0.5F, -0.5F, -0.5F),
                new Vector3(0.5F, 0.5F, -0.5F),
                new Vector3(0.5F, 0.5F, 0.5F),
            };

            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22,
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
#endif
