using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Checkout Frenzy/Customer/Dialogue")]
    public class Dialogue : ScriptableObject
    {
        [System.Serializable]
        private struct Line
        {
            [Tooltip("Reference to the localized string table entry.")]
            public LocalizedString LocalizedText;
        }

        [SerializeField] private List<Line> lines;

        /// <summary>
        /// Returns a random localized line from the list.
        /// </summary>
        public LocalizedString GetRandomLine()
        {
            if (lines == null || lines.Count == 0)
            {
                Debug.LogWarning("Dialogue list is empty.");
                return null;
            }

            int index = Random.Range(0, lines.Count);
            return lines[index].LocalizedText;
        }
    }
}
