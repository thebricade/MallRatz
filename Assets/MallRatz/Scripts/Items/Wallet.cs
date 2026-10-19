using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    // The Spender's budget. Only the host spends or refunds; everyone sees the balance.
    public class Wallet : NetworkBehaviour
    {
        [SerializeField, Min(0)] int startingBalance = 300;

        readonly NetworkVariable<int> balance = new();

        public int Balance => balance.Value;
        public int StartingBalance => startingBalance;

        public override void OnNetworkSpawn()
        {
            if (IsServer) ServerResetBalance();
        }

        public bool ServerTrySpend(int amount)
        {
            if (amount < 0 || amount > balance.Value) return false;
            balance.Value -= amount;
            return true;
        }

        public void ServerRefund(int amount) => balance.Value += Mathf.Max(0, amount);

        public void ServerResetBalance() => balance.Value = startingBalance;
    }
}
