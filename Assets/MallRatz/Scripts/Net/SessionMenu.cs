using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace MallRatz.Net
{
    // Host online and share the join code, or join with one. Relay means no port forwarding.
    // The local buttons skip Unity's services for testing two copies on one PC or a LAN.
    public class SessionMenu : MonoBehaviour
    {
        const ushort LocalPort = 7777;

        [SerializeField, Min(2)] int maxPlayers = 6;

        string joinCode = "";
        string localAddress = "127.0.0.1";
        string status = "";
        bool busy;
        ISession session;
        GUIStyle stampStyle;

        void Awake() => Application.runInBackground = true;

        // Everyone sends their build stamp when connecting, and the host turns away mismatched builds.
        void Start()
        {
            var network = NetworkManager.Singleton;
            if (network == null) return;
            network.NetworkConfig.ConnectionApproval = true;
            network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(BuildStamp.Payload);
            network.ConnectionApprovalCallback = Approve;
            network.OnClientDisconnectCallback += OnDisconnected;
        }

        void OnDestroy()
        {
            var network = NetworkManager.Singleton;
            if (network == null) return;
            network.ConnectionApprovalCallback = null;
            network.OnClientDisconnectCallback -= OnDisconnected;
        }

        static void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            string payload = request.Payload != null ? Encoding.UTF8.GetString(request.Payload) : "";
            bool matches = BuildStamp.Matches(payload, out string theirs);
            response.Approved = matches;
            response.CreatePlayerObject = matches;
            if (!matches)
                response.Reason = $"Wrong version: the host is on {BuildStamp.Commit} and you're on {theirs}. Grab the host's latest zip.";
        }

        // Turned away (or the host left): say why, and leave the session so Join works again.
        void OnDisconnected(ulong clientId)
        {
            var network = NetworkManager.Singleton;
            if (network.IsServer || clientId != network.LocalClientId) return;
            status = string.IsNullOrEmpty(network.DisconnectReason) ? "Disconnected." : network.DisconnectReason;
            if (session != null)
            {
                _ = session.LeaveAsync();
                session = null;
            }
        }

        void OnGUI()
        {
            var network = NetworkManager.Singleton;
            if (network == null) return;

            stampStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight };
            GUI.Label(new Rect(Screen.width - 410, Screen.height - 24, 400, 20), BuildStamp.Label, stampStyle);

            GUILayout.BeginArea(new Rect(Screen.width - 290, 10, 280, 300));
            GUILayout.BeginVertical(GUI.skin.box);

            if (busy)
            {
                GUILayout.Label(status);
            }
            else if (!network.IsListening)
            {
                if (GUILayout.Button("Host online")) _ = HostAsync();
                GUILayout.BeginHorizontal();
                joinCode = GUILayout.TextField(joinCode, 12).ToUpperInvariant();
                if (GUILayout.Button("Join", GUILayout.Width(60))) _ = JoinAsync(joinCode.Trim());
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label("Same PC or LAN, no Relay:");
                localAddress = GUILayout.TextField(localAddress, 40);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host local")) StartLocal(host: true);
                if (GUILayout.Button("Join local")) StartLocal(host: false);
                GUILayout.EndHorizontal();
            }
            else
            {
                if (session != null)
                {
                    GUILayout.Label($"Join code: {session.Code}");
                    if (GUILayout.Button("Copy code")) GUIUtility.systemCopyBuffer = session.Code;
                }
                GUILayout.Label(network.IsHost ? $"Hosting, {network.ConnectedClientsIds.Count} connected" : "Connected");
                if (GUILayout.Button("Leave")) _ = LeaveAsync();
            }

            if (!busy && !string.IsNullOrEmpty(status)) GUILayout.Label(status);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        async Task HostAsync()
        {
            await RunAsync("Creating a session...", async () =>
            {
                await SignInAsync();
                var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = true }.WithRelayNetwork();
                session = await MultiplayerService.Instance.CreateSessionAsync(options);
            });
        }

        async Task JoinAsync(string code)
        {
            if (code.Length == 0)
            {
                status = "Type the host's join code first.";
                return;
            }
            await RunAsync("Joining...", async () =>
            {
                await SignInAsync();
                session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            });
        }

        async Task LeaveAsync()
        {
            await RunAsync("Leaving...", async () =>
            {
                if (session != null) await session.LeaveAsync();
                else NetworkManager.Singleton.Shutdown();
                session = null;
            });
        }

        void StartLocal(bool host)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData(localAddress.Trim(), LocalPort, host ? "0.0.0.0" : null);
            bool started = host ? NetworkManager.Singleton.StartHost() : NetworkManager.Singleton.StartClient();
            status = started ? "" : "Couldn't start. Is something else using port 7777?";
        }

        async Task RunAsync(string message, Func<Task> work)
        {
            busy = true;
            status = message;
            try
            {
                await work();
                status = "";
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                status = e.Message;
            }
            busy = false;
        }

        static async Task SignInAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                // A fresh anonymous profile per launch, so two copies on one PC count as different players.
                var options = new InitializationOptions().SetProfile($"p{UnityEngine.Random.Range(0, 1_000_000)}");
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
