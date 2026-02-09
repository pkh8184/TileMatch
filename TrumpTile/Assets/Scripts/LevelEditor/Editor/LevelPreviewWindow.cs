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
        private LevelData levelData;
        private Vector2 rotationAngle = new Vector2(30f, -45f);
        private float zoom = 5f;
        private Vector3 pivotPoint = Vector3.zero;
        
        private bool isDragging = false;
        private Vector2 lastMousePos;
        
        private PreviewRenderUtility previewRenderUtility;
        private Material tileMaterial;
        private Mesh tileMesh;
        
        // 색상 설정
        private static readonly Color[] LayerColors = new Color[]
        {
            new Color(0.3f, 0.6f, 1f),
            new Color(0.3f, 0.9f, 0.5f),
            new Color(1f, 0.9f, 0.3f),
            new Color(1f, 0.5f, 0.5f),
            new Color(0.9f, 0.5f, 1f),
        };
        
        [MenuItem("Tools/Tile Match/Level Preview")]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelPreviewWindow>();
            window.titleContent = new GUIContent("Level Preview", EditorGUIUtility.IconContent("d_ViewToolOrbit").image);
            window.minSize = new Vector2(400, 400);
            window.Show();
        }
        
        public static void OpenWithLevel(LevelData level)
        {
            var window = GetWindow<LevelPreviewWindow>();
            window.levelData = level;
            window.titleContent = new GUIContent("Level Preview");
            window.Show();
            window.Repaint();
        }
        
        private void OnEnable()
        {
            previewRenderUtility = new PreviewRenderUtility();
            previewRenderUtility.camera.fieldOfView = 30f;
            previewRenderUtility.camera.nearClipPlane = 0.1f;
            previewRenderUtility.camera.farClipPlane = 100f;
            previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
            previewRenderUtility.camera.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            
            tileMesh = CreateTileMesh();
            tileMaterial = new Material(Shader.Find("Standard"));
        }
        
        private void OnDisable()
        {
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }
            
            if (tileMesh != null)
                DestroyImmediate(tileMesh);
            
            if (tileMaterial != null)
                DestroyImmediate(tileMaterial);
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            Rect previewRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
            if (levelData != null)
            {
                Draw3DPreview(previewRect);
                HandleInput(previewRect);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.18f));
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
                levelData = (LevelData)EditorGUILayout.ObjectField(levelData, typeof(LevelData), false, GUILayout.Width(200));
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
                
                GUILayout.Label($"Zoom: {zoom:F1}", EditorStyles.toolbarButton, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void Draw3DPreview(Rect rect)
        {
            if (previewRenderUtility == null || levelData == null)
                return;
            
            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            
            Quaternion rotation = Quaternion.Euler(rotationAngle.x, rotationAngle.y, 0);
            Vector3 cameraPos = rotation * new Vector3(0, 0, -zoom) + pivotPoint;
            previewRenderUtility.camera.transform.position = cameraPos;
            previewRenderUtility.camera.transform.LookAt(pivotPoint);
            
            previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
            previewRenderUtility.lights[0].intensity = 1f;
            
            DrawGridFloor();
            DrawTiles();
            
            previewRenderUtility.camera.Render();
            
            Texture resultRender = previewRenderUtility.EndPreview();
            GUI.DrawTexture(rect, resultRender, ScaleMode.StretchToFill, false);
        }
        
        private void DrawGridFloor()
        {
            float width = levelData.boardWidth;
            float height = levelData.boardHeight;
            float offsetX = -width / 2f;
            float offsetZ = -height / 2f;
            
            Handles.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            
            for (int x = 0; x <= levelData.boardWidth; x++)
            {
                Vector3 start = new Vector3(offsetX + x, 0, offsetZ);
                Vector3 end = new Vector3(offsetX + x, 0, offsetZ + height);
                Handles.DrawLine(start, end);
            }
            
            for (int z = 0; z <= levelData.boardHeight; z++)
            {
                Vector3 start = new Vector3(offsetX, 0, offsetZ + z);
                Vector3 end = new Vector3(offsetX + width, 0, offsetZ + z);
                Handles.DrawLine(start, end);
            }
        }
        
        private void DrawTiles()
        {
            if (levelData.tilePlacements == null)
                return;
            
            float offsetX = -levelData.boardWidth / 2f + 0.5f;
            float offsetZ = -levelData.boardHeight / 2f + 0.5f;
            float layerHeight = 0.3f;
            
            foreach (var tile in levelData.tilePlacements)
            {
                Vector3 position = new Vector3(
                    offsetX + tile.gridX,
                    tile.layer * layerHeight + 0.15f,
                    offsetZ + tile.gridY
                );
                
                Color tileColor = LayerColors[Mathf.Min(tile.layer, LayerColors.Length - 1)];
                
                if (tile.isFrozen)
                    tileColor = Color.Lerp(tileColor, Color.cyan, 0.5f);
                if (tile.isLocked)
                    tileColor = Color.Lerp(tileColor, Color.gray, 0.5f);
                
                tileMaterial.color = tileColor;
                
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, new Vector3(0.9f, 0.2f, 0.9f));
                Graphics.DrawMesh(tileMesh, matrix, tileMaterial, 0, previewRenderUtility.camera);
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
                        isDragging = true;
                        lastMousePos = e.mousePosition;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (isDragging)
                    {
                        Vector2 delta = e.mousePosition - lastMousePos;
                        
                        if (e.button == 0)
                        {
                            rotationAngle.y += delta.x * 0.5f;
                            rotationAngle.x += delta.y * 0.5f;
                            rotationAngle.x = Mathf.Clamp(rotationAngle.x, -89f, 89f);
                        }
                        else if (e.button == 1)
                        {
                            Quaternion rotation = Quaternion.Euler(rotationAngle.x, rotationAngle.y, 0);
                            Vector3 right = rotation * Vector3.right;
                            Vector3 up = rotation * Vector3.up;
                            pivotPoint -= (right * delta.x + up * delta.y) * 0.01f * zoom;
                        }
                        
                        lastMousePos = e.mousePosition;
                        Repaint();
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    isDragging = false;
                    e.Use();
                    break;
                    
                case EventType.ScrollWheel:
                    zoom += e.delta.y * 0.3f;
                    zoom = Mathf.Clamp(zoom, 2f, 20f);
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
                            levelData = DragAndDrop.objectReferences[0] as LevelData;
                            ResetView();
                        }
                        
                        e.Use();
                    }
                    break;
            }
        }
        
        private void DrawInfo()
        {
            if (levelData == null) return;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label($"Level: {levelData.levelName}", EditorStyles.miniLabel);
                GUILayout.Label($"Size: {levelData.boardWidth}x{levelData.boardHeight}", EditorStyles.miniLabel);
                GUILayout.Label($"Layers: {levelData.maxLayers}", EditorStyles.miniLabel);
                GUILayout.Label($"Tiles: {levelData.tilePlacements?.Count ?? 0}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("LMB: Rotate | RMB: Pan | Scroll: Zoom", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void ResetView()
        {
            rotationAngle = new Vector2(30f, -45f);
            zoom = 5f;
            pivotPoint = Vector3.zero;
            Repaint();
        }
        
        private Mesh CreateTileMesh()
        {
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                // Front
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                // Back
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                // Top
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                // Bottom
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                // Left
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                // Right
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
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
