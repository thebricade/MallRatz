using MallRatz.Items;
using MallRatz.Mall;
using MallRatz.Net;
using MallRatz.Round;
using MallRatz.UI;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    // Every Friend has a secret weakness (one of the shops) and hidden willpower. Hanging around that shop
    // wears it down, and the Spender can tempt them ("treat yourself!") to knock off a big chunk. At zero
    // they give in and switch sides: a Spender with their own pocket cash, whose purchases count for the
    // Spenders. At most half the Friends can turn, and a Friend can talk a turned one back, once each.
    // Friends shore each other up: sticking together recovers willpower faster, and the Voice of Reason
    // steadies everyone near them, even mid-temptation, and can snap one Friend out of it.
    // Only the owner sees their own willpower and weakness; everyone sees when someone looks tempted.
    // T does it all: Spenders tempt, Friends talk people back, the Voice of Reason gives pep talks.
    // The host runs it.
    [RequireComponent(typeof(MallRatzPlayer), typeof(Wallet))]
    public class Willpower : NetworkBehaviour
    {
        const float Full = 100f;

        [Tooltip("Per second spent inside your weakness shop.")]
        [SerializeField, Min(0)] float drainPerSecond = 2.5f;
        [Tooltip("Per second anywhere else.")]
        [SerializeField, Min(0)] float regenPerSecond = 0.5f;
        [Tooltip("Buddy system: recovery while another Friend is this close, instead of the above.")]
        [SerializeField, Min(0)] float buddyReach = 4f;
        [SerializeField, Min(0)] float buddyRegenPerSecond = 2f;
        [Tooltip("Below this, everyone can see you're wavering.")]
        [SerializeField, Range(0, 100)] float temptedBelow = 30f;

        [Header("Tempting")]
        [SerializeField, Min(0)] float temptReach = 2.5f;
        [SerializeField, Min(0)] float temptAmount = 30f;
        [Tooltip("Tempting someone inside their weakness shop hits harder.")]
        [SerializeField, Min(1)] float weaknessMultiplier = 1.5f;
        [SerializeField, Min(0)] int temptsPerRound = 3;
        [SerializeField, Min(0)] float temptCooldown = 5f;

        [Header("Capsule toy bait")]
        [Tooltip("A Spender holding a toy that matches your weakness, this close, wears you down.")]
        [SerializeField, Min(0)] float baitReach = 3f;
        [SerializeField, Min(0)] float baitDrainPerSecond = 3f;
        [Tooltip("\"Treat yourself, I got you this!\" Tempting with a matching toy hits this much harder.")]
        [SerializeField, Min(1)] float giftMultiplier = 2f;

        [Header("Voice of Reason")]
        [Tooltip("Friends this close to the Voice of Reason recover this much per second, even while being drained.")]
        [SerializeField, Min(0)] float reasonReach = 5f;
        [SerializeField, Min(0)] float reasonRegenPerSecond = 4f;
        [Tooltip("\"Snap out of it!\": an instant top-up for the nearest Friend.")]
        [SerializeField, Min(0)] float pepTalkReach = 3f;
        [SerializeField, Min(0)] float pepTalkAmount = 40f;
        [SerializeField, Min(0)] float pepTalkCooldown = 15f;
        [Tooltip("The Voice of Reason's own willpower drains this much slower.")]
        [SerializeField, Range(0, 1)] float reasonDrainMultiplier = 0.5f;
        [Tooltip("And they talk people back this much faster.")]
        [SerializeField, Min(1)] float reasonTalkSpeed = 2f;

        [Header("Turning")]
        [Tooltip("Share of the Friends that can be turned at once, rounded down.")]
        [SerializeField, Range(0, 1)] float maxTurnedShare = 0.5f;
        [SerializeField, Min(0)] int pocketCash = 100;

        [Header("Talking them back")]
        [SerializeField, Min(0)] float talkReach = 2f;
        [SerializeField, Min(0)] float talkSeconds = 4f;
        [Tooltip("How far either of you can drift before the talk breaks off.")]
        [SerializeField, Min(0)] float stillTolerance = 0.4f;
        [SerializeField, Range(0, 100)] float willpowerAfterTalk = 50f;

        readonly NetworkVariable<float> willpower = new(Full, NetworkVariableReadPermission.Owner);
        readonly NetworkVariable<int> weakness = new(-1, NetworkVariableReadPermission.Owner);
        readonly NetworkVariable<bool> tempted = new();
        readonly NetworkVariable<bool> turned = new();
        readonly NetworkVariable<int> temptsLeft = new();
        readonly NetworkVariable<float> talkProgress = new();
        // The theme of the toy someone's waving at you, or -1. Everyone sees it: that's the giveaway.
        readonly NetworkVariable<int> staringAt = new(-1);
        readonly NetworkVariable<double> pepTalkReadyAt = new();

        MallRatzPlayer self;
        float nextTempt;
        FriendRole turnedFrom;
        bool talkedBack;
        MallRatzPlayer talker;
        Vector3 talkerStart, selfStart;
        float bubbleShownAt = float.NegativeInfinity;
        string bubbleText;
        float announcedAt = float.NegativeInfinity;
        string announcement;

        public bool IsTurned => turned.Value;
        public bool LooksTempted => tempted.Value;
        public int TemptsLeft => temptsLeft.Value;
        public float TalkProgress => talkProgress.Value;
        // Only meaningful on the owner's machine (and the host's).
        public float Value => willpower.Value;
        public ShopZone Weakness => weakness.Value >= 0 && weakness.Value < ShopZone.ByName.Count ? ShopZone.ByName[weakness.Value] : null;

        bool InWeaknessShop => Weakness != null && ShopZone.At(transform.position) == Weakness;
        bool IsVoiceOfReason => self.Role == PlayerRole.Friend && self.FriendRole == FriendRole.VoiceOfReason;
        float PepTalkCooldownLeft => Mathf.Max(0f, (float)(pepTalkReadyAt.Value - NetState.ServerTime));

        void Awake() => self = GetComponent<MallRatzPlayer>();

        public override void OnNetworkSpawn()
        {
            if (IsServer) ServerReset();
        }

        // New round: full willpower, a fresh secret weakness, back on your own side.
        internal void ServerReset()
        {
            willpower.Value = Full;
            weakness.Value = ShopZone.ByName.Count > 0 ? Random.Range(0, ShopZone.ByName.Count) : -1;
            tempted.Value = false;
            turned.Value = false;
            temptsLeft.Value = temptsPerRound;
            staringAt.Value = -1;
            pepTalkReadyAt.Value = 0d;
            talkedBack = false;
            StopTalk();
            // The Spender gets a little cash of her own, for capsule toys and small things before she has her card.
            var round = RoundManager.Instance;
            int cash = self.Role == PlayerRole.Spender && round != null && round.Config != null ? round.Config.spenderPocketCash : 0;
            self.PocketCash.ServerSetBalance(cash);
        }

        void Update()
        {
            if (!IsServer) return;
            UpdateTalk();

            var round = RoundManager.Instance;
            if (round == null || !round.IsMallOpen || round.CountdownLeft > 0f || self.Role != PlayerRole.Friend)
            {
                staringAt.Value = -1;
                return;
            }

            var bait = Bait();
            staringAt.Value = bait != null ? bait.Theme : -1;
            float drain = (InWeaknessShop ? drainPerSecond : 0f) + (bait != null ? baitDrainPerSecond : 0f);
            if (IsVoiceOfReason) drain *= reasonDrainMultiplier;
            float change = drain > 0f ? -drain
                : NearFriend(buddyReach, _ => true) ? buddyRegenPerSecond
                : regenPerSecond;
            if (NearFriend(reasonReach, player => player.FriendRole == FriendRole.VoiceOfReason)) change += reasonRegenPerSecond;
            ServerChange(change * Time.deltaTime);
        }

        bool NearFriend(float reach, System.Predicate<MallRatzPlayer> wanted) =>
            Nearest(reach, player => player.Role == PlayerRole.Friend && wanted(player)) != null;

        // A Spender close by, holding a toy that matches this Friend's weakness.
        Trinket Bait()
        {
            foreach (var player in MallRatzPlayer.All)
            {
                if (player.Role != PlayerRole.Spender || !(player.Hands.Held is Trinket toy) || toy.Theme != weakness.Value) continue;
                if ((player.transform.position - transform.position).sqrMagnitude <= baitReach * baitReach) return toy;
            }
            return null;
        }

        void ServerChange(float delta)
        {
            // Once half the Friends have gone, the rest hang on by a thread instead.
            float floor = CanTurn() ? 0f : 1f;
            willpower.Value = Mathf.Clamp(willpower.Value + delta, floor, Full);
            tempted.Value = willpower.Value < temptedBelow;
            if (willpower.Value <= 0f) ServerTurn();
        }

        bool CanTurn()
        {
            int friends = 0, turnedCount = 0;
            foreach (var player in MallRatzPlayer.All)
            {
                if (player.Willpower.IsTurned) turnedCount++;
                if (player.Willpower.IsTurned || player.Role == PlayerRole.Friend) friends++;
            }
            return turnedCount + 1 <= Mathf.FloorToInt(friends * maxTurnedShare);
        }

        void ServerTurn()
        {
            turnedFrom = self.FriendRole;
            self.ServerSetRole(PlayerRole.Spender, FriendRole.None);
            turned.Value = true;
            tempted.Value = false;
            self.PocketCash.ServerSetBalance(pocketCash);
            AnnounceRpc($"{self.DisplayName} gave in!\nThey're shopping for the Spender now.");
        }

        void ServerUnturn()
        {
            self.ServerSetRole(PlayerRole.Friend, turnedFrom);
            turned.Value = false;
            talkedBack = true;
            willpower.Value = willpowerAfterTalk;
            tempted.Value = willpower.Value < temptedBelow;
            self.PocketCash.ServerSetBalance(0);
            AnnounceRpc($"{talker.DisplayName} talked {self.DisplayName} back round!");
        }

        // Owner request: T.
        public void Request() => TalkRpc();

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void TalkRpc()
        {
            if (self.Body.State != BodyState.Upright) return;
            if (self.Role == PlayerRole.Spender) ServerTryTempt();
            else if (!ServerTryTalkBack() && IsVoiceOfReason) ServerTryPepTalk();
        }

        void ServerTryPepTalk()
        {
            if (PepTalkCooldownLeft > 0f) return;
            var target = Nearest(pepTalkReach, player => player.Role == PlayerRole.Friend);
            if (target == null) return;
            target.Willpower.ServerChange(pepTalkAmount);
            pepTalkReadyAt.Value = NetState.ServerTime + pepTalkCooldown;
            BubbleRpc($"\"Snap out of it, {target.DisplayName}!\"");
        }

        void ServerTryTempt()
        {
            if (temptsLeft.Value <= 0 || Time.time < nextTempt) return;
            var target = Nearest(temptReach, player => player.Role == PlayerRole.Friend);
            if (target == null) return;

            float amount = temptAmount * (target.Willpower.InWeaknessShop ? weaknessMultiplier : 1f);
            // A matching toy is a gift: it hits harder and she hands it over.
            bool gift = self.Hands.Held is Trinket toy && toy.Theme == target.Willpower.weakness.Value;
            if (gift)
            {
                amount *= giftMultiplier;
                ((Trinket)self.Hands.Held).ServerReturnHome();
            }
            target.Willpower.ServerChange(-amount);
            temptsLeft.Value--;
            nextTempt = Time.time + temptCooldown;
            BubbleRpc(gift ? "\"Treat yourself! I got you this!\"" : "\"Treat yourself!\"");
        }

        bool ServerTryTalkBack()
        {
            var target = Nearest(talkReach, player => player.Willpower.IsTurned && !player.Willpower.talkedBack && player.Willpower.talker == null);
            if (target == null) return false;
            var other = target.Willpower;
            other.talker = self;
            other.talkerStart = transform.position;
            other.selfStart = target.transform.position;
            other.talkProgress.Value = 0.01f;
            return true;
        }

        // Runs on the turned player's side while someone talks to them.
        void UpdateTalk()
        {
            if (talker == null) return;
            float tolerance = stillTolerance * stillTolerance;
            bool still = IsTurned
                && talker.Role == PlayerRole.Friend
                && self.Body.State == BodyState.Upright
                && talker.Body.State == BodyState.Upright
                && (talker.transform.position - talkerStart).sqrMagnitude <= tolerance
                && (transform.position - selfStart).sqrMagnitude <= tolerance;
            if (!still)
            {
                StopTalk();
                return;
            }

            float speed = talker.FriendRole == FriendRole.VoiceOfReason ? reasonTalkSpeed : 1f;
            talkProgress.Value = talkSeconds > 0f ? Mathf.Min(1f, talkProgress.Value + speed * Time.deltaTime / talkSeconds) : 1f;
            if (talkProgress.Value < 1f) return;
            ServerUnturn();
            StopTalk();
        }

        void StopTalk()
        {
            talker = null;
            talkProgress.Value = 0f;
        }

        MallRatzPlayer Nearest(float reach, System.Predicate<MallRatzPlayer> wanted)
        {
            MallRatzPlayer nearest = null;
            float nearestDistance = reach * reach;
            foreach (var player in MallRatzPlayer.All)
            {
                if (player == self || !wanted(player)) continue;
                float distance = (player.transform.position - transform.position).sqrMagnitude;
                if (distance <= nearestDistance) (nearest, nearestDistance) = (player, distance);
            }
            return nearest;
        }

        // For the HUD, from this machine's view.
        public string Hint()
        {
            if (self.Role == PlayerRole.Friend)
            {
                string shop = Weakness != null ? Weakness.ShopName : "?";
                string warning = InWeaknessShop ? "   Your weakness! It's draining" : "";
                string help = IsVoiceOfReason
                    ? $"Friends near you recover. T: \"Snap out of it!\" +{pepTalkAmount:0} to the nearest Friend"
                      + (PepTalkCooldownLeft > 0f ? $" (in {PepTalkCooldownLeft:0}s)" : " (ready)")
                    : "Stick with other Friends to recover faster";
                return $"Willpower {Value:0}%   Weakness: {shop}{warning}\n{help}\nT near someone who gave in: talk them back (both stand still)";
            }

            string lines = IsTurned ? "You gave in! Buy wish-list stuff for her.\n" : "";
            lines += $"Pocket cash ${self.PocketCash.Balance}\n";
            if (talkProgress.Value > 0f) lines += $"Someone's talking you back... {talkProgress.Value:P0}\n";
            lines += temptsLeft.Value > 0 ? $"T near a Friend: \"treat yourself!\" ({temptsLeft.Value} left)" : "No temptations left";
            return lines;
        }

        [Rpc(SendTo.Everyone)]
        void BubbleRpc(string text)
        {
            bubbleShownAt = Time.time;
            bubbleText = text;
        }

        [Rpc(SendTo.Everyone)]
        void AnnounceRpc(string text)
        {
            announcedAt = Time.time;
            announcement = text;
        }

        void OnGUI()
        {
            if (!IsSpawned) return;
            var head = transform.position + Vector3.up * 2.1f;
            if (Time.time - announcedAt < 3f) WorldLabel.Draw(head + Vector3.up * 0.4f, announcement, Color.magenta);
            if (Time.time - bubbleShownAt < 2f) WorldLabel.Draw(head, bubbleText, new Color(1f, 0.5f, 0.8f));
            else if (talkProgress.Value > 0f) WorldLabel.Draw(head, $"Being talked back... {talkProgress.Value:P0}", Color.cyan);
            else if (staringAt.Value >= 0 && staringAt.Value < ShopZone.ByName.Count)
                WorldLabel.Draw(head, $"(staring at the {ShopZone.ByName[staringAt.Value].TrinketName})", Color.yellow);
            else if (LooksTempted && !IsOwner) WorldLabel.Draw(head, "(looks tempted)", Color.yellow);
        }
    }
}
