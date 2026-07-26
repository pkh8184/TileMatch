using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Threading.Tasks;
using System.Linq;
using TrumpTile.GameMain.Data;

namespace  TrumpTile.GameMain.Core
{
    //구매 복원 결과.
    public enum ERestoreResult
    {
        Restored,            //복원됨(광고 제거 소유 확인)
        NothingToRestore,    //온라인이지만 복원할 구매 없음
        NetworkUnavailable,  //네트워크 미연결
        Failed               //스토어 미연결/타임아웃 등 실패
    }

    public class IAPManager : MonoBehaviour
    {
        [SerializeField] private IAPProductDatabase mProductDatabase;
        private static IAPManager instance;
        private StoreController mStoreController;
        //구매복원 버튼 → FetchPurchases 완료(OnPurchasesFetched) 시 결과를 전달할 콜백.
        private System.Action<ERestoreResult> mOnRestoreCompleted;
        //구매복원 응답 지연 대비 타임아웃 코루틴.
        private Coroutine mRestoreTimeoutCo;
        public static IAPManager Instance => instance;
        private async void Awake()
        {
            if(instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
    
                mProductDatabase.Initialize();
                
                await InitializeIAP();
            }
            else
            {
                Destroy(this);
            }     
        }
        public void PurchaseProduct(EProductId eProductId)
        {
            //네트워크 미연결 시 NETWORK_NOT_CONNECT 이벤트를 발생시키고 구매를 시도하지 않는다.
            if(!NetworkUtil.EnsureConnected())
            {
                return;
            }
            if(mStoreController == null)
            {
                return;
            }
            string id = mProductDatabase.GetProductId(eProductId);
            mStoreController.PurchaseProduct(id);
        }
        /// <summary>
        /// 구매 복원. 스토어(구글 플레이)에 소유 중인 구매 목록을 다시 조회한다.
        /// 결과는 OnPurchasesFetched에서 처리되며, 광고 제거 소유가 확인되면 재지급된다.
        /// (결제는 됐지만 서버/로컬 기록이 유실된 경우를 스토어 기준으로 복구)
        /// onCompleted: 복원 완료 콜백. 인자는 광고 제거가 복원되었는지 여부.
        /// </summary>
        public void RestorePurchases(System.Action<ERestoreResult> onCompleted = null)
        {
            //네트워크 미연결 시 NETWORK_NOT_CONNECT 이벤트 발생 + NetworkUnavailable로 즉시 콜백.
            if(!NetworkUtil.EnsureConnected())
            {
                onCompleted?.Invoke(ERestoreResult.NetworkUnavailable);
                return;
            }
            if(mStoreController == null)
            {
                Debug.LogWarning("[IAPManager] 스토어 미연결 - 구매 복원 불가");
                onCompleted?.Invoke(ERestoreResult.Failed);
                return;
            }

            mOnRestoreCompleted = onCompleted;
            mStoreController.FetchPurchases();

            //응답이 안 오는 경우(오프라인 전환 등) 대비 타임아웃.
            if(mRestoreTimeoutCo != null)
            {
                StopCoroutine(mRestoreTimeoutCo);
            }
            mRestoreTimeoutCo = StartCoroutine(Co_RestoreTimeout());
        }

        private IEnumerator Co_RestoreTimeout()
        {
            yield return new WaitForSeconds(10F);

            mRestoreTimeoutCo = null;
            //타임아웃까지 OnPurchasesFetched가 안 온 경우 실패로 처리한다.
            System.Action<ERestoreResult> callback = mOnRestoreCompleted;
            mOnRestoreCompleted = null;
            callback?.Invoke(ERestoreResult.Failed);
        }
        public string GetProductPrice(EProductId eProductId)
        {
            //오프라인/스토어 미연결 시 상품 정보가 없어 NRE가 나므로 안전하게 빈 문자열을 반환한다.
            if(mStoreController == null)
            {
                return string.Empty;
            }

            string id = mProductDatabase.GetProductId(eProductId);

            Product product = mStoreController.GetProductById(id);
            if(product == null || product.metadata == null)
            {
                return string.Empty;
            }

            return product.metadata.localizedPriceString;
        }
        public List<RewardDisplayInfo> GetRewardDisplayInfos(EProductId eProductId)
        {
            return mProductDatabase.GetPackageInfo(eProductId);
        }
        public List<ProductReward> GetProductRewads(EProductId eProductId)
        {
            return mProductDatabase.GetProductRewards(eProductId);
        }
        /// <summary>해당 상품의 재구매 리필 시간(초). RefreshTimeMinute * 60.</summary>
        public int GetPackageRefreshSeconds(EProductId eProductId)
        {
            return mProductDatabase.GetRefreshTimeSeconds(eProductId);
        }
        private async Task InitializeIAP()
        {
            mStoreController = UnityIAPServices.StoreController();

            mStoreController.OnPurchasePending += OnPurchasePending;
            mStoreController.OnPurchaseFailed += OnPurchaseFailed;
            mStoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;

            await mStoreController.Connect();

            mStoreController.OnProductsFetched += OnProductsFetched;
            mStoreController.OnPurchasesFetched += OnPurchasesFetched;

            var products = new List<ProductDefinition>();
            foreach(var entry in mProductDatabase.ProductEntries)
            {
                products.Add(new ProductDefinition(entry.ProductId, entry.EProductType));
            }

            mStoreController.FetchProducts(products);
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log("[IAPManager] 상품 목록 로드 성공");

            foreach (var product in products)
            {
                Debug.Log($"[IAPManager] 상품 ID: {product.definition.id}");
            }

            mStoreController.FetchPurchases();
        }
        private void OnPurchasesFetched(Orders orders)
        {
            Debug.Log("[IAPManager] 구매 목록 로드 성공");

            //스토어에 소유 확정된 비소비성 구매(현재는 광고 제거)를 기준으로 복원한다.
            //앱 시작 시 자동 fetch에도, 구매복원 버튼으로도 이 경로를 탄다.
            bool bRestoredRemoveAds = RestoreOwnedNonConsumables(orders);

            //타임아웃 코루틴이 돌고 있으면 정지(정상 응답 도착).
            if(mRestoreTimeoutCo != null)
            {
                StopCoroutine(mRestoreTimeoutCo);
                mRestoreTimeoutCo = null;
            }

            //구매복원 버튼으로 호출된 경우에만 콜백이 있으므로, 결과를 전달하고 비운다.
            System.Action<ERestoreResult> callback = mOnRestoreCompleted;
            mOnRestoreCompleted = null;
            callback?.Invoke(bRestoredRemoveAds ? ERestoreResult.Restored : ERestoreResult.NothingToRestore);
        }

        //스토어 소유 목록(ConfirmedOrders)에서 광고 제거 소유가 확인되면 재지급한다. 복원 여부를 반환.
        private bool RestoreOwnedNonConsumables(Orders orders)
        {
            if(orders == null || orders.ConfirmedOrders == null)
            {
                return false;
            }

            bool bRestored = false;
            foreach(ConfirmedOrder order in orders.ConfirmedOrders)
            {
                foreach(CartItem cartItem in order.CartOrdered.Items())
                {
                    Product product = cartItem != null ? cartItem.Product : null;
                    if(product == null)
                    {
                        continue;
                    }

                    if(mProductDatabase.GetEProductId(product.definition.id) == EProductId.RemoveAds)
                    {
                        //RemoveAds()는 플래그 true 세팅+저장하는 멱등 처리라 중복 복원해도 안전.
                        PlayerDataManager.Inst.RemoveAds();
                        EventManager.Inst.ActiveEvent(EventKeys.REMOVE_ADS_PURCHASED);
                        bRestored = true;
                        Debug.Log("[IAPManager] 광고 제거 구매 복원 완료");
                    }
                }
            }

            return bRestored;
        }
        private void OnPurchasePending(PendingOrder order)
        {
            mStoreController.ConfirmPurchase(order);
        }
        private void OnPurchaseConfirmed(Order order)
        {
            var product = order.CartOrdered.Items().First()?.Product;
            string productId = product.definition.id;

            EProductId eProductId = mProductDatabase.GetEProductId(productId);
            mProductDatabase.GrantReward(productId);

            //초보자/중급자/상급자 패키지는 재구매 쿨타임 체크를 위해 구매 시각을 기록
            if(eProductId == EProductId.NewbiePackage
                || eProductId == EProductId.BigginerPackage
                || eProductId == EProductId.MasterPackage)
            {
                PlayerDataManager.Inst.PurchasePackage(eProductId);
            }

            EventManager.Inst.ActiveEvent(EventKeys.PURCHASE_CONFIRMED);
            EventManager.Inst.ActiveEvent(EventKeys.CONTENT_DATA_REFRESH);

            if(eProductId == EProductId.RemoveAds)
            {
                EventManager.Inst.ActiveEvent(EventKeys.REMOVE_ADS_PURCHASED);
            }

            //구매 보상 로컬 반영 후 서버에도 저장 (로그인 보장 포함, 오프라인이면 조용히 스킵)
            _ = ServerSyncService.SaveToServer();
        }
        // 구매 실패
        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"[IAPManager] 구매 실패: {failedOrder.FailureReason}");

            EventManager.Inst.ActiveEvent(EventKeys.PURCHASE_FAILED);
        }
    } 
}