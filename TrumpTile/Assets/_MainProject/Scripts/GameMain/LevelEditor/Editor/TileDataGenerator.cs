#if UNITY_EDITOR
using System.IO;
using TrumpTile.GameMain.Core;
using UnityEditor;
using UnityEngine;
using static PlasticPipe.Server.MonitorStats;

namespace TrumpTile.LevelEditor.Editor
{
	public class TileDataGenerator : EditorWindow
	{
		private string mSpriteFolderPath = "Assets/_MainProject/Textures/UI/Sprite/Tile";
		private string mOutputFolderPath = "Assets/_MainProject/SODatas/TileData";

		// 파일명 패턴 설정
		private string mDesertName = "Desert";
		private string mFruitName = "Fruit";
		private string mInteriorName = "Interior";
		private string mToolsName = "Tools";
		private string mFoodsName = "Foods";
		private string mToysName = "Toys";
		private string mBallsName = "Balls";

		[MenuItem("Tools/Tile Match/Generate Tile Data")]
		public static void OpenWindow()
		{
			TileDataGenerator window = GetWindow<TileDataGenerator>();
			window.titleContent = new GUIContent("Tile Data Generator");
			window.minSize = new Vector2(400, 300);
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("🃏 Tile Data Generator", EditorStyles.boldLabel);
			EditorGUILayout.Space(10);

			EditorGUILayout.HelpBox(
				"스프라이트 폴더의 이미지를 기반으로 TileData를 자동 생성합니다.\n" +
				"파일명 형식: Tile_[카테고리]_[넘버]",
				MessageType.Info);

			EditorGUILayout.Space(10);

			// 경로 설정
			EditorGUILayout.LabelField("📁 Paths", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			mSpriteFolderPath = EditorGUILayout.TextField("Sprite Folder", mSpriteFolderPath);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string path = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");
				if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
				{
					mSpriteFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			mOutputFolderPath = EditorGUILayout.TextField("Output Folder", mOutputFolderPath);
			if (GUILayout.Button("...", GUILayout.Width(30)))
			{
				string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
				if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
				{
					mOutputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(10);

			// 파일명 설정
			EditorGUILayout.LabelField("📝 File Name Patterns", EditorStyles.boldLabel);
			mDesertName = EditorGUILayout.TextField("Desert", mDesertName);
			mFruitName = EditorGUILayout.TextField("Fruit", mFruitName);
			mInteriorName = EditorGUILayout.TextField("Interior", mInteriorName);
			mToolsName = EditorGUILayout.TextField("Tools", mToolsName);
			mFoodsName = EditorGUILayout.TextField("Foods", mFoodsName);
			mToysName = EditorGUILayout.TextField("Toys", mToysName);
			mBallsName = EditorGUILayout.TextField("Balls", mBallsName);

			EditorGUILayout.Space(5);
			EditorGUILayout.HelpBox(
				$"예상 파일명:\n" +
				$"  Tile_{mDesertName}_000.png\n" +
				$"  Tile_{mFruitName}_000.png\n" +
				$"  Tile_{mInteriorName}_000.png\n" +
				$"  Tile_{mToolsName}_000.png\n" +
				$"  Tile_{mFoodsName}_000.png\n" +
				$"  Tile_{mToysName}_000.png\n" +
				$"  Tile_{mBallsName}_000.png",
				MessageType.None);

			EditorGUILayout.Space(20);

			// 생성 버튼
			GUI.backgroundColor = new Color(0.3F, 0.8F, 0.3F);
			if (GUILayout.Button("Generate Tile Data", GUILayout.Height(40)))
			{
				GenerateTileData();
			}
			GUI.backgroundColor = Color.white;

			EditorGUILayout.Space(10);

			// 미리보기
			if (GUILayout.Button("Preview (Check Sprites)"))
			{
				PreviewSprites();
			}
		}

		private void GenerateTileData()
		{
			// 출력 폴더 생성
			if (!AssetDatabase.IsValidFolder(mOutputFolderPath))
			{
				string[] folders = mOutputFolderPath.Split('/');
				string currentPath = folders[0];

				for (int i = 1; i < folders.Length; i++)
				{
					string newPath = currentPath + "/" + folders[i];
					if (!AssetDatabase.IsValidFolder(newPath))
					{
						AssetDatabase.CreateFolder(currentPath, folders[i]);
					}
					currentPath = newPath;
				}
			}

			int created = 0;
			int failed = 0;
			int exit = 0;
			for(int i = 0; i < (int)ETileCartegory.Length; i++)
			{
				string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { $"{mSpriteFolderPath}/{(ETileCartegory)i}" });
				string outputPath = $"{mOutputFolderPath}/{(ETileCartegory)i}";

                if (!AssetDatabase.IsValidFolder(outputPath))
                {
                    AssetDatabase.CreateFolder(mOutputFolderPath, $"{(ETileCartegory)i}");
                }

                int number = 1;

                foreach (string guid in guids)
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

					if(sprite == null)
					{
						failed++;
						continue;
					}
					string assetPath = $"{outputPath}/{(ETileCartegory)i}_{number}.asset";

                    if (AssetDatabase.LoadAssetAtPath<TileData>(assetPath) != null)
					{			
						exit++;	
						continue;
					}
					TileData tileData = ScriptableObject.CreateInstance<TileData>();

					tileData.tileTypeId = $"{(ETileCartegory)i}_{number}";
					tileData.displayName = tileData.tileTypeId;
					tileData.tileCartegory = (ETileCartegory)i;
                    tileData.sprite = sprite;

                    AssetDatabase.CreateAsset(tileData, assetPath);
                    created++;
					number++;
                }

			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			if (failed == 0)
			{
				EditorUtility.DisplayDialog("Complete",
					$"✓ {created}개의 TileData가 생성되었습니다!(이미 존재하던 {exit}개 제외)\n\n" +
					$"위치: {mOutputFolderPath}", "OK");
			}
			else
			{
				EditorUtility.DisplayDialog("Complete with Warnings",
					$"생성: {created}개\n실패: {failed}개\n\n" +
					$"Console에서 누락된 스프라이트를 확인하세요.", "OK");
			}
		}

		private void PreviewSprites()
		{
			int found = 0;
			int missing = 0;

			string[] suitNames = { mDesertName, mFruitName, mInteriorName, mToolsName };

			foreach (string suitName in suitNames)
			{
				for (int i = 1; i <= 13; i++)
				{
					string spritePath = $"{mSpriteFolderPath}/Tile_{suitName}_{i:D3}.png";
					Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

					if (sprite != null)
					{
						found++;
						Debug.Log($"✓ Found: {spritePath}");
					}
					else
					{
						missing++;
						Debug.LogWarning($"✗ Missing: {spritePath}");
					}
				}
			}

			EditorUtility.DisplayDialog("Preview Result",
				$"찾음: {found}개\n누락: {missing}개\n\n" +
				$"자세한 내용은 Console을 확인하세요.", "OK");
		}
	}
}
#endif
