using MallRatz.Players;
using MallRatz.Round;
using UnityEngine;

namespace MallRatz.UI
{
    // Who's who: tints the body by role and floats a name tag overhead.
    [RequireComponent(typeof(MallRatzPlayer))]
    public class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] Renderer bodyRenderer;
        [SerializeField, Min(0)] float tagHeight = 1.5f;

        MallRatzPlayer player;
        MaterialPropertyBlock block;
        (PlayerRole, FriendRole)? shown;
        GUIStyle tagStyle;

        void Awake()
        {
            player = GetComponent<MallRatzPlayer>();
            block = new MaterialPropertyBlock();
            if (bodyRenderer == null) bodyRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            var current = (player.Role, player.FriendRole);
            if (shown == current) return;
            shown = current;

            var color = ColorFor(player.Role, player.FriendRole);
            bodyRenderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            bodyRenderer.SetPropertyBlock(block);
        }

        void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null || !player.IsSpawned) return;
            var screen = camera.WorldToScreenPoint(transform.position + Vector3.up * tagHeight);
            if (screen.z <= 0f) return;

            tagStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            var round = RoundManager.Instance;
            string roleName = round != null && round.IsHaul ? "Shopper"
                : player.Willpower.IsTurned ? "Gave in"
                : RoleName(player.Role, player.FriendRole);
            var text = $"{player.DisplayName}{(player.IsOwner ? " (you)" : "")}\n{roleName}";
            var rect = new Rect(screen.x - 90f, Screen.height - screen.y - 20f, 180f, 40f);
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, tagStyle);
            GUI.color = ColorFor(player.Role, player.FriendRole);
            GUI.Label(rect, text, tagStyle);
            GUI.color = Color.white;
        }

        public static Color ColorFor(PlayerRole role, FriendRole friendRole) => role == PlayerRole.Spender
            ? new Color(1f, 0.35f, 0.7f)
            : friendRole switch
            {
                FriendRole.Bouncer => new Color(0.25f, 0.5f, 1f),
                FriendRole.Lookout => new Color(0.3f, 0.85f, 0.4f),
                FriendRole.Returns => new Color(1f, 0.8f, 0.2f),
                FriendRole.VoiceOfReason => new Color(0.65f, 0.45f, 1f),
                _ => new Color(0.6f, 0.6f, 0.6f),
            };

        public static string RoleName(PlayerRole role, FriendRole friendRole) => role == PlayerRole.Spender
            ? "Spender"
            : friendRole switch
            {
                FriendRole.VoiceOfReason => "Voice of Reason",
                FriendRole.None => "Friend",
                _ => friendRole.ToString(),
            };
    }
}
