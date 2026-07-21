using UnityEngine;
using UnityEngine.UI;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class SkipDialog : UIPanel
    {
        [SerializeField, Tooltip("The button used to skip the day.")]
        private Button skipButton;

        [SerializeField, Tooltip("The key used to skip the day.")]
        private KeyCode skipKey = KeyCode.Z;

        [SerializeField, Tooltip("Image showing the icon of the skip key.")]
        private Image keyIcon;

        [SerializeField, Tooltip("Toggle used to show and hide the dialog on mobile contol mode.")]
        private PanelToggle panelToggle;

        private bool isMobileControl;
        private System.Action onSkip;

        protected override void Awake()
        {
            base.Awake();

            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            keyIcon.gameObject.SetActive(!isMobileControl);
            panelToggle.gameObject.SetActive(isMobileControl);

            HideUI();
        }

        private void OnEnable() => StoreEvents.OnSkipDialogRequested += HandleSkipDialogRequested;
        private void OnDisable() => StoreEvents.OnSkipDialogRequested -= HandleSkipDialogRequested;

        private void Update()
        {
            if (Input.GetKeyDown(skipKey) && !isMobileControl)
            {
                SkipTheDay();
            }
        }

        /// <summary>
        /// Shows the skip dialog and sets up the skip button's action.
        /// </summary>
        /// <param name="onSkip">The action to be performed when the user manually skips the day.</param>
        /// <param name="onRegisterClose">A callback that provides the requester with a way to close this UI externally.</param>
        private void HandleSkipDialogRequested(System.Action onSkip, System.Action<System.Action> onRegisterClose)
        {
            this.onSkip = onSkip;

            onRegisterClose?.Invoke(HideUI);

            if (isMobileControl)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipTheDay);
            }

            ShowUI();
        }

        private void SkipTheDay()
        {
            onSkip?.Invoke();
            onSkip = null;
            AudioManager.Instance.PlaySFX(AudioID.Click);
            HideUI();
        }
    }
}
