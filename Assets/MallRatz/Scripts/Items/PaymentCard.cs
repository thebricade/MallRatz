using MallRatz.Mall;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    // The Spender's card. It's a physical object so it can be passed, dropped on a trip, or stashed.
    // A stashed card is out of play, hidden inside its stash, until someone takes it out.
    public class PaymentCard : Holdable
    {
        [SerializeField] Wallet wallet;

        readonly NetworkVariable<bool> stashed = new();
        readonly NetworkVariable<ulong> stashId = new();

        public Wallet Wallet => wallet;
        public bool IsStashed => stashed.Value;
        protected override bool Present => !stashed.Value;

        public CardStash StashedIn =>
            stashed.Value && NetworkManager != null && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(stashId.Value, out var stash)
                ? stash.GetComponent<CardStash>()
                : null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            stashed.OnValueChanged += OnStashedChanged;
            Apply();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            stashed.OnValueChanged -= OnStashedChanged;
        }

        void OnStashedChanged(bool previous, bool current) => Apply();

        internal void ServerStash(CardStash stash)
        {
            if (IsHeld) ServerDetach(Vector3.zero);
            stashId.Value = stash.NetworkObjectId;
            stashed.Value = true;
            Apply();
            ServerPlace(stash.transform.position);
        }

        // Back in play where the stash is; whoever took it out attaches it straight after.
        internal void ServerUnstash()
        {
            stashed.Value = false;
            Apply();
        }

        public override void ServerReturnHome()
        {
            ServerUnstash();
            base.ServerReturnHome();
        }
    }
}
