using UnityEngine;

namespace MallRatz.UI
{
    // Outlined text floating over a spot in the world, for OnGUI. Prototype stand-in for real UI.
    public static class WorldLabel
    {
        static GUIStyle style;

        public static void Draw(Vector3 world, string text, Color color)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f) return;

            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 14 };
            var rect = new Rect(screen.x - 120f, Screen.height - screen.y - 20f, 240f, 40f);
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = Color.white;
        }
    }
}
