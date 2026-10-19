using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Traps
{
    // A top-heavy gumball machine. Shove it over, or hit the glass hard enough (a cart, a thrown shrimp,
    // a tripped Friend), and the globe breaks and sprays gumballs across the floor. The janitor sweeps
    // them up and puts in a new globe after a while. The host decides when it breaks; everyone shows it.
    [RequireComponent(typeof(Rigidbody), typeof(Resettable))]
    public class GumballMachine : NetworkBehaviour
    {
        [SerializeField] SphereCollider globe;
        [SerializeField] Gumball[] gumballs;
        [Tooltip("Where the weight sits, locally. Higher tips over more easily.")]
        [SerializeField] Vector3 centerOfMass = new(0f, 0.9f, 0f);
        [SerializeField, Range(0, 90)] float breakTilt = 40f;
        [Tooltip("How hard something has to hit the glass to break it.")]
        [SerializeField, Min(0)] float breakImpactSpeed = 4f;
        [SerializeField, Min(0)] float burstSpeed = 3f;
        [Tooltip("The janitor refills it this long after it breaks, even if it's still standing.")]
        [SerializeField, Min(0)] float refillDelay = 30f;

        readonly NetworkVariable<bool> broken = new();

        Rigidbody body;
        Resettable resettable;
        Renderer[] globeRenderers;
        float refillAt;

        Vector3 GlobeCenter => globe.transform.TransformPoint(globe.center);
        float GlobeRadius => globe.radius * globe.transform.lossyScale.x;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            // Set by hand, so it stays put when the globe's collider turns off.
            body.centerOfMass = centerOfMass;
            resettable = GetComponent<Resettable>();
            resettable.PutBack += ServerRefill;
            globeRenderers = globe.GetComponentsInChildren<Renderer>();
        }

        public override void OnNetworkSpawn()
        {
            broken.OnValueChanged += OnBrokenChanged;
            Show();
        }

        public override void OnNetworkDespawn() => broken.OnValueChanged -= OnBrokenChanged;

        void OnBrokenChanged(bool previous, bool current) => Show();

        void Show()
        {
            globe.enabled = !broken.Value;
            foreach (var r in globeRenderers) r.enabled = !broken.Value;
            foreach (var gumball in gumballs) gumball.SetLoose(broken.Value);
        }

        void Update()
        {
            if (!IsServer) return;
            if (broken.Value)
            {
                if (Time.time >= refillAt) resettable.GoHome();
                return;
            }
            if (Vector3.Angle(transform.up, Vector3.up) > breakTilt) ServerBreak();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || broken.Value || collision.relativeVelocity.magnitude < breakImpactSpeed) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).thisCollider != globe) continue;
                ServerBreak();
                return;
            }
        }

        void ServerBreak()
        {
            var center = GlobeCenter;
            float spread = GlobeRadius * 0.7f;
            var carried = body.GetPointVelocity(center);
            broken.Value = true;
            Show();
            foreach (var gumball in gumballs)
            {
                var direction = Random.onUnitSphere;
                gumball.ServerRelease(center + direction * spread, carried + (direction + Vector3.up) * burstSpeed);
            }
            refillAt = Time.time + refillDelay;
        }

        void ServerRefill()
        {
            if (!IsServer) return;
            broken.Value = false;
            foreach (var gumball in gumballs) gumball.ServerPark(GlobeCenter);
            Show();
        }
    }
}
