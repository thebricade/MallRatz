using System;
using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Mall;
using MallRatz.Net;
using MallRatz.Players;
using MallRatz.Traps;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Round
{
    public enum RoundPhase : byte { Lobby, Shopping, Closing, Results }

    // One Shopaholic round: a lobby, the mall opens, the closing announcement, then the receipt of shame.
    // The host runs the clock and the rules; everyone reads the result.
    public class RoundManager : NetworkBehaviour
    {
        // Small groups get the roles at the front. Voice of Reason is the Friends' answer to willpower, so it
        // comes early. Friends past these are plain Friends with no special tool.
        static readonly FriendRole[] FriendRoleOrder =
            { FriendRole.Bouncer, FriendRole.VoiceOfReason, FriendRole.Returns, FriendRole.Lookout };

        public static RoundManager Instance { get; private set; }

        [SerializeField] RoundConfig config;
        [SerializeField] PaymentCard card;
        [SerializeField] Wallet wallet;
        [SerializeField] Transform[] spawnPoints;

        readonly NetworkVariable<RoundPhase> phase = new();
        readonly NetworkVariable<double> closesAt = new();
        readonly NetworkVariable<PlayerRole> winner = new();
        readonly NetworkVariable<int> spenderTurn = new();

        int purchases;

        public RoundConfig Config => config;
        public Wallet Wallet => wallet;
        public PaymentCard Card => card;
        public RoundPhase Phase => phase.Value;
        public PlayerRole Winner => winner.Value;
        public bool IsMallOpen => Phase == RoundPhase.Shopping || Phase == RoundPhase.Closing;

        public float TimeLeft => Phase switch
        {
            RoundPhase.Lobby => config.roundSeconds,
            RoundPhase.Results => 0f,
            _ => Mathf.Max(0f, (float)(closesAt.Value - NetState.ServerTime)),
        };

        // While this runs down, the Spender is frozen at the entrance with her eyes shut.
        public float CountdownLeft => IsMallOpen
            ? Mathf.Max(0f, config.spenderCountdownSeconds - (config.roundSeconds - TimeLeft))
            : 0f;

        // Everything bought this round, in order: the receipt of shame.
        public List<ShopItem> Receipt
        {
            get
            {
                var receipt = new List<ShopItem>();
                foreach (var holdable in Holdable.All)
                    if (holdable is ShopItem item && item.IsPaid) receipt.Add(item);
                receipt.Sort((a, b) => a.PurchaseOrder.CompareTo(b.PurchaseOrder));
                return receipt;
            }
        }

        // Share of the wish list's value bought so far. Duplicates only count as often as they're listed,
        // and chipped items count for less.
        public float WishListProgress
        {
            get
            {
                int total = 0;
                float bought = 0f;
                var unmatched = Receipt;
                foreach (var wanted in config.wishList)
                {
                    if (wanted == null) continue;
                    total += wanted.price;
                    int match = unmatched.FindIndex(item => item.Definition == wanted);
                    if (match < 0) continue;
                    bought += wanted.price * unmatched[match].Worth;
                    unmatched.RemoveAt(match);
                }
                return total == 0 ? 0f : bought / total;
            }
        }

        public event Action<RoundPhase> PhaseChanged;

        void Awake() => Instance = this;

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            phase.OnValueChanged += OnPhaseChanged;
            if (!IsServer) return;
            Register.Purchased += ServerOnPurchased;
            ServerPlayersChanged();
        }

        public override void OnNetworkDespawn()
        {
            phase.OnValueChanged -= OnPhaseChanged;
            Register.Purchased -= ServerOnPurchased;
        }

        void OnPhaseChanged(RoundPhase previous, RoundPhase current) => PhaseChanged?.Invoke(current);

        public Transform SpawnPointFor(ulong clientId) =>
            spawnPoints[(int)(clientId % (ulong)spawnPoints.Length)];

        void Update()
        {
            if (!IsServer || !IsMallOpen) return;
            if (Phase == RoundPhase.Shopping && TimeLeft <= config.closingSeconds) phase.Value = RoundPhase.Closing;
            if (TimeLeft <= 0f) ServerEnd(PlayerRole.Friend);
        }

        // Host buttons.
        public void ServerStartRound()
        {
            if (!IsServer || Phase != RoundPhase.Lobby) return;
            closesAt.Value = NetState.ServerTime + config.roundSeconds;
            phase.Value = RoundPhase.Shopping;
        }

        // Puts the whole mall back, hands the Spender role to the next player, and returns to the lobby.
        public void ServerNextRound()
        {
            if (!IsServer) return;
            foreach (var holdable in Holdable.All) holdable.ServerReturnHome();
            foreach (var prop in FindObjectsByType<Resettable>()) prop.GoHome();
            wallet.ServerResetBalance();
            purchases = 0;
            spenderTurn.Value++;
            phase.Value = RoundPhase.Lobby;
            ServerAssignRoles();
            foreach (var player in MallRatzPlayer.All)
            {
                var spawn = SpawnPointFor(player.OwnerClientId);
                player.Body.ServerTeleport(spawn.position, spawn.eulerAngles.y);
            }
        }

        internal void ServerPlayersChanged()
        {
            if (!IsServer || !IsSpawned) return;
            if (Phase == RoundPhase.Lobby) ServerAssignRoles();
            else ServerFillFriendRoles();
        }

        // Someone joined mid-round: keep the Spender, and give newcomers whichever Friend roles are free.
        void ServerFillFriendRoles()
        {
            var taken = new HashSet<FriendRole>();
            foreach (var player in MallRatzPlayer.All) taken.Add(player.FriendRole);
            foreach (var player in MallRatzPlayer.All)
            {
                if (player.Role == PlayerRole.Spender || player.FriendRole != FriendRole.None) continue;
                var free = System.Array.Find(FriendRoleOrder, candidate => !taken.Contains(candidate));
                if (free == FriendRole.None) continue;
                taken.Add(free);
                player.ServerSetRole(PlayerRole.Friend, free);
            }
        }

        // One Spender, taking turns; everyone else gets the next Friend role in order, then plain Friend.
        void ServerAssignRoles()
        {
            var players = MallRatzPlayer.All;
            if (players.Count == 0) return;
            int spender = spenderTurn.Value % players.Count;
            int friend = 0;
            var friends = new List<MallRatzPlayer>();
            for (int i = 0; i < players.Count; i++)
            {
                if (i == spender)
                {
                    players[i].ServerSetRole(PlayerRole.Spender, FriendRole.None);
                    continue;
                }
                players[i].ServerSetRole(PlayerRole.Friend, friend < FriendRoleOrder.Length ? FriendRoleOrder[friend++] : FriendRole.None);
                friends.Add(players[i]);
            }
            foreach (var player in players) player.Willpower.ServerReset();

            // She handed her card to a Friend to hold. Playing alone, she keeps it.
            Hands keeper;
            if (config.cardStartsWithFriends && friends.Count > 0)
            {
                bool friendHasIt = friends.Exists(player => player.Hands == card.Holder);
                keeper = friendHasIt ? card.Holder : friends[UnityEngine.Random.Range(0, friends.Count)].Hands;
            }
            else keeper = players[spender].Hands;

            if (card.Holder != keeper)
            {
                card.ServerReturnHome();
                keeper.ServerTakeCard(card);
            }
        }

        void ServerOnPurchased(MallRatzPlayer buyer, ShopItem item)
        {
            if (!IsMallOpen) return;
            item.ServerMarkPaid(purchases++);
            if (WishListProgress >= config.wishListTarget) ServerEnd(PlayerRole.Spender);
        }

        void ServerEnd(PlayerRole roundWinner)
        {
            winner.Value = roundWinner;
            phase.Value = RoundPhase.Results;
        }
    }
}
