using MallRatz.Players;
using UnityEngine;

namespace MallRatz.UI
{
    // Fixed-angle follow. Cinemachine replaces this once there's a real controller.
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] LocalPlayerInput input;
        [SerializeField] Vector3 offset = new(0f, 7f, -8f);
        [SerializeField, Min(0)] float smoothing = 6f;

        void LateUpdate()
        {
            if (input == null || input.Controlled == null) return;
            var target = input.Controlled.transform.position;
            transform.position = Vector3.Lerp(transform.position, target + offset, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            transform.LookAt(target + Vector3.up);
        }
    }
}
