using MallRatz.Items;
using MallRatz.Mall;
using MallRatz.Players;
using MallRatz.Round;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.UI
{
    // Throwaway prototype readout: round state, who's who, your player, the controls, and the host's buttons.
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] LocalPlayerInput input;

        void OnGUI()
        {
            var round = RoundManager.Instance;
            var network = NetworkManager.Singleton;
            if (round != null && round.Config == null)
            {
                GUI.Label(new Rect(10, 10, 500, 40), "The Round Manager has no config. Run MallRatz → Build Greybox Scene.");
                return;
            }
            if (round == null || network == null || !network.IsListening || !round.IsSpawned) return;

            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20));
            GUILayout.BeginVertical(GUI.skin.box);

            int seconds = Mathf.CeilToInt(round.TimeLeft);
            GUILayout.Label($"{round.Phase}   {seconds / 60}:{seconds % 60:00}");
            GUILayout.Label($"Wish list {round.WishListProgress:P0} of {round.Config.wishListTarget:P0}");
            GUILayout.Label($"Spender's wallet ${round.Wallet.Balance} of ${round.Wallet.StartingBalance}");

            GUILayout.Space(6);
            foreach (var player in MallRatzPlayer.All)
            {
                string role = player.Role == PlayerRole.Friend ? $"Friend ({player.FriendRole})" : "Spender";
                string you = player.IsOwner ? " (you)" : "";
                GUILayout.Label($"{player.DisplayName}{you}: {role}, {player.Body.State}");
            }

            if (round.Phase == RoundPhase.Results)
            {
                GUILayout.Space(6);
                GUILayout.Label(round.Winner == PlayerRole.Spender ? "The Spender wins." : "The Friends win.");
                GUILayout.Label("Receipt of shame:");
                foreach (var item in round.Receipt) GUILayout.Label($"  {item.Definition.displayName}  ${item.Definition.price}");
            }

            if (network.IsServer)
            {
                GUILayout.Space(6);
                if (round.Phase == RoundPhase.Lobby && GUILayout.Button("Start round")) round.ServerStartRound();
                if (round.Phase != RoundPhase.Lobby && GUILayout.Button("Reset to lobby (next Spender)")) round.ServerNextRound();
            }
            else if (round.Phase == RoundPhase.Lobby)
            {
                GUILayout.Label("Waiting for the host to start the round.");
            }

            var me = input != null ? input.Controlled : null;
            if (me != null)
            {
                GUILayout.Space(6);
                GUILayout.Label($"Hand: {Describe(me.Hands.Held)}   Card: {(me.Hands.Card ? "yes" : "no")}{(me.IsInShop ? "   In a shop" : "")}");
                var register = Register.At(me.transform.position);
                if (register != null && register.IsBusy) GUILayout.Label($"Scanning {register.Progress01:P0}");
                if (me.Body.State == BodyState.Carried) GUILayout.Label($"Mash Space to wriggle free ({me.Body.WrigglesLeft} left)");
                var grabHint = me.GetComponent<Carrier>().GrabHint();
                if (grabHint != null) GUILayout.Label(grabHint);
            }

            GUILayout.Space(6);
            GUILayout.Label("WASD move, Shift run, E pick up / drop / scan at a register");
            GUILayout.Label("Q throw (or toss who you're carrying), G throw the card");
            GUILayout.Label("F grab / set down the Spender (Bouncer only), Space wriggle free");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        static string Describe(Holdable held) => held switch
        {
            null => "empty",
            ShopItem item => $"{item.Definition.displayName} (${item.Definition.price})",
            _ => held.name,
        };
    }
}
