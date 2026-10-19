using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    public class ShopItem : Holdable
    {
        [SerializeField] ItemDefinition definition;

        readonly NetworkVariable<int> purchaseOrder = new(-1);

        public ItemDefinition Definition => definition;
        public bool IsPaid => purchaseOrder.Value >= 0;
        // Position on the receipt of shame.
        public int PurchaseOrder => purchaseOrder.Value;
        // Share of its price it counts for on the wish list. A chipped vase counts for less.
        public virtual float Worth => 1f;
        // Host only: whose money paid for it, so a refund goes back to the right wallet.
        internal Wallet PaidFrom { get; set; }

        internal void ServerMarkPaid(int order) => purchaseOrder.Value = order;

        public override void ServerReturnHome()
        {
            base.ServerReturnHome();
            purchaseOrder.Value = -1;
            PaidFrom = null;
        }
    }
}
