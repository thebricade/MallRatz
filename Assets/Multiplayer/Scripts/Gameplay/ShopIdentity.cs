using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace MallRatz.Multiplayer.Gameplay
{
    /// <summary>
    /// Tracks which connected client owns this shop plot. Plots start unclaimed; a client
    /// claims one by walking up to its <see cref="ShopClaimPoint"/> and interacting with it.
    /// The server is the sole authority over ownership changes.
    /// </summary>
    public class ShopIdentity : NetworkBehaviour
    {
        private const int UnclaimedOwnerId = -1;

        private readonly SyncVar<int> ownerClientId = new SyncVar<int>(UnclaimedOwnerId);

        /// <summary>Raised on every client when this plot's claimed state changes.</summary>
        public event System.Action<bool> OnClaimStateChanged;

        public bool IsClaimed => ownerClientId.Value != UnclaimedOwnerId;

        /// <summary>True if the local client is the one who owns this shop plot.</summary>
        public bool IsOwnedByLocalClient =>
            InstanceFinder.ClientManager != null
            && InstanceFinder.ClientManager.Connection != null
            && ownerClientId.Value == InstanceFinder.ClientManager.Connection.ClientId;

        private void Awake()
        {
            ownerClientId.OnChange += HandleOwnerClientIdChanged;
        }

        private void HandleOwnerClientIdChanged(int previousOwnerId, int nextOwnerId, bool asServer)
        {
            OnClaimStateChanged?.Invoke(nextOwnerId != UnclaimedOwnerId);

            // If the local client is the one who just claimed this plot, bind the UI to this shop.
            if (IsOwnedByLocalClient)
            {
                var storeManager = GetComponent<StoreManager>();
                if (storeManager != null) ClientShopContext.Instance.SetMyStore(storeManager);
            }
        }

        /// <summary>
        /// True if the given connection currently owns this shop plot.
        /// </summary>
        public bool IsOwnedBy(NetworkConnection connection)
        {
            return connection != null && ownerClientId.Value == connection.ClientId;
        }

        /// <summary>
        /// Requests that the calling client's connection claims this plot. Ignored by the
        /// server if the plot is already claimed. Safe to call from any connected client,
        /// regardless of whether they own this NetworkObject (they don't; it's a shared
        /// scene object).
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestClaim(NetworkConnection caller = null)
        {
            if (IsClaimed) return;
            if (caller == null) return;

            ownerClientId.Value = caller.ClientId;
        }
    }
}
