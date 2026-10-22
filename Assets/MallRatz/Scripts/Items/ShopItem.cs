using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    public class ShopItem : Holdable
    {
        [SerializeField] ItemDefinition definition;

        readonly NetworkVariable<int> purchaseOrder = new(-1);
        // Haul: loaded into the car. It's out of play until the next day.
        readonly NetworkVariable<bool> delivered = new();

        public ItemDefinition Definition => definition;
        public bool IsPaid => purchaseOrder.Value >= 0;
        public bool IsDelivered => delivered.Value;
        // Position on the receipt of shame.
        public int PurchaseOrder => purchaseOrder.Value;
        // Share of its price it counts for on the wish list. A chipped vase counts for less.
        public virtual float Worth => 1f;
        // Host only: whose money paid for it, so a refund goes back to the right wallet.
        internal Wallet PaidFrom { get; set; }
        protected override bool Present => !delivered.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            delivered.OnValueChanged += OnDeliveredChanged;
            Apply();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            delivered.OnValueChanged -= OnDeliveredChanged;
        }

        void OnDeliveredChanged(bool previous, bool current) => Apply();

        internal void ServerMarkPaid(int order) => purchaseOrder.Value = order;

        internal void ServerDeliver()
        {
            delivered.Value = true;
            Apply();
        }

        public override void ServerReturnHome()
        {
            base.ServerReturnHome();
            purchaseOrder.Value = -1;
            PaidFrom = null;
            delivered.Value = false;
            Apply();
        }
    }
}
