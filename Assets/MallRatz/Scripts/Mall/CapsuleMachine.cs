using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Players;
using MallRatz.Round;
using MallRatz.UI;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Mall
{
    // A gacha machine. Spenders pay for a random capsule toy matching one of the shops, to bait a Friend
    // whose weakness it is. Pocket cash goes first, then the card. Everyone sees what came out. Host only.
    [RequireComponent(typeof(BoxCollider))]
    public class CapsuleMachine : NetworkBehaviour
    {
        static readonly List<CapsuleMachine> all = new();

        [SerializeField] Trinket[] capsules;
        [SerializeField, Min(0)] int price = 5;
        [SerializeField, Min(0)] float labelHeight = 2.2f;

        BoxCollider zone;
        float resultShownAt = float.NegativeInfinity;
        string resultText;

        public int Price => price;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static CapsuleMachine At(Vector3 position)
        {
            foreach (var machine in all)
                if (machine.zone.bounds.Contains(position)) return machine;
            return null;
        }

        // E at the machine. True if the press was used up here, even when the pull didn't happen.
        public bool ServerTryPull(MallRatzPlayer player)
        {
            if (player.Role != PlayerRole.Spender || player.Body.State != BodyState.Upright || player.Hands.Held != null) return false;
            if (RoundManager.Instance == null || !RoundManager.Instance.IsMallOpen) return false;

            var wallet = player.PocketCash.Balance >= price ? player.PocketCash
                : player.Hands.Card != null && player.Hands.Card.Wallet.Balance >= price ? player.Hands.Card.Wallet
                : null;
            if (wallet == null)
            {
                ResultRpc($"${price} a pull. Not enough money!");
                return true;
            }
            var capsule = System.Array.Find(capsules, candidate => !candidate.InPlay);
            if (capsule == null || ShopZone.ByName.Count == 0)
            {
                ResultRpc("Sold out! Come back later.");
                return true;
            }

            wallet.ServerTrySpend(price);
            capsule.ServerDispense(Random.Range(0, ShopZone.ByName.Count));
            capsule.ServerAttach(player.Hands, HoldSlot.Hand);
            ResultRpc($"{player.DisplayName} got a {capsule.DisplayName}!");
            return true;
        }

        [Rpc(SendTo.Everyone)]
        void ResultRpc(string text)
        {
            resultShownAt = Time.time;
            resultText = text;
        }

        void OnGUI()
        {
            if (!IsSpawned) return;
            var top = transform.position + Vector3.up * labelHeight;
            if (Time.time - resultShownAt < 3f) WorldLabel.Draw(top, resultText, new Color(1f, 0.6f, 0.9f));
            else WorldLabel.Draw(top, $"Capsule Toys ${price}", Color.white);
        }
    }
}
