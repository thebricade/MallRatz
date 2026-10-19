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
            if (message == null || Time.time >= hideAt) return;
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
            var rect = new Rect(0f, Screen.height * 0.25f, Screen.width, 60f);
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), message, style);
            GUI.color = Color.white;
            GUI.Label(rect, message, style);
        }
    }
}
