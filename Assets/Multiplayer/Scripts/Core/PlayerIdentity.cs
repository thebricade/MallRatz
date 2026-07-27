using System;
using CryingSnow.CheckoutFrenzy.Core;

namespace MallRatz.Multiplayer.Core
{
    /// <summary>
    /// Data persisted locally on a client to identify the same player across sessions,
    /// without requiring a login/account system.
    /// </summary>
    [Serializable]
    public class PlayerIdentityData
    {
        public string PlayerId;
    }

    /// <summary>
    /// Provides a stable, locally-generated identifier for the current player.
    /// This identifier is sent to the host on connect so the host can match the
    /// connecting client to their own shop save record, since there is no
    /// login/account system in MallRatz.
    /// </summary>
    public static class PlayerIdentity
    {
        private const string IdentityFileName = "PlayerIdentity";

        private static string cachedPlayerId;

        /// <summary>
        /// The current player's persistent identifier. Generated once on first
        /// access and stored locally; the same value is returned on every
        /// subsequent run of the game on this machine.
        /// </summary>
        public static string PlayerId
        {
            get
            {
                if (string.IsNullOrEmpty(cachedPlayerId))
                {
                    cachedPlayerId = LoadOrCreatePlayerId();
                }

                return cachedPlayerId;
            }
        }

        /// <summary>
        /// Loads the persisted player identity from disk, or creates and saves a
        /// new one if none exists yet.
        /// </summary>
        private static string LoadOrCreatePlayerId()
        {
            var data = SaveSystem.LoadData<PlayerIdentityData>(IdentityFileName);

            if (data == null || string.IsNullOrEmpty(data.PlayerId))
            {
                data = new PlayerIdentityData { PlayerId = Guid.NewGuid().ToString() };
                SaveSystem.SaveData(data, IdentityFileName);
            }

            return data.PlayerId;
        }
    }
}
