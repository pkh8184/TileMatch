#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
    public static class DataResetor
    {
        [MenuItem("Tools/Reset All Saved Data")]
        public static void ResetData()
        {
            if(EditorUtility.DisplayDialog(
            "데이터 초기화", 
            "모든 PlayerPrefs 데이터를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
            "삭제", 
            "취소"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[DataReset] 모든 PlayerPrefs 데이터가 삭제되었습니다.");
            }
        }
    }   
}
#endif