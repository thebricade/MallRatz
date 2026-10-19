using MallRatz.Net;
using MallRatz.Players;
using UnityEngine;

namespace MallRatz.Traps
{
    // Carts, cans, bowling balls: anything moving fast enough knocks a player over. Props are simulated
    // on the host, so this only runs there.
    [RequireComponent(typeof(Rigidbody))]
    public class TripOnImpact : MonoBehaviour
    {
        [Tooltip("Relative speed needed to knock a player over. Walking into a still prop never trips.")]
        [SerializeField, Min(0)] float minImpactSpeed = 3f;
        [SerializeField, Min(0)] float knockback = 3f;

        void OnCollisionEnter(Collision collision)
        {
            if (!NetState.IsServer || collision.relativeVelocity.magnitude < minImpactSpeed) return;
            var body = collision.collider.GetComponentInParent<PlayerBody>();
            if (body == null) return;

            var away = Vector3.ProjectOnPlane(body.transform.position - transform.position, Vector3.up).normalized;
            body.Trip(away * knockback + Vector3.up);
        }
    }
}
