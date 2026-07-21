using UnityEngine;
using UnityEngine.Localization.Components;
using TMPro;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class BoxInfoRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private LocalizeStringEvent labelLocalizer;

        public void Setup(string key, string value)
        {
            labelLocalizer.StringReference.SetReference("UI", key);
            valueText.text = $": {value}";
        }
    }
}
