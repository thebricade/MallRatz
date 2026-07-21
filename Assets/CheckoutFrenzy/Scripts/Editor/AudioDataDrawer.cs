using UnityEngine;
using UnityEditor;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomPropertyDrawer(typeof(AudioData))]
    public class AudioDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            float idWidth = position.width * 0.4f; // 40% for the ID field
            float clipWidth = position.width * 0.6f; // 60% for the clip field
            float padding = 5f; // Small padding between fields

            SerializedProperty idProperty = property.FindPropertyRelative("id");
            SerializedProperty clipProperty = property.FindPropertyRelative("clip");

            Rect idRect = new Rect(position.x, position.y, idWidth - padding, position.height);
            Rect clipRect = new Rect(position.x + idWidth + padding, position.y, clipWidth - padding, position.height);

            EditorGUI.PropertyField(idRect, idProperty, GUIContent.none);
            EditorGUI.PropertyField(clipRect, clipProperty, GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
