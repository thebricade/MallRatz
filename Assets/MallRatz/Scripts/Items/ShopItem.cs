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

        internal void ServerMarkPaid(int order) => purchaseOrder.Value = order;

        public override void ServerReturnHome()
        {
            base.ServerReturnHome();
            purchaseOrder.Value = -1;
        }
    }
}
