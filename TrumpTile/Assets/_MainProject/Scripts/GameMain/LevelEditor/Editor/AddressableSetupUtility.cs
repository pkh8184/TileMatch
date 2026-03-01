#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using TrumpTile.GameMain.Data;
using TrumpTile.LevelEditor;

namespace TrumpTile.LevelEditor.Editor
{
    /// <summary>
    /// Addressable 주소 일괄 설정 유틸리티
    /// </summary>
    public static class AddressableSetupUtility
    {
        private const string GROUP_LEVELS = "Levels";
        private const string GROUP_DATA_TABLES = "DataTables";

        // ----------------------------------------------------------------
        // 메뉴 진입점
        // ----------------------------------------------------------------

        [MenuItem("Tools/Tile Match/Addressables/Setup All (Level + Tables)")]
        public static void SetupAll()
        {
            int levelCount = SetupLevelAssets(silent: true);
            int tableCount = SetupTableAssets(silent: true);

            EditorUtility.DisplayDialog("Addressable Setup Complete",
                $"Levels  : {levelCount}개 등록\n" +
                $"Tables  : {tableCount}개 등록\n\n" +
                "Window > Asset Management > Addressables > Groups 에서 확인하세요.",
                "OK");
        }

        [MenuItem("Tools/Tile Match/Addressables/Setup Level Data Only")]
        public static void SetupLevelsOnly()
        {
            int count = SetupLevelAssets(silent: false);
            EditorUtility.DisplayDialog("Level Setup Complete",
                $"{count}개 LevelData 에셋이 Addressables에 등록되었습니다.\n" +
                $"Address 형식: Level_001 ~ Level_{count:D3}",
                "OK");
        }

        [MenuItem("Tools/Tile Match/Addressables/Setup Data Tables Only")]
        public static void SetupTablesOnly()
        {
            int count = SetupTableAssets(silent: false);
            EditorUtility.DisplayDialog("Table Setup Complete",
                $"{count}개 테이블 에셋이 Addressables에 등록되었습니다.",
                "OK");
        }

        // ----------------------------------------------------------------
        // 내부 구현
        // ----------------------------------------------------------------

        /// <summary>
        /// 모든 LevelData 에셋을 "Levels" 그룹에 등록
        /// Address: Level_001 ~ Level_NNN (levelNumber 기준)
        /// </summary>
        private static int SetupLevelAssets(bool silent)
        {
            AddressableAssetSettings settings = GetOrCreateSettings();
            if (settings == null) return 0;

            AddressableAssetGroup group = GetOrCreateGroup(settings, GROUP_LEVELS);

            string[] guids = AssetDatabase.FindAssets("t:LevelData");
            int count = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (levelData == null) continue;

                string address = $"Level_{levelData.levelNumber:D3}";
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = address;
                count++;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();

            if (!silent)
                Debug.Log($"[AddressableSetup] LevelData {count}개 등록 완료 (Group: {GROUP_LEVELS})");

            return count;
        }

        /// <summary>
        /// StageTable, ItemTable 에셋을 "DataTables" 그룹에 등록
        /// Address: StageTable, ItemTable
        /// </summary>
        private static int SetupTableAssets(bool silent)
        {
            AddressableAssetSettings settings = GetOrCreateSettings();
            if (settings == null) return 0;

            AddressableAssetGroup group = GetOrCreateGroup(settings, GROUP_DATA_TABLES);
            int count = 0;

            count += RegisterTableAsset<StageTable>(settings, group, "StageTable", silent);
            count += RegisterTableAsset<ItemTable>(settings, group, "ItemTable", silent);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();

            return count;
        }

        private static int RegisterTableAsset<T>(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string address,
            bool silent) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                if (!silent)
                    Debug.LogWarning($"[AddressableSetup] {typeof(T).Name} 에셋을 찾을 수 없습니다.");
                return 0;
            }

            // 동일 타입 에셋이 여러 개면 첫 번째만 사용
            string guid = guids[0];
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;

            if (!silent)
                Debug.Log($"[AddressableSetup] {typeof(T).Name} → Address: {address}");

            return 1;
        }

        // ----------------------------------------------------------------
        // 헬퍼
        // ----------------------------------------------------------------

        private static AddressableAssetSettings GetOrCreateSettings()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null) return settings;

            // Addressable Settings가 없으면 생성
            settings = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                true, true);

            AddressableAssetSettingsDefaultObject.Settings = settings;
            Debug.Log("[AddressableSetup] Addressable Settings를 새로 생성했습니다.");
            return settings;
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null) return group;

            group = settings.CreateGroup(
                groupName,
                setAsDefaultGroup: false,
                readOnly: false,
                postEvent: false,
                schemasToCopy: null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));

            Debug.Log($"[AddressableSetup] 그룹 '{groupName}' 생성 완료");
            return group;
        }
    }
}
#endif
