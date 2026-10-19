using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Net
{
    // A rigidbody the host simulates. Clients keep it kinematic and ease toward the host's state, which
    // is only sent while it's moving. Clients shove it by asking the host.
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkProp : NetworkBehaviour
    {
        struct PropState : INetworkSerializeByMemcpy
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        [SerializeField, Min(1)] float sendRate = 20f;
        [SerializeField, Min(0)] float smoothing = 20f;
        [SerializeField, Min(0)] float pushSendInterval = 0.05f;

        readonly NetworkVariable<PropState> state = new();

        Rigidbody body;
        float nextSend;
        bool wasMoving;
        Vector3 pendingPush;
        Vector3 pendingPoint;
        float nextPushSend;

        // Held props are placed by whoever holds them, not simulated or synced.
        public bool Suspended { get; set; }

        void Awake() => body = GetComponent<Rigidbody>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SendNow();
                return;
            }
            body.isKinematic = true;
            transform.SetPositionAndRotation(state.Value.Position, state.Value.Rotation);
        }

        void Update()
        {
            if (!IsSpawned || Suspended) return;

            if (IsServer)
            {
                bool moving = !body.isKinematic && !body.IsSleeping();
                if ((moving && Time.time >= nextSend) || (wasMoving && !moving)) SendNow();
                wasMoving = moving;
                return;
            }

            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, state.Value.Position, t),
                Quaternion.Slerp(transform.rotation, state.Value.Rotation, t));
        }

        public void SendNow()
        {
            if (!IsServer) return;
            nextSend = Time.time + 1f / sendRate;
            state.Value = new PropState { Position = transform.position, Rotation = transform.rotation };
        }

        public void Push(Vector3 impulse, Vector3 point)
        {
            if (IsServer)
            {
                body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
                return;
            }
            pendingPush += impulse;
            pendingPoint = point;
            if (Time.time < nextPushSend) return;
            nextPushSend = Time.time + pushSendInterval;
            PushRpc(pendingPush, pendingPoint);
            pendingPush = Vector3.zero;
        }

        [Rpc(SendTo.Server)]
        void PushRpc(Vector3 impulse, Vector3 point)
        {
            if (!Suspended) body.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        }
    }
}
