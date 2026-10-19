using System.Collections.Generic;
using MallRatz.Items;
using MallRatz.Mall;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Players
{
    // One thing in hand, the card in a pocket, and a bag of paid items. Who holds what lives on each
    // Holdable and is decided by the host; the owner only sends requests.
    public class Hands : NetworkBehaviour
    {
        [SerializeField] Transform holdAnchor;
        [SerializeField] Transform cardAnchor;
        [SerializeField, Min(0)] float reach = 1.2f;
        [SerializeField, Min(0)] float throwSpeed = 8f;
        [Tooltip("Speed lost per unit of carry weight, held or bagged. Bags make you clumsy.")]
        [SerializeField, Min(0)] float slowdownPerWeight = 0.08f;
        [SerializeField, Range(0, 1)] float minSpeedMultiplier = 0.5f;

        public Holdable Held => Find(HoldSlot.Hand);
        public PaymentCard Card => Find(HoldSlot.Card) as PaymentCard;
        public bool HoldsUnpaid => Held is ShopItem item && !item.IsPaid;

        public IEnumerable<ShopItem> Bag
        {
            get
            {
                foreach (var holdable in Holdable.All)
                    if (holdable.Slot == HoldSlot.Bag && holdable.Holder == this && holdable is ShopItem item)
                        yield return item;
            }
        }

        public float Load
        {
            get
            {
                float load = Held is ShopItem held ? held.Definition.carryWeight : 0f;
                foreach (var item in Bag) load += item.Definition.carryWeight;
                return load;
            }
        }

        public float SpeedMultiplier => Mathf.Clamp(1f - Load * slowdownPerWeight, minSpeedMultiplier, 1f);

        internal Transform AnchorFor(HoldSlot slot) => slot == HoldSlot.Card ? cardAnchor : holdAnchor;

        Holdable Find(HoldSlot slot)
        {
            foreach (var holdable in Holdable.All)
                if (holdable.Slot == slot && holdable.Holder == this) return holdable;
            return null;
        }

        // Owner requests.
        public void Interact() => InteractRpc();
        public void ThrowHeld() => ThrowRpc(card: false);
        public void ThrowCard() => ThrowRpc(card: true);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void InteractRpc()
        {
            var player = GetComponent<MallRatzPlayer>();
            var register = Register.At(transform.position);
            if (register != null && register.ServerTryStartScan(player)) return;
            var desk = ReturnsDesk.At(transform.position);
            if (desk != null && desk.ServerTryStartRefund(player)) return;
            var gacha = CapsuleMachine.At(transform.position);
            if (gacha != null && gacha.ServerTryPull(player)) return;
            var stash = CardStash.At(transform.position);
            if (stash != null && stash.ServerTryUse(player)) return;
            if (Held != null) Held.ServerPutDown();
            else ServerPickUpNearest();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void ThrowRpc(bool card)
        {
            var holdable = card ? Card : Held;
            if (holdable != null) holdable.ServerDetach(transform.forward * throwSpeed + Vector3.up * 2f);
        }

        // Host only from here down.
        bool ServerPickUpNearest()
        {
            bool spender = IsSpender;
            Holdable nearest = null;
            float nearestDistance = float.MaxValue;
            var center = transform.position + transform.forward * (reach * 0.5f);
            foreach (var hit in Physics.OverlapSphere(center, reach))
            {
                var holdable = hit.attachedRigidbody ? hit.attachedRigidbody.GetComponent<Holdable>() : null;
                if (holdable == null || holdable.IsHeld) continue;
                bool rebag = spender && holdable is ShopItem { IsPaid: true };
                if (holdable is PaymentCard ? Card != null : Held != null && !rebag) continue;
                float distance = (holdable.transform.position - center).sqrMagnitude;
                if (distance < nearestDistance) (nearest, nearestDistance) = (holdable, distance);
            }

            if (nearest is PaymentCard card) return ServerTakeCard(card);
            if (nearest == null) return false;
            // Her own shopping goes straight back in her bag.
            if (spender && nearest is ShopItem { IsPaid: true } paid) ServerBag(paid);
            else nearest.ServerAttach(this, HoldSlot.Hand);
            return true;
        }

        bool IsSpender => GetComponent<MallRatzPlayer>().Role == PlayerRole.Spender;

        public bool ServerTakeCard(PaymentCard card)
        {
            if (card == null || Card != null) return false;
            card.ServerAttach(this, HoldSlot.Card);
            return true;
        }

        // On a trip: whatever is in hand and the card go flying, and the Spender's latest purchase spills
        // out of her bag, which is how Returns gets hold of something to refund.
        internal void ServerDropAll(Vector3 velocity)
        {
            if (Held != null) Held.ServerDetach(velocity);
            if (Card != null) Card.ServerDetach(velocity);
            if (!IsSpender) return;

            ShopItem latest = null;
            foreach (var item in Bag)
                if (latest == null || item.PurchaseOrder > latest.PurchaseOrder) latest = item;
            if (latest != null) latest.ServerDetach(velocity + Random.insideUnitSphere);
        }

        internal void ServerBag(ShopItem item) => item.ServerAttach(this, HoldSlot.Bag);

        public override void OnNetworkDespawn()
        {
            // Someone left: drop everything they had, bag included, where they stood.
            if (!IsServer) return;
            foreach (var holdable in Holdable.All)
                if (holdable.Holder == this) holdable.ServerDetach(Vector3.zero);
        }
    }
}
