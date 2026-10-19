using System.Collections.Generic;
using MallRatz.Net;
using MallRatz.Players;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    public enum HoldSlot : byte { None, Hand, Card, Bag }

    // Anything that can sit in a player's Hands. The host decides who holds what; every machine then
    // places the object at the holder's anchor itself, so held things don't lag behind.
    [RequireComponent(typeof(NetworkProp))]
    public class Holdable : NetworkBehaviour
    {
        static readonly List<Holdable> all = new();
        public static IReadOnlyList<Holdable> All => all;

        readonly NetworkVariable<ulong> holderId = new();
        readonly NetworkVariable<HoldSlot> slot = new();

        NetworkProp prop;
        Rigidbody body;
        Collider[] colliders;
        Renderer[] renderers;
        Vector3 homePosition;
        Quaternion homeRotation;
        Hands holder;
        ulong holderLookupId = ulong.MaxValue;

        public HoldSlot Slot => slot.Value;
        public bool IsHeld => slot.Value != HoldSlot.None;

        public Hands Holder
        {
            get
            {
                if (!IsHeld) return null;
                if (holder == null || holderLookupId != holderId.Value)
                {
                    holderLookupId = holderId.Value;
                    holder = NetworkManager != null && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(holderId.Value, out var owner)
                        ? owner.GetComponent<Hands>()
                        : null;
                }
                return holder;
            }
        }

        protected virtual void Awake()
        {
            prop = GetComponent<NetworkProp>();
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            slot.OnValueChanged += OnSlotChanged;
            Apply();
        }

        public override void OnNetworkDespawn() => slot.OnValueChanged -= OnSlotChanged;

        void OnSlotChanged(HoldSlot previous, HoldSlot current) => Apply();

        void Apply()
        {
            bool held = IsHeld;
            prop.Suspended = held;
            if (held || !IsServer) body.isKinematic = true;
            else body.isKinematic = false;
            foreach (var c in colliders) c.enabled = !held;
            foreach (var r in renderers) r.enabled = slot.Value != HoldSlot.Bag;
        }

        void LateUpdate()
        {
            var hands = Holder;
            if (hands == null) return;
            var anchor = hands.AnchorFor(slot.Value);
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        internal void ServerAttach(Hands hands, HoldSlot to)
        {
            holderId.Value = hands.NetworkObjectId;
            slot.Value = to;
            Apply();
            LateUpdate();
        }

        internal void ServerDetach(Vector3 velocity)
        {
            slot.Value = HoldSlot.None;
            Apply();
            body.linearVelocity = velocity;
            prop.SendNow();
        }

        // Round reset: back on the shelf it started on.
        public virtual void ServerReturnHome()
        {
            if (IsHeld) ServerDetach(Vector3.zero);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            body.position = homePosition;
            body.rotation = homeRotation;
            prop.SendNow();
        }
    }
}
