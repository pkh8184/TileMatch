using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using System.Threading.Tasks;
using System.Linq;
using TrumpTile.GameMain.Data;

namespace  TrumpTile.GameMain.Core
{
    public class IAPManager : MonoBehaviour
    {
        [SerializeField] private IAPProductDatabase mProductDatabase;
        private static IAPManager instance;
        private StoreController mStoreController;
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
            if(mStoreController == null)
            {
                return;
            }
            string id = mProductDatabase.GetProductId(eProductId);
            mStoreController.PurchaseProduct(id);
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
        }
        private void OnPurchasePending(PendingOrder order)
        {
            mStoreController.ConfirmPurchase(order);
        }
        private void OnPurchaseConfirmed(Order order)
        {
            var product = order.CartOrdered.Items().First()?.Product;
            string productId = product.definition.id;

            mProductDatabase.GrantReward(productId);
        }
        // 구매 실패
        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log($"[IAPManager] 구매 실패: {failedOrder.FailureReason}");
        }
    } 
}