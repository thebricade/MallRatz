using MallRatz.Net;
using MallRatz.Players;
using UnityEngine;

namespace MallRatz.Traps
{
    // A gumball (or marble, or bouncy ball). Loose ones roll everywhere, and running over one takes your
    // feet out; walking carefully just kicks it along. Checked by the runner's own machine, like a spill.
    // Inside an intact GumballMachine it's hidden and parked.
    [RequireComponent(typeof(Rigidbody), typeof(NetworkProp))]
    public class Gumball : MonoBehaviour
    {
        [SerializeField, Min(0)] float minSpeed = 5f;
        [SerializeField, Min(0)] float slideSpeed = 4f;
        [Tooltip("Caps how fast a kick can send it. Gumballs are light, so a shove would launch them.")]
        [SerializeField, Min(0)] float maxSpeed = 7f;

        Rigidbody body;
        NetworkProp prop;
        Collider[] colliders;
        Renderer[] renderers;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            prop = GetComponent<NetworkProp>();
            colliders = GetComponentsInChildren<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
            body.maxLinearVelocity = maxSpeed;
            SetLoose(false);
        }

        internal void SetLoose(bool loose)
        {
            prop.Suspended = !loose;
            body.isKinematic = !loose || !NetState.IsServer;
            foreach (var c in colliders) c.enabled = loose;
            foreach (var r in renderers) r.enabled = loose;
        }

        internal void SteppedOn(PlaceholderMover mover)
        {
            if (mover.Velocity.magnitude < minSpeed) return;
            var runner = mover.GetComponent<PlayerBody>();
            if (runner.IsOwner) runner.Trip(mover.Velocity.normalized * slideSpeed + Vector3.up);
        }

        // Host only from here down.
        internal void ServerRelease(Vector3 position, Vector3 velocity)
        {
            transform.position = position;
            body.position = position;
            SetLoose(true);
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 10f;
            prop.SendNow();
        }

        internal void ServerPark(Vector3 position)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            SetLoose(false);
            transform.position = position;
            body.position = position;
            prop.SendNow();
        }
    }
}
