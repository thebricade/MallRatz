using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class SummaryScreen : UIPanel
    {
        [SerializeField, Tooltip("RectTransform of the main summary panel.")]
        private RectTransform mainPanel;

        [Header("Component References")]
        [SerializeField, Tooltip("Text component to display the summary values.")]
        private TMP_Text valuesText;

        [SerializeField, Tooltip("Toggle to allow skipping the summary.")]
        private Toggle skipToggle;

        [SerializeField, Tooltip("Button to continue after viewing the summary.")]
        private Button continueButton;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private void Start()
        {
            mainPanel.anchoredPosition = Vector2.zero; // Center the panel.

            // Set a semi-transparent background color if an Image component exists.
            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.4f);
            }

            // Add a listener to the skip toggle's value changed event.
            skipToggle.onValueChanged.AddListener(isOn =>
                AudioManager.Instance.PlaySFX(AudioID.Click)
            );

            HideUI();
        }

        private void OnEnable() => StoreEvents.OnSummaryRequested += HandleSummaryRequested;
        private void OnDisable() => StoreEvents.OnSummaryRequested -= HandleSummaryRequested;

        /// <summary>
        /// Shows the summary screen and populates it with the provided data.
        /// </summary>
        /// <param name="data">The SummaryData object containing the summary information.</param>
        /// <param name="onContinue">The action to be performed when the continue button is clicked. Passes a boolean indicating if the day was skipped.</param>
        private void HandleSummaryRequested(SummaryData data, System.Action<bool> onContinue)
        {
            string values = $"{data.TotalCustomers}";                                          // 1. Total Customers
            values += $"\n{currencySymbol}{data.PreviousBalance:N2}";                          // 2. Previous Balance
            values += $"\n<color=green>+{currencySymbol}{data.TotalRevenues:N2}";              // 3. Total Revenues (green text)
            values += $"\n<color=red>-{currencySymbol}{data.TotalSpending:N2}";                // 4. Total Spending (red text)
            values += $"\n<color=white>{currencySymbol}{DataManager.Instance.PlayerMoney:N2}"; // 5. Current Balance (white text)
            valuesText.text = values;

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                onContinue?.Invoke(skipToggle.isOn);
                AudioManager.Instance.PlaySFX(AudioID.Click);
                HideUI();
            });

            ShowUI();
        }
    }
}
