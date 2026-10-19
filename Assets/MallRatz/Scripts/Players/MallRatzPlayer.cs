using System.Collections.Generic;
using MallRatz.Mall;
using MallRatz.Round;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    [RequireComponent(typeof(Hands), typeof(PlayerBody))]
    public class MallRatzPlayer : NetworkBehaviour
    {
        static readonly List<MallRatzPlayer> all = new();
        public static IReadOnlyList<MallRatzPlayer> All => all;

        // The player this machine controls.
        public static MallRatzPlayer Local { get; private set; }

        readonly NetworkVariable<PlayerRole> role = new(PlayerRole.Friend);
        readonly NetworkVariable<FriendRole> friendRole = new(FriendRole.None);

        public PlayerRole Role => role.Value;
        public FriendRole FriendRole => friendRole.Value;
        public Hands Hands { get; private set; }
        public PlayerBody Body { get; private set; }
        public bool IsInShop => ShopZone.At(transform.position) != null;
        public string DisplayName => $"Player {OwnerClientId + 1}";

        void Awake()
        {
            Hands = GetComponent<Hands>();
            Body = GetComponent<PlayerBody>();
        }

        public override void OnNetworkSpawn()
        {
            all.Add(this);
            all.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
            name = DisplayName;
            if (IsOwner) Local = this;
            if (IsServer && RoundManager.Instance != null) RoundManager.Instance.ServerPlayersChanged();
        }

        public override void OnNetworkDespawn()
        {
            all.Remove(this);
            if (Local == this) Local = null;
            if (IsServer && RoundManager.Instance != null) RoundManager.Instance.ServerPlayersChanged();
        }

        internal void ServerSetRole(PlayerRole newRole, FriendRole newFriendRole)
        {
            role.Value = newRole;
            friendRole.Value = newFriendRole;
        }
    }
}
