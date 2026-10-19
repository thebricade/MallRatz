using MallRatz.Net;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    // The Bouncer's grab: the only way to carry the Spender out of a shop. Every player has one, since
    // roles rotate; only the Bouncer's works. The host decides; the owner just asks.
    [RequireComponent(typeof(MallRatzPlayer))]
    public class Carrier : NetworkBehaviour
    {
        [SerializeField] Transform carryAnchor;
        [SerializeField, Min(0)] float reach = 2.2f;
        [SerializeField, Min(0)] float maxCarrySeconds = 5f;
        [SerializeField, Min(0)] float cooldownSeconds = 20f;
        [SerializeField, Min(0)] float throwSpeed = 6f;

        readonly NetworkVariable<bool> carrying = new();
        readonly NetworkVariable<double> readyAt = new();

        MallRatzPlayer self;
        PlayerBody carried;
        double carryUntil;

        public Transform CarryAnchor => carryAnchor;
        public bool IsCarrying => carrying.Value;
        public float CooldownLeft => Mathf.Max(0f, (float)(readyAt.Value - NetState.ServerTime));

        void Awake() => self = GetComponent<MallRatzPlayer>();

        void Update()
        {
            if (!IsServer || carried == null) return;
            if (NetState.ServerTime >= carryUntil || self.Body.State != BodyState.Upright) ServerRelease(toss: false);
        }

        public bool CanGrab(MallRatzPlayer target)
        {
            if (self.FriendRole != FriendRole.Bouncer || target.Role != PlayerRole.Spender) return false;
            if (target.Body.State == BodyState.Carried) return false;
            // Hands off in the hallway: only inside a shop, or while she's holding unpaid stuff.
            return target.IsInShop || target.Hands.HoldsUnpaid;
        }

        // For the HUD: what F would do right now, or why it won't. Read from this machine's view, so it
        // can be a moment out of date compared to the host.
        public string GrabHint()
        {
            if (self.FriendRole != FriendRole.Bouncer) return null;
            if (IsCarrying) return "Carrying her: F set down, Q toss";
            if (CooldownLeft > 0f) return $"Grab ready in {CooldownLeft:0}s";

            MallRatzPlayer spender = null;
            foreach (var player in MallRatzPlayer.All)
                if (player.Role == PlayerRole.Spender) spender = player;
            if (spender == null) return "No Spender to grab";
            if (spender.Body.State == BodyState.Carried) return "She's already being carried";
            if (!spender.IsInShop && !spender.Hands.HoldsUnpaid) return "Hands off: she's in the hallway with nothing unpaid";
            if ((spender.transform.position - transform.position).sqrMagnitude > reach * reach) return "Get closer to grab her";
            return "F to grab her";
        }

        // Owner requests.
        public void RequestGrabOrSetDown() => GrabOrSetDownRpc();
        public void RequestToss() => TossRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void GrabOrSetDownRpc()
        {
            if (carried != null) ServerRelease(toss: false);
            else ServerTryGrab();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void TossRpc() => ServerRelease(toss: true);

        void ServerTryGrab()
        {
            if (CooldownLeft > 0f || self.Body.State != BodyState.Upright) return;

            MallRatzPlayer nearest = null;
            float nearestDistance = reach * reach;
            foreach (var player in MallRatzPlayer.All)
            {
                if (player == self || !CanGrab(player)) continue;
                float distance = (player.transform.position - transform.position).sqrMagnitude;
                if (distance <= nearestDistance) (nearest, nearestDistance) = (player, distance);
            }
            if (nearest == null) return;

            nearest.Body.ServerBeginCarry(this);
            // Grabbing her takes the card back.
            self.Hands.ServerTakeCard(nearest.Hands.Card);

            carried = nearest.Body;
            carrying.Value = true;
            carryUntil = NetState.ServerTime + maxCarrySeconds;
        }

        internal void ServerDrop(PlayerBody body)
        {
            if (carried == body) ServerRelease(toss: false);
        }

        void ServerRelease(bool toss)
        {
            if (carried == null) return;
            carried.ServerEndCarry(toss ? transform.forward * throwSpeed + Vector3.up * 2f : Vector3.zero);
            carried = null;
            carrying.Value = false;
            readyAt.Value = NetState.ServerTime + cooldownSeconds;
        }
    }
}
