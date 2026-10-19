using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Traps
{
    // A smoothie or mop bucket: knock it over and it leaves a slippery patch until the janitor cleans up.
    // The host decides when it spills; everyone shows the patch.
    [RequireComponent(typeof(Resettable))]
    public class SpillSource : NetworkBehaviour
    {
        [SerializeField] SlipperySurface patch;
        [SerializeField, Range(0, 180)] float spillAngle = 60f;

        readonly NetworkVariable<bool> spilled = new();
        readonly NetworkVariable<Vector3> spot = new();

        void Awake()
        {
            patch.gameObject.SetActive(false);
            GetComponent<Resettable>().PutBack += ServerClean;
        }

        public override void OnNetworkSpawn()
        {
            spilled.OnValueChanged += OnSpilledChanged;
            ShowPatch();
        }

        public override void OnNetworkDespawn() => spilled.OnValueChanged -= OnSpilledChanged;

        void OnSpilledChanged(bool previous, bool current) => ShowPatch();

        void ShowPatch()
        {
            patch.transform.SetPositionAndRotation(spot.Value, Quaternion.identity);
            patch.gameObject.SetActive(spilled.Value);
        }

        void Update()
        {
            if (!IsServer || spilled.Value || Vector3.Angle(transform.up, Vector3.up) < spillAngle) return;

            var floor = transform.position;
            if (Physics.Raycast(floor, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                floor = hit.point;
            spot.Value = floor;
            spilled.Value = true;
            ShowPatch();
        }

        void ServerClean()
        {
            if (!IsServer) return;
            spilled.Value = false;
            ShowPatch();
        }
    }
}
