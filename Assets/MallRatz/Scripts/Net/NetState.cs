using Unity.Netcode;

namespace MallRatz.Net
{
    public static class NetState
    {
        // True on the host. Plain MonoBehaviours use this to run host-only logic (traps, the janitor).
        public static bool IsServer => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        public static double ServerTime => NetworkManager.Singleton != null ? NetworkManager.Singleton.ServerTime.Time : 0d;
    }
}
