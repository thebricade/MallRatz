using System;
using MallRatz.Round;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    public enum BodyState : byte { Upright, Tripped, Carried }

    // Upright: driven by the CharacterController. Tripped: a tipped-over rigidbody capsule, standing in
    // for a real ragdoll. Carried: pinned to the Bouncer's carry anchor (the roadmap's "fake carry").
    // The owner simulates its own body and NetworkTransform shares it. The host decides trips and carries
    // that involve anyone else and tells the owner.
    [RequireComponent(typeof(CharacterController), typeof(CapsuleCollider), typeof(Rigidbody))]
    public class PlayerBody : NetworkBehaviour
    {
        [SerializeField, Min(0)] float tripSeconds = 2f;
        [Tooltip("After getting up, a player can't be tripped again for this long.")]
        [SerializeField, Min(0)] float getUpGraceSeconds = 3f;
        [SerializeField, Min(0)] float toppleSpeed = 4f;
        [Tooltip("Presses of Space it takes to wriggle out of a carry.")]
        [SerializeField, Min(1)] int wrigglesToEscape = 8;

        readonly NetworkVariable<BodyState> state = new(writePerm: NetworkVariableWritePermission.Owner);

        CharacterController controller;
        CapsuleCollider ragdollCollider;
        Rigidbody body;
        Hands hands;
        Transform carryAnchor;
        float trippedUntil;
        float graceUntil;
        int wriggles;

        public BodyState State => state.Value;
        public int WrigglesLeft => Mathf.Max(0, wrigglesToEscape - wriggles);
        public bool CanBeTripped => State == BodyState.Upright && Time.time >= graceUntil;

        public event Action<BodyState> StateChanged;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            ragdollCollider = GetComponent<CapsuleCollider>();
            body = GetComponent<Rigidbody>();
            hands = GetComponent<Hands>();
            ragdollCollider.enabled = false;
            body.isKinematic = true;
        }

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += OnStateChanged;
            ApplyRemote();
            if (IsOwner && RoundManager.Instance != null)
            {
                var spawn = RoundManager.Instance.SpawnPointFor(OwnerClientId);
                Teleport(spawn.position, spawn.eulerAngles.y);
            }
        }

        public override void OnNetworkDespawn() => state.OnValueChanged -= OnStateChanged;

        void OnStateChanged(BodyState previous, BodyState current)
        {
            ApplyRemote();
            StateChanged?.Invoke(current);
        }

        // Other machines' copies just follow NetworkTransform, but still need the right collider
        // so the host's props can hit them.
        void ApplyRemote()
        {
            if (IsOwner) return;
            body.isKinematic = true;
            controller.enabled = State == BodyState.Upright;
            ragdollCollider.enabled = State == BodyState.Tripped;
        }

        void Update()
        {
            if (IsOwner && State == BodyState.Tripped && Time.time >= trippedUntil) GetUp();
        }

        void LateUpdate()
        {
            if (IsOwner && State == BodyState.Carried && carryAnchor != null)
                transform.SetPositionAndRotation(carryAnchor.position, carryAnchor.rotation * Quaternion.Euler(0f, 0f, 90f));
        }

        // Anyone can report a trip; the owner has the final say (grace period, already down).
        public void Trip(Vector3 velocity)
        {
            if (IsOwner) OwnerTrip(velocity);
            else if (IsServer && State == BodyState.Upright) TripRpc(velocity);
        }

        [Rpc(SendTo.Owner)]
        void TripRpc(Vector3 velocity) => OwnerTrip(velocity);

        void OwnerTrip(Vector3 velocity)
        {
            if (!CanBeTripped) return;
            DropAllRpc(velocity * 0.5f + Vector3.up * 2f);
            Ragdoll(velocity);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void DropAllRpc(Vector3 velocity) => hands.ServerDropAll(velocity);

        internal void ServerBeginCarry(Carrier carrier) => BeginCarryRpc(carrier.NetworkObject);

        // Zero velocity sets them down on their feet; anything else is a throw, which counts as a trip.
        internal void ServerEndCarry(Vector3 velocity) => EndCarryRpc(velocity);

        [Rpc(SendTo.Owner)]
        void BeginCarryRpc(NetworkObjectReference carrierReference)
        {
            if (State == BodyState.Carried || !carrierReference.TryGet(out var carrierObject)) return;
            carryAnchor = carrierObject.GetComponent<Carrier>().CarryAnchor;
            wriggles = 0;
            controller.enabled = false;
            ragdollCollider.enabled = false;
            body.isKinematic = true;
            state.Value = BodyState.Carried;
        }

        [Rpc(SendTo.Owner)]
        void EndCarryRpc(Vector3 velocity)
        {
            if (State != BodyState.Carried) return;
            carryAnchor = null;
            if (velocity.sqrMagnitude < 0.01f) GetUp();
            else Ragdoll(velocity);
        }

        // Being carried isn't fun for long: mash to get out early.
        public void Wriggle()
        {
            if (!IsOwner || State != BodyState.Carried || wriggles >= wrigglesToEscape) return;
            if (++wriggles >= wrigglesToEscape) WriggleFreeRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void WriggleFreeRpc()
        {
            foreach (var carrier in FindObjectsByType<Carrier>()) carrier.ServerDrop(this);
        }

        internal void ServerTeleport(Vector3 position, float yaw) => TeleportRpc(position, yaw);

        [Rpc(SendTo.Owner)]
        void TeleportRpc(Vector3 position, float yaw) => Teleport(position, yaw);

        void Teleport(Vector3 position, float yaw)
        {
            carryAnchor = null;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (State != BodyState.Upright) GetUp();
            else controller.enabled = true;
        }

        void Ragdoll(Vector3 velocity)
        {
            controller.enabled = false;
            ragdollCollider.enabled = true;
            body.isKinematic = false;
            body.AddForce(velocity, ForceMode.VelocityChange);

            var flat = Vector3.ProjectOnPlane(velocity, Vector3.up);
            var axis = flat.sqrMagnitude > 0.01f ? Vector3.Cross(Vector3.up, flat.normalized) : transform.right;
            body.AddTorque(axis * toppleSpeed, ForceMode.VelocityChange);

            trippedUntil = Time.time + tripSeconds;
            state.Value = BodyState.Tripped;
        }

        void GetUp()
        {
            body.isKinematic = true;
            ragdollCollider.enabled = false;

            var position = transform.position;
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out var ground, 5f, ~0, QueryTriggerInteraction.Ignore))
                position.y = ground.point.y + controller.height * 0.5f - controller.center.y;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

            controller.enabled = true;
            graceUntil = Time.time + getUpGraceSeconds;
            state.Value = BodyState.Upright;
        }
    }
}
