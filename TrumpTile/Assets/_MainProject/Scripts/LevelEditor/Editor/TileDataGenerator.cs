#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using TrumpTile.Core;

namespace TileMatch
{
    public class TileDataGenerator : EditorWindow
    {
        private string mSpriteFolderPath = "Assets/Texture/Tile";
        private string mOutputFolderPath = "Assets/Data/TileData";

        // 파일명 패턴 설정
        private string mSpadeName = "Spade";
        private string mHeartName = "Heart";
        private string mDiamondName = "Diamond";
        private string mClubName = "Clover"; // 파일명이 Clover로 되어있으므로

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
                "스프라이트 폴더의 이미지를 기반으로 52개의 TileData를 자동 생성합니다.\n" +
                "파일명 형식: Tile_[무늬]_[001-013]",
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
            mSpadeName = EditorGUILayout.TextField("Spade (♠)", mSpadeName);
            mHeartName = EditorGUILayout.TextField("Heart (♥)", mHeartName);
            mDiamondName = EditorGUILayout.TextField("Diamond (♦)", mDiamondName);
            mClubName = EditorGUILayout.TextField("Club (♣)", mClubName);

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                $"예상 파일명:\n" +
                $"  Tile_{mSpadeName}_001.png ~ Tile_{mSpadeName}_013.png\n" +
                $"  Tile_{mHeartName}_001.png ~ Tile_{mHeartName}_013.png\n" +
                $"  Tile_{mDiamondName}_001.png ~ Tile_{mDiamondName}_013.png\n" +
                $"  Tile_{mClubName}_001.png ~ Tile_{mClubName}_013.png",
                MessageType.None);

            EditorGUILayout.Space(20);

            // 생성 버튼
            GUI.backgroundColor = new Color(0.3F, 0.8F, 0.3F);
            if (GUILayout.Button("🎴 Generate 52 Tile Data", GUILayout.Height(40)))
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

            // 4개 무늬
            (ECardSuit suit, string fileName)[] suits = new (ECardSuit suit, string fileName)[]
            {
                (ECardSuit.Spade, mSpadeName),
                (ECardSuit.Heart, mHeartName),
                (ECardSuit.Diamond, mDiamondName),
                (ECardSuit.Club, mClubName)
            };

            // 13개 숫자
            ECardRank[] ranks = new ECardRank[]
            {
                ECardRank.Ace, ECardRank.Two, ECardRank.Three, ECardRank.Four,
                ECardRank.Five, ECardRank.Six, ECardRank.Seven, ECardRank.Eight,
                ECardRank.Nine, ECardRank.Ten, ECardRank.Jack, ECardRank.Queen, ECardRank.King
            };

            foreach ((ECardSuit suit, string fileName) suitEntry in suits)
            {
                for (int i = 0; i < ranks.Length; i++)
                {
                    ECardRank rank = ranks[i];
                    int num = i + 1; // 001 ~ 013

                    // 스프라이트 찾기
                    string spritePath = $"{mSpriteFolderPath}/Tile_{suitEntry.fileName}_{num:D3}.png";
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                    if (sprite == null)
                    {
                        // .png 대신 다른 확장자 시도
                        spritePath = $"{mSpriteFolderPath}/Tile_{suitEntry.fileName}_{num:D3}.jpg";
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    }

                    if (sprite == null)
                    {
                        Debug.LogWarning($"Sprite not found: Tile_{suitEntry.fileName}_{num:D3}");
                        failed++;
                        continue;
                    }

                    // TileData 생성
                    TileData tileData = ScriptableObject.CreateInstance<TileData>();
                    tileData.suit = suitEntry.suit;
                    tileData.rank = rank;
                    tileData.sprite = sprite;

                    // 저장
                    string assetPath = $"{mOutputFolderPath}/{suitEntry.suit}_{rank}.asset";
                    AssetDatabase.CreateAsset(tileData, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (failed == 0)
            {
                EditorUtility.DisplayDialog("Complete",
                    $"✓ {created}개의 TileData가 생성되었습니다!\n\n" +
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

            string[] suitNames = { mSpadeName, mHeartName, mDiamondName, mClubName };

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
