using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class PaymentTerminal : MonoBehaviour
    {
        [Header("Messages")]
        [SerializeField, Tooltip("The localized text shown when the entered card payment amount is incorrect.")]
        private LocalizedString invalidAmountMessage;

        [Header("Terminal Settings")]
        [SerializeField, Tooltip("The text display showing the entered amount.")]
        private TMP_Text displayText;

        [SerializeField, Tooltip("The button used to confirm the payment.")]
        private Button confirmButton;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private RectTransform rect;
        private float originalPosY;
        private bool allowInput;
        private string enteredAmount;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;
            confirmButton.onClick.AddListener(ConfirmAmount); // Add listener to the confirm button.
        }

        private void OnEnable()
        {
            CheckoutEvents.OnPaymentTerminalToggleRequested += HandleToggleRequest;
        }

        private void OnDisable()
        {
            CheckoutEvents.OnPaymentTerminalToggleRequested -= HandleToggleRequest;
        }

        private void HandleToggleRequest(bool open)
        {
            if (open) Open();
            else Close();
        }

        /// <summary>
        /// Appends the input to the entered amount string.
        /// </summary>
        /// <param name="input">The input string (number, "back", or ".").</param>
        public void Append(string input)
        {
            if (!allowInput) return;

            if (input == "back")
            {
                if (enteredAmount.Length > 0)
                {
                    enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1); // Remove the last character.
                }
            }
            else if (input == "." && !enteredAmount.Contains(".")) // Allow only one decimal point.
            {
                enteredAmount += ".";
            }
            else if (int.TryParse(input, out int _)) // Only allow numeric input.
            {
                enteredAmount += input;
            }

            displayText.text = $"{currencySymbol} {enteredAmount}";

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Confirms the entered amount and triggers the OnConfirm event.
        /// </summary>
        private void ConfirmAmount()
        {
            if (decimal.TryParse(enteredAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) && amount > 0)
            {
                CheckoutEvents.RaisePaymentTerminalConfirm(amount);
            }
            else
            {
                UIEvents.RaiseMessage(invalidAmountMessage.GetLocalizedString(), Color.red);
            }

            AudioManager.Instance.PlaySFX(AudioID.Beep);
        }

        /// <summary>
        /// Opens the payment terminal UI, allowing input.
        /// </summary>
        private void Open()
        {
            enteredAmount = "";                 // Clear the entered amount.
            displayText.text = currencySymbol;  // Reset the display text.

            rect.DOAnchorPosY(0f, 0.5f)                 // Animate the terminal opening.
                .OnComplete(() => allowInput = true);   // Enable input after the animation.
        }

        /// <summary>
        /// Closes the payment terminal UI, disabling input.
        /// </summary>
        private void Close()
        {
            allowInput = false; // Disable input.
            rect.DOAnchorPosY(originalPosY, 0.5f); // Animate the terminal closing.
        }
    }
}
