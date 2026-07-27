using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using UnityEngine;

namespace MallRatz.Multiplayer.Gameplay
{
    /// <summary>
    /// Drives the transition into the shared Mall scene once the local server starts.
    /// The server loads the Mall as a FishNet global scene, which FishNet automatically
    /// replicates to every current and future connected client. Clients must not load the
    /// Mall scene through Unity's own SceneManager, since that would create an
    /// un-networked scene instance that FishNet cannot synchronize.
    /// </summary>
    public class NetworkGameFlow : MonoBehaviour
    {
        private const string MallSceneName = "Game";

        [SerializeField, Tooltip("The NetworkManager driving this session. Defaults to the NetworkManager on this GameObject.")]
        private NetworkManager networkManager;

        private bool hasLoadedMallScene;

        private void Awake()
        {
            if (networkManager == null) networkManager = GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
        }

        private void OnDisable()
        {
            networkManager.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
        }

        private void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started) return;
            if (hasLoadedMallScene) return;

            hasLoadedMallScene = true;

            var sceneLoadData = new SceneLoadData(MallSceneName)
            {
                ReplaceScenes = ReplaceOption.All
            };
            networkManager.SceneManager.LoadGlobalScenes(sceneLoadData);
        }
    }
}
