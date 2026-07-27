using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    /// <summary>
    /// Tracks which shop the LOCAL player currently owns. Unlike StoreManager/DataManager
    /// (which are per-shop and never singletons), this is legitimately a single per-client
    /// singleton: each client only ever has one shop of their own to manage.
    ///
    /// Global UI panels (PC monitor screens, summary screen, etc.) bind to this instead of
    /// a specific shop, so they always reflect whichever shop the local player claimed.
    /// </summary>
    public class ClientShopContext : MonoBehaviour
    {
        public static ClientShopContext Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject(nameof(ClientShopContext));
            go.AddComponent<ClientShopContext>();
            Object.DontDestroyOnLoad(go);
        }

        /// <summary>The shop currently owned by the local player, or null if not yet claimed.</summary>
        public StoreManager MyStore { get; private set; }

        /// <summary>Raised whenever the local player's owned shop changes (e.g. after claiming one).</summary>
        public event System.Action<StoreManager> OnMyStoreChanged;

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Sets the shop owned by the local player. Called once the local player successfully
        /// claims a ShopPlot via ShopIdentity.
        /// </summary>
        public void SetMyStore(StoreManager store)
        {
            if (MyStore == store) return;

            MyStore = store;
            OnMyStoreChanged?.Invoke(store);
        }
    }
}
