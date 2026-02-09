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
        [SerializeField] private StageTable stageTable;
        [SerializeField] private ItemTable itemTable;

        // 테이블 접근자
        public StageTable StageTable => stageTable;
        public ItemTable ItemTable => itemTable;

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
            if (stageTable == null)
            {
                stageTable = Resources.Load<StageTable>("Data/StageTable");
                if (stageTable == null)
                    Debug.LogWarning("[DataManager] StageTable not found!");
            }

            if (itemTable == null)
            {
                itemTable = Resources.Load<ItemTable>("Data/ItemTable");
                if (itemTable == null)
                    Debug.LogWarning("[DataManager] ItemTable not found!");
            }

            Debug.Log($"[DataManager] Tables loaded - Stages: {stageTable?.TotalStageCount ?? 0}");
        }

        #region Stage Data Access

        /// <summary>
        /// 스테이지 데이터 가져오기
        /// </summary>
        public StageData GetStage(int stageId)
        {
            return stageTable?.GetStageById(stageId);
        }

        /// <summary>
        /// 레벨로 스테이지 데이터 가져오기
        /// </summary>
        public StageData GetStageByLevel(int level)
        {
            return stageTable?.GetStageByLevel(level);
        }

        /// <summary>
        /// 총 스테이지 수
        /// </summary>
        public int TotalStages => stageTable?.TotalStageCount ?? 0;

        #endregion

        #region Item Data Access

        /// <summary>
        /// 아이템 데이터 가져오기
        /// </summary>
        public ItemData GetItem(int itemId)
        {
            return itemTable?.GetItemById(itemId);
        }

        /// <summary>
        /// 아이템 타입으로 데이터 가져오기
        /// </summary>
        public ItemData GetItemByType(ItemType type)
        {
            return itemTable?.GetItemByType(type);
        }

        #endregion
    }
}
