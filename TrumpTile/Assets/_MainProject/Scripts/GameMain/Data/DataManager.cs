using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using TrumpTile.LevelEditor;

namespace TrumpTile.GameMain.Data
{
    /// <summary>
    /// 데이터 테이블 관리자
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public const string STAGE_TABLE_ADDRESS = "StageTable";
        public const string ITEM_TABLE_ADDRESS = "ItemTable";
        public const string LEVEL_ADDRESS_FORMAT = "Level_{0:D3}";

        public static DataManager Instance { get; private set; }

        private StageTable mStageTable;
        private ItemTable mItemTable;
        private LevelData mCurrentLevelData;

        private AsyncOperationHandle<StageTable> mStageTableHandle;
        private AsyncOperationHandle<ItemTable> mItemTableHandle;
        private AsyncOperationHandle<LevelData> mCurrentLevelHandle;

        // 테이블 접근자
        public StageTable StageTable => mStageTable;
        public ItemTable ItemTable => mItemTable;
        public LevelData CurrentLevelData => mCurrentLevelData;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            IsInitialized = true;
        }

        // 20250226 - 예석경 추후 StageTable 과 ItemTable 개발 후 사용 예정
        // private async void Start()
        // {
        //     await LoadTablesAsync();
        // }

        // /// <summary>
        // /// StageTable, ItemTable 비동기 로드 (앱 시작 시 1회)
        // /// </summary>
        // public async Task LoadTablesAsync()
        // {
        //     IsInitialized = false;

        //     mStageTableHandle = Addressables.LoadAssetAsync<StageTable>(STAGE_TABLE_ADDRESS);
        //     mItemTableHandle = Addressables.LoadAssetAsync<ItemTable>(ITEM_TABLE_ADDRESS);

        //     await Task.WhenAll(mStageTableHandle.Task, mItemTableHandle.Task);

        //     if (mStageTableHandle.Status == AsyncOperationStatus.Succeeded)
        //         mStageTable = mStageTableHandle.Result;
        //     else
        //         Debug.LogWarning("[DataManager] StageTable load failed!");

        //     if (mItemTableHandle.Status == AsyncOperationStatus.Succeeded)
        //         mItemTable = mItemTableHandle.Result;
        //     else
        //         Debug.LogWarning("[DataManager] ItemTable load failed!");

        //     IsInitialized = true;
        //     Debug.Log($"[DataManager] Tables loaded - Stages: {mStageTable?.TotalStageCount ?? 0}");
        // }

        /// <summary>
        /// 레벨 데이터 비동기 로드 (이전 레벨 자동 해제)
        /// </summary>
        public async Task<LevelData> LoadLevelAsync(int levelNumber)
        {
            if (mCurrentLevelHandle.IsValid())
            {
                Addressables.Release(mCurrentLevelHandle);
                mCurrentLevelData = null;
            }

            string address = string.Format(LEVEL_ADDRESS_FORMAT, levelNumber);
            mCurrentLevelHandle = Addressables.LoadAssetAsync<LevelData>(address);
            await mCurrentLevelHandle.Task;

            if (mCurrentLevelHandle.Status == AsyncOperationStatus.Succeeded)
            {
                mCurrentLevelData = mCurrentLevelHandle.Result;
                return mCurrentLevelData;
            }

            Debug.LogWarning($"[DataManager] LevelData load failed: {address}");
            return null;
        }

        /// <summary>
        /// 현재 레벨 데이터 해제
        /// </summary>
        public void ReleaseLevelData()
        {
            if (mCurrentLevelHandle.IsValid())
            {
                Addressables.Release(mCurrentLevelHandle);
                mCurrentLevelData = null;
            }
        }

        private void OnDestroy()
        {
            ReleaseLevelData();
            if (mStageTableHandle.IsValid()) Addressables.Release(mStageTableHandle);
            if (mItemTableHandle.IsValid()) Addressables.Release(mItemTableHandle);
        }

        #region Stage Data Access

        /// <summary>
        /// 스테이지 데이터 가져오기
        /// </summary>
        public StageData GetStage(int stageId)
        {
            return mStageTable?.GetStageById(stageId);
        }

        /// <summary>
        /// 레벨로 스테이지 데이터 가져오기
        /// </summary>
        public StageData GetStageByLevel(int level)
        {
            return mStageTable?.GetStageByLevel(level);
        }

        /// <summary>
        /// 총 스테이지 수
        /// </summary>
        public int TotalStages => mStageTable?.TotalStageCount ?? 0;

        #endregion

        #region Item Data Access

        /// <summary>
        /// 아이템 데이터 가져오기
        /// </summary>
        public ItemData GetItem(int itemId)
        {
            return mItemTable?.GetItemById(itemId);
        }

        /// <summary>
        /// 아이템 타입으로 데이터 가져오기
        /// </summary>
        public ItemData GetItemByType(EItemType type)
        {
            return mItemTable?.GetItemByType(type);
        }

        #endregion
    }
}
