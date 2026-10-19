using MallRatz.Players;
using UnityEngine;

namespace MallRatz.Traps
{
    // A spill. Running across it slips you; walking carefully is safe. Checked by each player's own
    // machine, since that's where their speed is known.
    [RequireComponent(typeof(Collider))]
    public class SlipperySurface : MonoBehaviour
    {
        [SerializeField, Min(0)] float minSpeed = 5f;
        [SerializeField, Min(0)] float slideSpeed = 5f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            var mover = other.GetComponent<PlaceholderMover>();
            if (mover == null || mover.Velocity.magnitude < minSpeed) return;
            var body = other.GetComponent<PlayerBody>();
            if (body.IsOwner) body.Trip(mover.Velocity.normalized * slideSpeed + Vector3.up);
        }
    }
}
