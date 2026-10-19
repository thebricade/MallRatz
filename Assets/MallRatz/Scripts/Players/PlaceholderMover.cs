using MallRatz.Net;
using UnityEngine;

namespace MallRatz.Players
{
    // Stand-in until Starter Assets – Third Person goes in. Input sets MoveInput; this applies it.
    // Only runs on the machine that owns the player.
    [RequireComponent(typeof(CharacterController), typeof(MallRatzPlayer))]
    public class PlaceholderMover : MonoBehaviour
    {
        [SerializeField, Min(0)] float walkSpeed = 4f;
        [SerializeField, Min(0)] float runSpeed = 6.5f;
        [Tooltip("The Bouncer walks slower than everyone else.")]
        [SerializeField, Range(0, 1)] float bouncerSpeedMultiplier = 0.85f;
        [SerializeField, Min(0)] float turnSpeed = 720f;
        [SerializeField] float gravity = -20f;
        [Tooltip("How hard walking into carts, cans and cups shoves them.")]
        [SerializeField, Min(0)] float pushForce = 300f;

        CharacterController controller;
        MallRatzPlayer player;
        float verticalSpeed;

        public Vector3 MoveInput { get; set; }
        public bool Running { get; set; }
        public Vector3 Velocity { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            player = GetComponent<MallRatzPlayer>();
        }

        void Update()
        {
            if (!player.IsOwner || !controller.enabled)
            {
                Velocity = Vector3.zero;
                verticalSpeed = 0f;
                return;
            }

            float speed = (Running ? runSpeed : walkSpeed) * player.Hands.SpeedMultiplier;
            if (player.FriendRole == FriendRole.Bouncer) speed *= bouncerSpeedMultiplier;
            var planar = Vector3.ClampMagnitude(MoveInput, 1f) * speed;

            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -1f;
            verticalSpeed += gravity * Time.deltaTime;
            controller.Move((planar + Vector3.up * verticalSpeed) * Time.deltaTime);
            Velocity = planar;

            if (planar.sqrMagnitude > 0.01f)
            {
                var facing = Quaternion.LookRotation(planar);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.rigidbody;
            if (!player.IsOwner || rb == null || hit.moveDirection.y < -0.3f) return;
            var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z) * pushForce * Time.deltaTime;
            // Props are simulated on the host, so clients ask it to do the shoving.
            var prop = rb.GetComponent<NetworkProp>();
            if (prop != null) prop.Push(push, hit.point);
            else if (!rb.isKinematic) rb.AddForceAtPosition(push, hit.point, ForceMode.Impulse);
        }
    }
}
