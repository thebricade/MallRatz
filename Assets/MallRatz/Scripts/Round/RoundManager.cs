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
        static readonly FriendRole[] FriendRoleOrder =
            { FriendRole.Bouncer, FriendRole.Lookout, FriendRole.Returns, FriendRole.VoiceOfReason };

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
        public RoundPhase Phase => phase.Value;
        public PlayerRole Winner => winner.Value;
        public bool IsMallOpen => Phase == RoundPhase.Shopping || Phase == RoundPhase.Closing;

        public float TimeLeft => Phase switch
        {
            RoundPhase.Lobby => config.roundSeconds,
            RoundPhase.Results => 0f,
            _ => Mathf.Max(0f, (float)(closesAt.Value - NetState.ServerTime)),
        };

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

        // Share of the wish list's value bought so far. Duplicates only count as often as they're listed.
        public float WishListProgress
        {
            get
            {
                int total = 0, bought = 0;
                var unmatched = new List<ItemDefinition>();
                foreach (var item in Receipt) unmatched.Add(item.Definition);
                foreach (var wanted in config.wishList)
                {
                    if (wanted == null) continue;
                    total += wanted.price;
                    if (unmatched.Remove(wanted)) bought += wanted.price;
                }
                return total == 0 ? 0f : (float)bought / total;
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
                if (free == FriendRole.None) free = FriendRoleOrder[0];
                taken.Add(free);
                player.ServerSetRole(PlayerRole.Friend, free);
            }
        }

        // One Spender, taking turns; everyone else gets the next Friend role in order.
        void ServerAssignRoles()
        {
            var players = MallRatzPlayer.All;
            if (players.Count == 0) return;
            int spender = spenderTurn.Value % players.Count;
            int friend = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (i == spender) players[i].ServerSetRole(PlayerRole.Spender, FriendRole.None);
                else players[i].ServerSetRole(PlayerRole.Friend, FriendRoleOrder[friend++ % FriendRoleOrder.Length]);
            }

            // The Spender starts with her card.
            if (card.Holder != players[spender].Hands)
            {
                card.ServerReturnHome();
                players[spender].Hands.ServerTakeCard(card);
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
