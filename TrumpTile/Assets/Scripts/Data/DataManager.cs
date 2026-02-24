using UnityEngine;

namespace TrumpTile.Data
{
    /// <summary>
    /// 데이터 테이블 관리자
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        [Header("Data Tables")]
        [SerializeField] private StageTable mStageTable;
        [SerializeField] private ItemTable mItemTable;

        // 테이블 접근자
        public StageTable StageTable => mStageTable;
        public ItemTable ItemTable => mItemTable;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadTables();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 테이블 로드
        /// </summary>
        private void LoadTables()
        {
            // Inspector에서 할당되지 않은 경우 Resources에서 로드
            if (mStageTable == null)
            {
                mStageTable = Resources.Load<StageTable>("Data/StageTable");
                if (mStageTable == null)
                    Debug.LogWarning("[DataManager] StageTable not found!");
            }

            if (mItemTable == null)
            {
                mItemTable = Resources.Load<ItemTable>("Data/ItemTable");
                if (mItemTable == null)
                    Debug.LogWarning("[DataManager] ItemTable not found!");
            }

            Debug.Log($"[DataManager] Tables loaded - Stages: {mStageTable?.TotalStageCount ?? 0}");
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
