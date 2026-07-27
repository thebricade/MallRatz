using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class DayTimeDisplay : MonoBehaviour
    {
        [SerializeField] private LocalizedString dayTimeFormat;

        private TMP_Text displayText;

        private void Start()
        {
            displayText = GetComponent<TMP_Text>();

            TimeEvents.OnMinutePassed += UpdateDisplay;

            UpdateDisplay();

            dayTimeFormat.StringChanged += OnStringChanged;
        }

        private void OnDestroy()
        {
            dayTimeFormat.StringChanged -= OnStringChanged;
            TimeEvents.OnMinutePassed -= UpdateDisplay;
        }

        private void OnStringChanged(string value)
        {
            displayText.text = $"<mspace=0.7em>{value}";
        }

        private void UpdateDisplay()
        {
            int day = ClientShopContext.Instance.MyStore?.DataManager.Data.TotalDays ?? 0;
            string time = TimeManager.Instance.GetFormattedTime();

            dayTimeFormat.Arguments = new object[] { day, time };

            dayTimeFormat.RefreshString();
        }
    }
}
