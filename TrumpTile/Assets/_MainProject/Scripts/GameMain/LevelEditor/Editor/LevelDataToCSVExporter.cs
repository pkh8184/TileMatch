#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

namespace TrumpTile.LevelEditor.Editor
{
    public class LevelDataToCSVExporter : EditorWindow
    {
        [MenuItem("Tools/Export Level Data to CSV")]
        private static void Export()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/_MainProject/SODatas/Levels" });
            
            var sb = new StringBuilder();
            sb.AppendLine("LevelNumber,TotalTileCount");
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                
                int levelNumber = level.levelNumber;
                int tileCount = 0;
                foreach(var list in level.layerList)
                {
                    tileCount += list.tilePlacementList.Count;
                }
                sb.AppendLine($"{levelNumber},{tileCount}");
            }
            
            string savePath = EditorUtility.SaveFilePanel("Save LevelData To CSV", "", "AllLevelsData","csv");
            if(string.IsNullOrEmpty(savePath))
            {
                return;
            }
            File.WriteAllText(savePath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("레벨 데이터 CSV 내보내기 완료");
        }
    }   
}
#endif