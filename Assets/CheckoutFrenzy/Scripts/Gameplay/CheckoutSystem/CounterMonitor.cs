using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class CounterMonitor : MonoBehaviour
    {
        [SerializeField, Tooltip("The TextMeshPro object used to display information on the counter monitor.")]
        private TextMeshPro monitorText;

        [Header("Localization")]
        [SerializeField, Tooltip("Standby text")]
        private LocalizedString standbyString;

        [SerializeField, Tooltip("Placing/Waiting text")]
        private LocalizedString placingString;

        [SerializeField, Tooltip("Scanning text. Expects {0} for currency and {1} for total.")]
        private LocalizedString scanningString;

        [SerializeField, Tooltip("Cash payment text. Expects {0}=currency, {1}=total, {2}=received, {3}=change, {4}=color, {5}=given.")]
        private LocalizedString cashPayString;

        [SerializeField, Tooltip("Card payment text. Expects {0} for currency and {1} for total.")]
        private LocalizedString cardPayString;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        public void UpdateDisplay(CounterState state, decimal totalPrice, decimal customerMoney = 0, decimal totalChangeGiven = 0)
        {
            switch (state)
            {
                case CounterState.Standby:
                    monitorText.text = standbyString.GetLocalizedString();
                    break;

                case CounterState.Placing:
                    monitorText.text = placingString.GetLocalizedString();
                    break;

                case CounterState.Scanning:
                    scanningString.Arguments = new object[] { currencySymbol, totalPrice.ToString("N2") };
                    monitorText.text = scanningString.GetLocalizedString();
                    break;

                case CounterState.CashPay:
                    decimal changeNeeded = customerMoney - totalPrice;
                    string color = totalChangeGiven >= changeNeeded ? "green" : "red";

                    cashPayString.Arguments = new object[] {
                        currencySymbol,
                        totalPrice.ToString("N2"),
                        customerMoney.ToString("N2"),
                        changeNeeded.ToString("N2"),
                        color,
                        totalChangeGiven.ToString("N2")
                    };
                    monitorText.text = cashPayString.GetLocalizedString();
                    break;

                case CounterState.CardPay:
                    cardPayString.Arguments = new object[] { currencySymbol, totalPrice.ToString("N2") };
                    monitorText.text = cardPayString.GetLocalizedString();
                    break;
            }
        }

        public void ClearDisplay() => monitorText.text = string.Empty;
    }
}
