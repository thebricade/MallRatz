using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace MallRatz.Multiplayer.Gameplay
{
    /// <summary>
    /// Enables local-only player components (camera rig, input-driven controller) for the
    /// owning client only, and disables them for every remote observer of this networked player.
    /// The scene's single shared Camera/AudioListener/CinemachineBrain is left untouched here;
    /// this only controls which player's virtual camera and input script are allowed to drive it.
    /// </summary>
    public class NetworkPlayerSetup : NetworkBehaviour
    {
        [SerializeField, Tooltip("Root GameObject of the player's camera rig (Cinemachine virtual camera, held-object camera). Active only for the owning client.")]
        private GameObject playerCameraRoot;

        [SerializeField, Tooltip("Input-driven player controller script. Enabled only for the owning client; remote copies are driven by NetworkTransform instead.")]
        private MonoBehaviour playerControllerScript;

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyOwnershipState();
        }

        public override void OnOwnershipClient(NetworkConnection previousOwner)
        {
            base.OnOwnershipClient(previousOwner);
            ApplyOwnershipState();
        }

        private void ApplyOwnershipState()
        {
            bool isLocalPlayer = IsOwner;

            if (playerCameraRoot != null) playerCameraRoot.SetActive(isLocalPlayer);
            if (playerControllerScript != null) playerControllerScript.enabled = isLocalPlayer;
        }
    }
}
