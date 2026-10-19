using MallRatz.Round;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MallRatz.Players
{
    // Keyboard controls for the player this machine owns. Movement is applied locally; everything else
    // is a request to the host.
    public class LocalPlayerInput : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;

        MallRatzPlayer controlled;
        PlaceholderMover mover;
        Carrier carrier;
        Pickpocket pickpocket;

        public MallRatzPlayer Controlled => controlled;

        void Update()
        {
            if (controlled != MallRatzPlayer.Local) Control(MallRatzPlayer.Local);
            var keyboard = Keyboard.current;
            if (keyboard == null || controlled == null) return;

            // Eyes shut and counting while the Friends hide her card.
            var round = RoundManager.Instance;
            if (controlled.Role == PlayerRole.Spender && round != null && round.CountdownLeft > 0f)
            {
                mover.MoveInput = Vector3.zero;
                return;
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            mover.MoveInput = CameraRelative(input);
            mover.Running = keyboard.leftShiftKey.isPressed;

            if (controlled.Body.State == BodyState.Carried && keyboard.spaceKey.wasPressedThisFrame) controlled.Body.Wriggle();
            if (controlled.Body.State != BodyState.Upright) return;
            if (keyboard.eKey.wasPressedThisFrame) controlled.Hands.Interact();
            if (keyboard.qKey.wasPressedThisFrame)
            {
                if (carrier.IsCarrying) carrier.RequestToss();
                else controlled.Hands.ThrowHeld();
            }
            if (keyboard.gKey.wasPressedThisFrame) controlled.Hands.ThrowCard();
            if (keyboard.fKey.wasPressedThisFrame) carrier.RequestGrabOrSetDown();
            if (keyboard.rKey.wasPressedThisFrame) pickpocket.Request();
            if (keyboard.tKey.wasPressedThisFrame) controlled.Willpower.Request();
        }

        void Control(MallRatzPlayer player)
        {
            controlled = player;
            mover = player != null ? player.GetComponent<PlaceholderMover>() : null;
            carrier = player != null ? player.GetComponent<Carrier>() : null;
            pickpocket = player != null ? player.GetComponent<Pickpocket>() : null;
        }

        Vector3 CameraRelative(Vector2 input)
        {
            if (cameraTransform == null) return new Vector3(input.x, 0f, input.y);
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return forward * input.y + right * input.x;
        }
    }
}
