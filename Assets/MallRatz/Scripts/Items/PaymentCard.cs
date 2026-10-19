using UnityEngine;

namespace MallRatz.Items
{
    // The Spender's card. It's a physical object so it can be passed, dropped on a trip, or stashed.
    public class PaymentCard : Holdable
    {
        [SerializeField] Wallet wallet;

        public Wallet Wallet => wallet;
    }
}
