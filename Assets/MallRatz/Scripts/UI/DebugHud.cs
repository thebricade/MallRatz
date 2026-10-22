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
            if (round.IsHaul)
            {
                GUILayout.Label($"Haul, day {round.Day}   {round.Phase}   {seconds / 60}:{seconds % 60:00}");
                GUILayout.Label($"In the car: {round.DeliveredProgress:P0} of the list's value ({round.Config.haulTarget:P0} needed)");
                GUILayout.Label($"Team budget ${round.Wallet.Balance}");
                if (round.IsMallOpen || round.Phase == RoundPhase.Results)
                {
                    GUILayout.Label("Shopping list:");
                    foreach (var wanted in round.ShoppingList)
                        GUILayout.Label($"  {wanted.displayName} (${wanted.price}): {HaulStatus(wanted)}");
                }
            }
            else
            {
                GUILayout.Label($"{round.Phase}   {seconds / 60}:{seconds % 60:00}");
                GUILayout.Label($"Wish list {round.WishListProgress:P0} of {round.Config.wishListTarget:P0}");
                GUILayout.Label($"Spender's wallet ${round.Wallet.Balance} of ${round.Wallet.StartingBalance}");
            }

            GUILayout.Space(6);
            foreach (var player in MallRatzPlayer.All)
            {
                string role = round.IsHaul ? "Shopper"
                    : player.Role == PlayerRole.Friend ? $"Friend ({player.FriendRole})"
                    : player.Willpower.IsTurned ? "Spender (gave in)" : "Spender";
                string you = player.IsOwner ? " (you)" : "";
                GUILayout.Label($"{player.DisplayName}{you}: {role}, {player.Body.State}");
            }

            if (round.Phase == RoundPhase.Results)
            {
                GUILayout.Space(6);
                if (round.IsHaul) GUILayout.Label(round.HaulSucceeded ? $"Day {round.Day} done! Next day's list is longer." : "The mall closed on you. Back to day 1.");
                else GUILayout.Label(round.Winner == PlayerRole.Spender ? "The Spender wins." : "The Friends win.");
                GUILayout.Label("Receipt of shame:");
                foreach (var item in round.Receipt) GUILayout.Label($"  {item.Definition.displayName}  ${item.Definition.price}{Chipped(item)}");
            }

            if (network.IsServer)
            {
                GUILayout.Space(6);
                if (round.Phase == RoundPhase.Lobby)
                {
                    var other = round.IsHaul ? GameMode.Shopaholic : GameMode.Haul;
                    GUILayout.Label($"Mode: {(round.IsHaul ? "Haul (co-op)" : "Shopaholic (Spender vs Friends)")}");
                    if (GUILayout.Button($"Switch to {(other == GameMode.Haul ? "Haul (co-op)" : "Shopaholic")}")) round.ServerSetMode(other);
                    if (GUILayout.Button(round.IsHaul ? $"Start day {round.Day}" : "Start round")) round.ServerStartRound();
                }
                else
                {
                    string next = !round.IsHaul ? "Reset to lobby (next Spender)"
                        : round.HaulSucceeded ? $"On to day {round.Day + 1}"
                        : round.Phase == RoundPhase.Results ? "Start over at day 1"
                        : "Back to the lobby (same day)";
                    if (GUILayout.Button(next)) round.ServerNextRound();
                }
            }
            else if (round.Phase == RoundPhase.Lobby)
            {
                GUILayout.Label("Waiting for the host to start the round.");
            }

            var me = input != null ? input.Controlled : null;
            if (me != null)
            {
                GUILayout.Space(6);
                var stash = round.Card != null ? round.Card.StashedIn : null;
                string cardState = me.Hands.Card ? "yes" : me.Role == PlayerRole.Friend && stash != null ? $"hidden in the {stash.Label.ToLower()}" : "no";
                GUILayout.Label($"Hand: {Describe(me.Hands.Held)}   Card: {cardState}{(me.IsInShop ? "   In a shop" : "")}");
                var register = Register.At(me.transform.position);
                if (register != null && register.IsBusy) GUILayout.Label($"Scanning {register.Progress01:P0}");
                if (me.Body.State == BodyState.Carried) GUILayout.Label($"Mash Space to wriggle free ({me.Body.WrigglesLeft} left)");
                var grabHint = me.GetComponent<Carrier>().GrabHint();
                if (grabHint != null) GUILayout.Label(grabHint);
                if (round.IsMallOpen) GUILayout.Label(me.Willpower.Hint());
                var pocketHint = me.GetComponent<Pickpocket>().Hint();
                if (pocketHint != null) GUILayout.Label(pocketHint);
                foreach (var player in MallRatzPlayer.All)
                    if (player.GetComponent<Pickpocket>().Victim == me) GUILayout.Label("Someone's in your pocket! Turn around!");
                var gacha = CapsuleMachine.At(me.transform.position);
                if (gacha != null && me.Role == PlayerRole.Spender)
                    GUILayout.Label(me.Hands.Held == null ? $"E to pull a capsule toy (${gacha.Price})" : "Empty your hands to pull a capsule toy");
                if (me.Hands.Held is Trinket && me.Role == PlayerRole.Spender)
                    GUILayout.Label("Wave it near Friends: whoever it matches will stare. T near them makes it a gift.");
                if (ReturnsDesk.At(me.transform.position) != null && me.Hands.Held is ShopItem { IsPaid: true })
                    GUILayout.Label(ReturnsDesk.CanRefund(me) ? "E to refund it" : "Only Returns can refund here");
            }

            GUILayout.Space(6);
            GUILayout.Label("WASD move, Shift run, E pick up / set down / scan at a register");
            GUILayout.Label("Q throw (or toss who you're carrying), G throw the card");
            GUILayout.Label("F grab / set down the Spender (Bouncer only), Space wriggle free");
            GUILayout.Label("E at a stash: Friends hide or take the card, the Spender searches");
            GUILayout.Label("R pickpocket the card from behind (Spender)");
            GUILayout.Label("T: Spenders tempt a Friend; Friends talk someone who gave in back round");
            GUILayout.Label("E at the capsule machine: pull a toy that baits a Friend's weakness (Spenders)");
            GUILayout.Label("E at Customer Service with something she bought: refund it (Returns)");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        static string Describe(Holdable held) => held switch
        {
            null => "empty",
            ShopItem item => $"{item.Definition.displayName} (${item.Definition.price}){Chipped(item)}{(item.Definition.bulky ? ", too heavy to run" : "")}",
            Trinket toy => toy.DisplayName,
            _ => held.name,
        };

        // Where one thing on the Haul list has got to, best case first.
        static string HaulStatus(ItemDefinition wanted)
        {
            string status = "still in the shop";
            foreach (var holdable in Holdable.All)
            {
                if (!(holdable is ShopItem item) || item.Definition != wanted) continue;
                if (item.IsDelivered) return $"in the car{Chipped(item)}";
                if (item.IsPaid) status = item.IsHeld ? "paid, carry it to the car" : "paid, lying around";
                else if (item is FragileItem { IsBroken: true } && status == "still in the shop") status = "broken!";
            }
            return status;
        }

        static string Chipped(ShopItem item) => item.Worth < 1f ? $", {item.Worth:P0} intact" : "";
    }
}
