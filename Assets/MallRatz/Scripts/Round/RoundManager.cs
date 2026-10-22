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

    // Shopaholic: Spender vs Friends. Haul: everyone together against the clock, R.E.P.O.-style.
    public enum GameMode : byte { Shopaholic, Haul }

    // One round: a lobby, the mall opens, the closing announcement, then the receipt of shame. In Haul a
    // round is a "day": the team gets a shopping list and a budget, and has to buy the list and carry it
    // out to the car. Finish and the next day's list is longer; fail and it's back to day 1.
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
        readonly NetworkVariable<GameMode> mode = new();
        readonly NetworkVariable<int> day = new(1);
        // Haul: which wish-list entries are on today's list, one bit each.
        readonly NetworkVariable<int> listMask = new();

        int purchases;

        public RoundConfig Config => config;
        public Wallet Wallet => wallet;
        public PaymentCard Card => card;
        public RoundPhase Phase => phase.Value;
        public PlayerRole Winner => winner.Value;
        public bool IsMallOpen => Phase == RoundPhase.Shopping || Phase == RoundPhase.Closing;
        public GameMode Mode => mode.Value;
        public bool IsHaul => mode.Value == GameMode.Haul;
        // In Shopaholic, purchases disappear into a bag. In Haul you carry them out yourself.
        public bool UsesBags => !IsHaul;
        public int Day => day.Value;
        // Haul has no winner as such: Spender means the team made it, Friend means the mall closed on them.
        public bool HaulSucceeded => IsHaul && Phase == RoundPhase.Results && Winner == PlayerRole.Spender;

        float RoundSeconds => IsHaul ? config.haulSeconds : config.roundSeconds;

        public float TimeLeft => Phase switch
        {
            RoundPhase.Lobby => RoundSeconds,
            RoundPhase.Results => 0f,
            _ => Mathf.Max(0f, (float)(closesAt.Value - NetState.ServerTime)),
        };

        // While this runs down, the Spender is frozen at the entrance with her eyes shut.
        public float CountdownLeft => IsMallOpen && !IsHaul
            ? Mathf.Max(0f, config.spenderCountdownSeconds - (config.roundSeconds - TimeLeft))
            : 0f;

        // Haul: today's list. Empty in the lobby before the first day starts.
        public List<ItemDefinition> ShoppingList
        {
            get
            {
                var list = new List<ItemDefinition>();
                for (int i = 0; i < config.wishList.Count && i < 31; i++)
                    if ((listMask.Value & (1 << i)) != 0 && config.wishList[i] != null) list.Add(config.wishList[i]);
                return list;
            }
        }

        // Haul: share of today's list's value that's in the car. Chipped items count for less.
        public float DeliveredProgress
        {
            get
            {
                int total = 0;
                float delivered = 0f;
                var unmatched = new List<ShopItem>();
                foreach (var holdable in Holdable.All)
                    if (holdable is ShopItem item && item.IsDelivered) unmatched.Add(item);
                foreach (var wanted in ShoppingList)
                {
                    total += wanted.price;
                    int match = unmatched.FindIndex(item => item.Definition == wanted);
                    if (match < 0) continue;
                    delivered += wanted.price * unmatched[match].Worth;
                    unmatched.RemoveAt(match);
                }
                return total == 0 ? 0f : delivered / total;
            }
        }

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
            if (IsHaul && DeliveredProgress >= config.haulTarget) ServerEnd(PlayerRole.Spender);
            else if (TimeLeft <= 0f) ServerEnd(PlayerRole.Friend);
        }

        // Host buttons.
        public void ServerStartRound()
        {
            if (!IsServer || Phase != RoundPhase.Lobby) return;
            if (IsHaul) ServerPlanDay();
            closesAt.Value = NetState.ServerTime + RoundSeconds;
            phase.Value = RoundPhase.Shopping;
        }

        public void ServerSetMode(GameMode newMode)
        {
            if (!IsServer || Phase != RoundPhase.Lobby || newMode == Mode) return;
            mode.Value = newMode;
            day.Value = 1;
            listMask.Value = 0;
            wallet.ServerResetBalance();
            ServerAssignRoles();
        }

        // Haul: a fresh list, one item longer each day, and a budget that just covers it with some to spare.
        void ServerPlanDay()
        {
            var pool = new List<int>();
            for (int i = 0; i < config.wishList.Count && i < 31; i++)
                if (config.wishList[i] != null) pool.Add(i);
            int count = Mathf.Min(pool.Count, config.haulFirstDayItems + day.Value - 1);

            int mask = 0, budget = config.haulSpareCash;
            for (int n = 0; n < count; n++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                mask |= 1 << pool[pick];
                budget += config.wishList[pool[pick]].price;
                pool.RemoveAt(pick);
            }
            listMask.Value = mask;
            wallet.ServerSetBalance(budget);
        }

        // Puts the whole mall back, hands the Spender role to the next player, and returns to the lobby.
        // In Haul, finishing a day moves on to the next one, and failing starts over at day 1.
        public void ServerNextRound()
        {
            if (!IsServer) return;
            if (IsHaul && Phase == RoundPhase.Results) day.Value = Winner == PlayerRole.Spender ? day.Value + 1 : 1;
            listMask.Value = 0;
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
            if (Phase == RoundPhase.Lobby || IsHaul) ServerAssignRoles();
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

            // Haul: everyone's a shopper on the same team, paying from the shared budget. No card.
            if (IsHaul)
            {
                foreach (var player in players)
                {
                    if (player.Role != PlayerRole.Spender || player.FriendRole != FriendRole.None)
                        player.ServerSetRole(PlayerRole.Spender, FriendRole.None);
                    // Only reset the newcomers mid-day; everyone else keeps what they have.
                    if (Phase == RoundPhase.Lobby) player.Willpower.ServerReset();
                }
                if (card.IsHeld || card.IsStashed) card.ServerReturnHome();
                return;
            }

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
            // In Haul, buying it is only half the job; Update checks what's in the car.
            if (!IsHaul && WishListProgress >= config.wishListTarget) ServerEnd(PlayerRole.Spender);
        }

        void ServerEnd(PlayerRole roundWinner)
        {
            winner.Value = roundWinner;
            phase.Value = RoundPhase.Results;
        }
    }
}
