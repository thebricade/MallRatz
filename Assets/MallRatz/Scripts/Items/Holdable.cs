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

        // False while it's out of play, like a shattered vase: hidden, solid to nothing, not simulated.
        protected virtual bool Present => true;

        protected void Apply()
        {
            bool held = IsHeld;
            bool present = Present;
            prop.Suspended = held || !present;
            body.isKinematic = held || !present || !IsServer;
            foreach (var c in colliders) c.enabled = !held && present;
            foreach (var r in renderers) r.enabled = slot.Value != HoldSlot.Bag && present;
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

        // Moves it without simulating the trip there, for things tucked out of play.
        protected void ServerPlace(Vector3 position)
        {
            transform.position = position;
            body.position = position;
            prop.SendNow();
        }

        // Set it down on whatever is under it, instead of dropping it from chest height.
        internal void ServerPutDown()
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            if (Physics.Raycast(bounds.center, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                transform.position += Vector3.up * (hit.point.y - bounds.min.y + 0.01f);
            ServerDetach(Vector3.zero);
        }

        // Round reset: back on the shelf it started on.
        public virtual void ServerReturnHome()
        {
            if (IsHeld) ServerDetach(Vector3.zero);
            // Out-of-play things are kinematic, and kinematic bodies don't take velocities.
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(homePosition, homeRotation);
            body.position = homePosition;
            body.rotation = homeRotation;
            prop.SendNow();
        }
    }
}
