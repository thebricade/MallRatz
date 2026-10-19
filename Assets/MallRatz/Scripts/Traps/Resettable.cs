using System;
using MallRatz.Net;
using UnityEngine;

namespace MallRatz.Traps
{
    // Stands in for the janitor NPC: a knocked-over prop goes back after a while, so traps can't be
    // farmed. Host only; NetworkProp carries the move to everyone else.
    [RequireComponent(typeof(Rigidbody), typeof(NetworkProp))]
    public class Resettable : MonoBehaviour
    {
        [SerializeField, Min(0)] float resetDelay = 20f;
        [SerializeField, Min(0)] float disturbedDistance = 0.3f;
        [SerializeField, Range(0, 180)] float disturbedAngle = 20f;

        Rigidbody body;
        NetworkProp prop;
        Vector3 homePosition;
        Quaternion homeRotation;
        float resetAt = -1f;

        public event Action PutBack;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            prop = GetComponent<NetworkProp>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        void Update()
        {
            if (!NetState.IsServer) return;
            bool disturbed = (transform.position - homePosition).sqrMagnitude > disturbedDistance * disturbedDistance
                || Quaternion.Angle(transform.rotation, homeRotation) > disturbedAngle;
            if (!disturbed)
            {
                resetAt = -1f;
                return;
            }

            if (resetAt < 0f) resetAt = Time.time + resetDelay;
            else if (Time.time >= resetAt) GoHome();
        }

        public void GoHome()
        {
            resetAt = -1f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = homePosition;
            body.rotation = homeRotation;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            prop.SendNow();
            PutBack?.Invoke();
        }
    }
}
