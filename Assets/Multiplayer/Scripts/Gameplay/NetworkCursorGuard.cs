using FishNet;
using UnityEngine;

namespace MallRatz.Multiplayer.Gameplay
{
    /// <summary>
    /// Keeps the cursor free until the local machine actually starts a FishNet client.
    /// The Mall scene's UIManager locks the cursor as soon as it loads, regardless of
    /// whether this machine has a locally-owned player yet. On a server-only host (before
    /// clicking "Client"), that would trap the cursor and block the connection HUD. This
    /// component forces the cursor to stay visible and unlocked while no local client is
    /// running, then steps aside once gameplay UI should take over.
    /// </summary>
    public class NetworkCursorGuard : MonoBehaviour
    {
        /// <summary>
        /// Bootstraps this guard automatically at startup so it does not need to be wired
        /// into any scene or prefab by hand, and survives every scene load.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var guardObject = new GameObject(nameof(NetworkCursorGuard));
            guardObject.AddComponent<NetworkCursorGuard>();
            DontDestroyOnLoad(guardObject);
        }

        private void Update()
        {
            if (InstanceFinder.IsClientStarted) return;

            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }
    }
}
