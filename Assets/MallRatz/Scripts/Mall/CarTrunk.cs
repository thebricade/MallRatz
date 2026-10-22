using MallRatz.Items;
using MallRatz.Net;
using MallRatz.Round;
using MallRatz.UI;
using UnityEngine;

namespace MallRatz.Mall
{
    // Haul: the car out in the parking lot. Anything paid for that's set down or thrown in here is loaded
    // up and counts toward the day's list. The host decides; ShopItem carries it to everyone.
    [RequireComponent(typeof(BoxCollider))]
    public class CarTrunk : MonoBehaviour
    {
        [SerializeField, Min(0)] float labelHeight = 2f;

        BoxCollider zone;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void Update()
        {
            var round = RoundManager.Instance;
            if (!NetState.IsServer || round == null || !round.IsHaul || !round.IsMallOpen) return;
            foreach (var holdable in Holdable.All)
            {
                if (!(holdable is ShopItem item) || !item.IsPaid || item.IsHeld || item.IsDelivered) continue;
                if (!zone.bounds.Contains(item.transform.position)) continue;
                item.ServerDeliver();
            }
        }

        void OnGUI()
        {
            var round = RoundManager.Instance;
            if (round == null || !round.IsSpawned || !round.IsHaul) return;
            WorldLabel.Draw(transform.position + Vector3.up * labelHeight,
                $"The car: drop what you bought here\n{round.DeliveredProgress:P0} loaded of {round.Config.haulTarget:P0} needed", Color.white);
        }
    }
}
