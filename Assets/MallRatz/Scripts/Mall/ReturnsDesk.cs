using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Players;
using MallRatz.Round;
using MallRatz.UI;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Mall
{
    // Customer service. Returns stands here holding something the Spender bought, and after a while the
    // money goes back on her card and the item goes back on its shelf. Damaged goods refund less. One
    // item at a time; it stops if they leave, trip, or lose the item. Host only; everyone sees it.
    [RequireComponent(typeof(BoxCollider))]
    public class ReturnsDesk : NetworkBehaviour
    {
        static readonly List<ReturnsDesk> all = new();

        [SerializeField, Min(0)] float refundSeconds = 3f;
        [SerializeField, Min(0)] float labelHeight = 2.2f;

        readonly NetworkVariable<float> progress = new();

        BoxCollider zone;
        MallRatzPlayer clerk;
        ShopItem item;
        float resultShownAt = float.NegativeInfinity;
        string resultText;

        public bool IsBusy => progress.Value > 0f;
        public float Progress01 => progress.Value;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static ReturnsDesk At(Vector3 position)
        {
            foreach (var desk in all)
                if (desk.zone.bounds.Contains(position)) return desk;
            return null;
        }

        public static bool CanRefund(MallRatzPlayer player) =>
            player.FriendRole == FriendRole.Returns && player.Hands.Held is ShopItem { IsPaid: true };

        public bool ServerTryStartRefund(MallRatzPlayer player)
        {
            if (clerk != null || player.Body.State != BodyState.Upright || !CanRefund(player)) return false;
            if (RoundManager.Instance == null || !RoundManager.Instance.IsMallOpen) return false;
            clerk = player;
            item = (ShopItem)player.Hands.Held;
            progress.Value = 0.01f;
            return true;
        }

        void Update()
        {
            if (!IsServer || clerk == null) return;
            bool still = clerk.Body.State == BodyState.Upright
                && zone.bounds.Contains(clerk.transform.position)
                && clerk.Hands.Held == item
                && item.IsPaid;
            if (!still)
            {
                Stop();
                return;
            }

            progress.Value = refundSeconds > 0f ? Mathf.Min(1f, progress.Value + Time.deltaTime / refundSeconds) : 1f;
            if (progress.Value < 1f) return;

            int refund = Mathf.RoundToInt(item.Definition.price * item.Worth);
            (item.PaidFrom != null ? item.PaidFrom : RoundManager.Instance.Wallet).ServerRefund(refund);
            RefundedRpc(item.Definition.displayName, refund);
            item.ServerReturnHome();
            Stop();
        }

        void Stop()
        {
            clerk = null;
            item = null;
            progress.Value = 0f;
        }

        [Rpc(SendTo.Everyone)]
        void RefundedRpc(string itemName, int refund)
        {
            resultShownAt = Time.time;
            resultText = $"Refunded {itemName}: +${refund}";
        }

        void OnGUI()
        {
            if (!IsSpawned) return;
            var top = transform.position + Vector3.up * labelHeight;
            if (Time.time - resultShownAt < 3f) WorldLabel.Draw(top, resultText, Color.green);
            else if (IsBusy) WorldLabel.Draw(top, $"Processing return... {progress.Value:P0}", Color.yellow);
            else WorldLabel.Draw(top, "Customer Service", Color.white);
        }
    }
}
