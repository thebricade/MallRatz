using MallRatz.Players;
using MallRatz.Round;
using UnityEngine;

namespace MallRatz.UI
{
    // Big centre-screen messages when the round changes phase: the PA announcements, for now.
    public class RoundBanner : MonoBehaviour
    {
        [SerializeField, Min(0)] float showSeconds = 3f;
        [SerializeField, Min(0)] float resultSeconds = 6f;

        RoundPhase? lastPhase;
        string message;
        float hideAt;
        GUIStyle style;

        void Update()
        {
            var round = RoundManager.Instance;
            if (round == null || !round.IsSpawned || round.Config == null)
            {
                lastPhase = null;
                return;
            }
            if (lastPhase == round.Phase) return;

            // No banner for whatever phase we happened to join in.
            bool joining = lastPhase == null;
            lastPhase = round.Phase;
            if (joining) return;

            message = round.Phase switch
            {
                RoundPhase.Shopping => "The mall is open!",
                RoundPhase.Closing => $"The mall closes in {round.Config.closingSeconds:0} seconds!",
                RoundPhase.Results => round.Winner == PlayerRole.Spender ? "The Spender got her haul!" : "The Friends saved her wallet!",
                _ => "Back to the lobby. New Spender!",
            };
            hideAt = Time.time + (round.Phase == RoundPhase.Results ? resultSeconds : showSeconds);
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
            Countdown();
            if (message == null || Time.time >= hideAt) return;
            Shadowed(new Rect(0f, Screen.height * 0.25f, Screen.width, 60f), message);
        }

        // Hide and seek: the Spender's screen goes dark while she counts; the Friends get a hurry-up.
        void Countdown()
        {
            var round = RoundManager.Instance;
            var me = MallRatzPlayer.Local;
            if (round == null || !round.IsSpawned || me == null || round.CountdownLeft <= 0f) return;
            int seconds = Mathf.CeilToInt(round.CountdownLeft);

            if (me.Role == PlayerRole.Spender)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                Shadowed(new Rect(0f, Screen.height * 0.4f, Screen.width, 60f), $"Counting to ten... {seconds}");
                Shadowed(new Rect(0f, Screen.height * 0.4f + 60f, Screen.width, 60f), "Your Friends are hiding your card.");
            }
            else Shadowed(new Rect(0f, Screen.height * 0.12f, Screen.width, 60f), $"Hide the card! She opens her eyes in {seconds}");
        }

        void Shadowed(Rect rect, string text)
        {
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            GUI.color = Color.white;
            GUI.Label(rect, text, style);
        }
    }
}
