using MallRatz.Players;
using MallRatz.Round;
using MallRatz.UI;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    // A vase, a crystal swan, a porcelain cat. Every hard knock while it's loose chips it, and a chipped
    // one counts for less on the wish list. At zero it shatters, and if the Spender was the last to hold
    // it, she pays for it anyway: you break it, you buy it. Safe once it's paid and bagged. The host
    // decides the damage; everyone sees it.
    public class FragileItem : ShopItem
    {
        [Tooltip("Impacts slower than this don't hurt it. Setting it down gently is well under.")]
        [SerializeField, Min(0)] float safeSpeed = 3f;
        [Tooltip("Condition lost per m/s over the safe speed.")]
        [SerializeField, Range(0, 1)] float damagePerSpeed = 0.15f;
        [Tooltip("One drop rattles a few contacts at once; count it as one knock.")]
        [SerializeField, Min(0)] float knockCooldown = 0.2f;
        [SerializeField] bool youBreakItYouBuyIt = true;
        [SerializeField, Min(0)] int shardCount = 10;
        [SerializeField, Min(0)] float shardSeconds = 6f;

        readonly NetworkVariable<float> condition = new(1f);

        Rigidbody body;
        float nextKnock;
        bool lastHeldBySpender;
        float crashShownAt = float.NegativeInfinity;
        Vector3 crashPoint;
        string crashText;

        public float Condition => condition.Value;
        public bool IsBroken => condition.Value <= 0f;
        public override float Worth => condition.Value;
        protected override bool Present => base.Present && !IsBroken;

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            condition.OnValueChanged += OnConditionChanged;
            Apply();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            condition.OnValueChanged -= OnConditionChanged;
        }

        void OnConditionChanged(float previous, float current) => Apply();

        void Update()
        {
            if (!IsServer || Holder == null) return;
            var player = Holder.GetComponent<MallRatzPlayer>();
            lastHeldBySpender = player != null && player.Role == PlayerRole.Spender;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!IsServer || IsHeld || IsPaid || IsBroken || Time.time < nextKnock) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed <= safeSpeed) return;

            nextKnock = Time.time + knockCooldown;
            condition.Value = Mathf.Max(0f, condition.Value - (speed - safeSpeed) * damagePerSpeed);
            if (IsBroken) ServerShatter();
        }

        void ServerShatter()
        {
            int charged = 0;
            var round = RoundManager.Instance;
            if (youBreakItYouBuyIt && lastHeldBySpender && round != null && round.IsMallOpen)
                charged = round.Wallet.ServerCharge(Definition.price);
            ShatterRpc(transform.position, body.linearVelocity, charged);
            Apply();
        }

        [Rpc(SendTo.Everyone)]
        void ShatterRpc(Vector3 at, Vector3 velocity, int charged)
        {
            crashShownAt = Time.time;
            crashPoint = at;
            crashText = charged > 0 ? $"CRASH!\n${charged} on the Spender's card" : "CRASH!";
            SpawnShards(at, velocity);
        }

        // Just for show, so each machine throws its own and nobody syncs them.
        void SpawnShards(Vector3 at, Vector3 velocity)
        {
            var material = GetComponentInChildren<Renderer>(true).sharedMaterial;
            for (int i = 0; i < shardCount; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Shard";
                // Ignore Raycast, so shards don't get in the way of ground checks and set-downs.
                shard.layer = 2;
                shard.transform.SetPositionAndRotation(at + Random.insideUnitSphere * 0.15f, Random.rotation);
                shard.transform.localScale = new Vector3(Random.Range(0.05f, 0.14f), Random.Range(0.02f, 0.05f), Random.Range(0.05f, 0.14f));
                shard.GetComponent<Renderer>().sharedMaterial = material;
                var shardBody = shard.AddComponent<Rigidbody>();
                shardBody.mass = 0.05f;
                shardBody.linearVelocity = velocity * 0.5f + Random.onUnitSphere * 2.5f + Vector3.up;
                shardBody.angularVelocity = Random.insideUnitSphere * 15f;
                Destroy(shard, shardSeconds);
            }
        }

        public override void ServerReturnHome()
        {
            // Back in play before it moves, so it isn't kinematic when its velocity is cleared.
            condition.Value = 1f;
            lastHeldBySpender = false;
            Apply();
            base.ServerReturnHome();
        }

        // How chipped it is, over the item, and the crash when it goes.
        void OnGUI()
        {
            if (!IsSpawned) return;
            if (Time.time - crashShownAt < 3f) WorldLabel.Draw(crashPoint + Vector3.up, crashText, Color.red);
            else if (!IsBroken && !IsDelivered && Condition < 1f && Slot != HoldSlot.Bag)
                WorldLabel.Draw(transform.position + Vector3.up * 0.6f, $"{Definition.displayName} {Condition:P0}", Color.yellow);
        }
    }
}
