using UnityEditor;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomEditor(typeof(Mission))]
    public class MissionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "You should modify this mission from:\n" +
                "Tools > Checkout Frenzy > Mission Manager.\n" +
                "DO NOT delete these missions directly!\n" +
                "Modify or delete them from the Mission Manager tool.",
                MessageType.Info);

            // Disable GUI to make all fields read-only
            GUI.enabled = false;

            // Display the mission fields as read-only
            Mission mission = (Mission)target;

            // Show the relevant fields for the mission in a read-only mode
            EditorGUILayout.IntField("Mission ID", mission.missionId);
            EditorGUILayout.EnumPopup("Goal Type", mission.goalType);
            EditorGUILayout.IntField("Target ID", mission.targetId);
            EditorGUILayout.IntField("Goal Amount", mission.goalAmount);
            EditorGUILayout.IntField("Reward", mission.reward);

            GUI.enabled = true;
        }
    }
}
