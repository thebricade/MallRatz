using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    // The Spender sneaks up behind whoever has her card and lifts it. It takes a moment, and fails if they
    // turn around, step away or go down. The holder gets a warning, so it's a game of who notices first.
    // Every player has one, since roles rotate; only the Spender's works. The host decides.
    [RequireComponent(typeof(MallRatzPlayer))]
    public class Pickpocket : NetworkBehaviour
    {
        [SerializeField, Min(0)] float reach = 1.6f;
        [SerializeField, Min(0)] float seconds = 1.5f;
        [Tooltip("How far round the back she has to be, measured from the way the holder is facing.")]
        [SerializeField, Range(0, 180)] float behindAngle = 110f;
        [SerializeField, Min(0)] float failCooldown = 3f;

        readonly NetworkVariable<float> progress = new();
        readonly NetworkVariable<ulong> victimId = new();

        MallRatzPlayer self;
        MallRatzPlayer victim;
        float readyAt;

        public float Progress01 => progress.Value;

        // Who she's working on, on every machine, so the victim's HUD can warn them.
        public MallRatzPlayer Victim =>
            progress.Value > 0f && NetworkManager != null && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(victimId.Value, out var target)
                ? target.GetComponent<MallRatzPlayer>()
                : null;

        void Awake() => self = GetComponent<MallRatzPlayer>();

        public bool IsBehind(MallRatzPlayer target)
        {
            var offset = Vector3.ProjectOnPlane(transform.position - target.transform.position, Vector3.up);
            var facing = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
            return offset.sqrMagnitude <= reach * reach && Vector3.Angle(facing, offset) >= behindAngle;
        }

        public MallRatzPlayer CardHolderInReach()
        {
            if (self.Role != PlayerRole.Spender || self.Hands.Card != null) return null;
            foreach (var player in MallRatzPlayer.All)
                if (player != self && player.Hands.Card != null
                    && (player.transform.position - transform.position).sqrMagnitude <= reach * reach)
                    return player;
            return null;
        }

        // For the HUD, from this machine's view.
        public string Hint()
        {
            if (progress.Value > 0f) return $"Picking their pocket... {progress.Value:P0}";
            var holder = CardHolderInReach();
            if (holder == null) return null;
            return IsBehind(holder) ? "R to pickpocket the card" : "Get behind them to pickpocket";
        }

        public void Request() => StartRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void StartRpc()
        {
            if (victim != null || Time.time < readyAt || self.Body.State != BodyState.Upright) return;
            var holder = CardHolderInReach();
            if (holder == null || !IsBehind(holder)) return;
            victim = holder;
            victimId.Value = holder.NetworkObjectId;
            progress.Value = 0.01f;
        }

        void Update()
        {
            if (!IsServer || victim == null) return;
            bool still = self.Body.State == BodyState.Upright
                && victim.Body.State == BodyState.Upright
                && victim.Hands.Card != null
                && IsBehind(victim);
            if (!still)
            {
                readyAt = Time.time + failCooldown;
                Stop();
                return;
            }

            progress.Value = seconds > 0f ? Mathf.Min(1f, progress.Value + Time.deltaTime / seconds) : 1f;
            if (progress.Value < 1f) return;
            self.Hands.ServerTakeCard(victim.Hands.Card);
            Stop();
        }

        void Stop()
        {
            victim = null;
            progress.Value = 0f;
        }
    }
}
