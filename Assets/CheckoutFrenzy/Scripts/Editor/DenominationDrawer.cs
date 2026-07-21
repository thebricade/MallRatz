using UnityEngine;
using UnityEditor;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Editor
{
    [CustomPropertyDrawer(typeof(Denomination))]
    public class DenominationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string indexMatch = System.Text.RegularExpressions.Regex.Match(label.text, @"\d+").Value;

            int displayIndex = 0;
            if (int.TryParse(indexMatch, out int parsedIndex))
            {
                displayIndex = parsedIndex + 1;
            }

            float indexWidth = 25f;
            Rect indexRect = new Rect(position.x, position.y, indexWidth, position.height);
            EditorGUI.LabelField(indexRect, new GUIContent(displayIndex.ToString()));

            Rect fieldRect = new Rect(position.x + indexWidth, position.y, position.width - indexWidth, position.height);

            float valueWidth = fieldRect.width * 0.3f;
            float spriteWidth = fieldRect.width * 0.7f;
            float padding = 5f;

            SerializedProperty valueProp = property.FindPropertyRelative("value");
            SerializedProperty spriteProp = property.FindPropertyRelative("sprite");

            Rect valueRect = new Rect(fieldRect.x, fieldRect.y, valueWidth - padding, fieldRect.height);
            Rect spriteRect = new Rect(fieldRect.x + valueWidth, fieldRect.y, spriteWidth, fieldRect.height);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
            EditorGUI.PropertyField(spriteRect, spriteProp, GUIContent.none);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
