using UnityEngine;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class InteractMessage : UIPanel
    {
        [SerializeField] private ControlMode controlMode;

        private TMP_Text messageText;

        private const string DisplayMessageKey = "Display Interact Message";

        protected override void Awake()
        {
            if (GameConfig.Instance.ControlMode != controlMode)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();
            messageText = GetComponentInChildren<TMP_Text>();
            HideUI();
        }

        private void OnEnable() => UIEvents.OnInteractMessageRequested += HandleInteractMessageRequested;
        private void OnDisable() => UIEvents.OnInteractMessageRequested -= HandleInteractMessageRequested;

        /// <summary>
        /// Displays the interaction message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void HandleInteractMessageRequested(string message)
        {
            if (!IsDisplayEnabled()) return;

            // Determine state based on message content
            bool hasMessage = !string.IsNullOrEmpty(message);

            if (hasMessage)
            {
                messageText.text = message;
                ShowUI();
            }
            else
            {
                HideUI();
            }
        }

        private bool IsDisplayEnabled() => PlayerPrefs.GetInt(DisplayMessageKey, 1) >= 1;
    }
}
