using System;
using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Players;
using MallRatz.Round;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Mall
{
    // Stand in the zone holding an unpaid item and the card, then scan. Scanning takes time and stops if
    // you leave, trip, or lose the item or card. The purchase only commits once it's bagged. Host only;
    // everyone sees the progress.
    [RequireComponent(typeof(BoxCollider))]
    public class Register : NetworkBehaviour
    {
        static readonly List<Register> all = new();

        // The Lookout's card ping hooks in here (host side).
        public static event Action<Register, MallRatzPlayer> ScanStarted;
        public static event Action<MallRatzPlayer, ShopItem> Purchased;

        [SerializeField, Min(0)] float scanSeconds = 2f;
        [Tooltip("How late Friends hear about a scan. The Lookout hears at once.")]
        [SerializeField, Min(0)] float pingDelay = 2f;
        [SerializeField, Min(0)] float pingSeconds = 4f;

        readonly NetworkVariable<float> progress = new();

        BoxCollider zone;
        MallRatzPlayer scanner;
        ShopItem item;
        float pingedAt = float.NegativeInfinity;
        GUIStyle pingStyle;

        public bool IsBusy => progress.Value > 0f;
        public float Progress01 => progress.Value;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static Register At(Vector3 position)
        {
            foreach (var register in all)
                if (register.zone.bounds.Contains(position)) return register;
            return null;
        }

        public bool ServerTryStartScan(MallRatzPlayer player)
        {
            if (scanner != null || player.Body.State != BodyState.Upright) return false;
            if (RoundManager.Instance != null && !RoundManager.Instance.IsMallOpen) return false;
            if (!(player.Hands.Held is ShopItem held) || held.IsPaid) return false;
            var wallet = WalletFor(player);
            if (wallet == null || wallet.Balance < held.Definition.price) return false;

            scanner = player;
            item = held;
            progress.Value = 0.01f;
            // Cash leaves no trail: only the card pings.
            if (player.Hands.Card != null)
            {
                ScanStarted?.Invoke(this, player);
                CardPingRpc();
            }
            return true;
        }

        // The card if they hold it, otherwise a Spender's own cash (hers, or a Friend's who gave in).
        static Wallet WalletFor(MallRatzPlayer player)
        {
            var card = player.Hands.Card;
            if (card != null) return card.Wallet;
            return player.Role == PlayerRole.Spender ? player.PocketCash : null;
        }

        [Rpc(SendTo.Everyone)]
        void CardPingRpc() => pingedAt = Time.time;

        // The card ping: Friends see where the card was just used, clamped to the screen edge if it's off-screen.
        void OnGUI()
        {
            var me = MallRatzPlayer.Local;
            var camera = Camera.main;
            if (me == null || me.Role != PlayerRole.Friend || camera == null) return;
            float age = Time.time - pingedAt - (me.FriendRole == FriendRole.Lookout ? 0f : pingDelay);
            if (age < 0f || age > pingSeconds || (int)(age * 4f) % 2 == 1) return;

            var screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screen.z < 0f) screen = -screen;
            const float margin = 60f;
            float x = Mathf.Clamp(screen.x, margin, Screen.width - margin);
            float y = Mathf.Clamp(Screen.height - screen.y, margin, Screen.height - margin);

            pingStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 };
            var shop = ShopZone.At(transform.position);
            GUI.color = Color.yellow;
            GUI.Label(new Rect(x - 80f, y - 25f, 160f, 50f), $"CARD PING\n{(shop != null ? shop.ShopName : "Register")}", pingStyle);
            GUI.color = Color.white;
        }

        void Update()
        {
            if (!IsServer || scanner == null) return;
            if (!StillScanning())
            {
                Stop();
                return;
            }

            progress.Value = scanSeconds > 0f ? Mathf.Min(1f, progress.Value + Time.deltaTime / scanSeconds) : 1f;
            if (progress.Value < 1f) return;

            var buyer = scanner;
            var bought = item;
            Stop();
            var wallet = WalletFor(buyer);
            if (!wallet.ServerTrySpend(bought.Definition.price)) return;
            bought.PaidFrom = wallet;
            buyer.Hands.ServerBag(bought);
            Purchased?.Invoke(buyer, bought);
        }

        bool StillScanning() =>
            scanner != null
            && scanner.Body.State == BodyState.Upright
            && zone.bounds.Contains(scanner.transform.position)
            && scanner.Hands.Held == item
            && WalletFor(scanner) != null;

        void Stop()
        {
            scanner = null;
            item = null;
            progress.Value = 0f;
        }
    }
}
