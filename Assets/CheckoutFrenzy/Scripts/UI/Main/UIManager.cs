using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using SimpleInputNamespace;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField, Tooltip("Image used for the player's crosshair.")]
        private Image crosshair;

        [SerializeField, Tooltip("Joystick component for player movement input.")]
        private Joystick joystick;

        [SerializeField, Tooltip("Button for interacting with interactables.")]
        private Button interactButton;

        [Header("Gameplay UI")]
        [SerializeField, Tooltip("Radial fill image indicating hold progress (e.g., when moving furniture).")]
        private Image holdProgressImage;

        [SerializeField, Tooltip("Displays the remaining time for deliveries.")]
        private TMP_Text deliveryTimerText;

        [SerializeField, Tooltip("Format for the next delivery countdown. {0} is replaced with the remaining time.")]
        private LocalizedString nextDeliveryFormat;

        [Header("Action UIs")]
        [SerializeField, Tooltip("Parent for action buttons (Mobile).")]
        private RectTransform actionButtonsParent;

        [SerializeField, Tooltip("Parent for action prompts (PC).")]
        private RectTransform actionPromptsParent;

        [Header("Game Pause")]
        [SerializeField, Tooltip("Reference to the pause menu.")]
        private PauseMenu pauseMenu;

        [SerializeField, Tooltip("Key to pause the game and display the pause menu.")]
        private KeyCode pauseKey = KeyCode.Escape;

        private bool isMobileControl;
        private bool isGamePaused => Time.timeScale < 1f;
        private List<IActionUI> actionUIs;

        private void Awake()
        {
            nextDeliveryFormat.StringChanged += OnNextDeliveryStringChanged;
        }

        private void OnDestroy()
        {
            nextDeliveryFormat.StringChanged -= OnNextDeliveryStringChanged;
        }

        private void Start()
        {
            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            if (isMobileControl)
            {
                actionUIs = actionButtonsParent.GetComponentsInChildren<IActionUI>().ToList();

                actionPromptsParent.gameObject.SetActive(false);
            }
            else
            {
                actionUIs = actionPromptsParent.GetComponentsInChildren<IActionUI>().ToList();

                actionButtonsParent.gameObject.SetActive(false);

                Vector2 originalPos = actionPromptsParent.anchoredPosition;
                Vector2 targetPos = new Vector2(0f, originalPos.y);
                actionPromptsParent.anchoredPosition = targetPos;
            }

            HandleCrosshairVisibilityChanged(true);
            HandleHoldProgressChanged(0f);
            HandleInteractionAvailable(false);
            HandleDeliveryTimeChanged(0);
            actionUIs.ForEach(actionUI => actionUI.SetActive(false));

            nextDeliveryFormat.StringChanged += OnNextDeliveryStringChanged;
        }

        private void OnEnable()
        {
            UIEvents.OnCrosshairVisibilityChanged += HandleCrosshairVisibilityChanged;
            UIEvents.OnHoldProgressChanged += HandleHoldProgressChanged;
            UIEvents.OnInteractionAvailable += HandleInteractionAvailable;
            UIEvents.OnActionUIToggled += HandleActionUIToggled;

            StoreEvents.OnDeliveryTimeChanged += HandleDeliveryTimeChanged;
        }

        private void OnDisable()
        {
            UIEvents.OnCrosshairVisibilityChanged -= HandleCrosshairVisibilityChanged;
            UIEvents.OnHoldProgressChanged -= HandleHoldProgressChanged;
            UIEvents.OnInteractionAvailable -= HandleInteractionAvailable;
            UIEvents.OnActionUIToggled -= HandleActionUIToggled;

            StoreEvents.OnDeliveryTimeChanged -= HandleDeliveryTimeChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (pauseMenu.gameObject.activeSelf) pauseMenu.Close();
                else if (!isGamePaused) pauseMenu.Open();
            }
        }

        private void HandleCrosshairVisibilityChanged(bool visible)
        {
            crosshair.gameObject.SetActive(visible);
            joystick.gameObject.SetActive(visible);

            if (!isMobileControl)
            {
                Cursor.visible = !visible;
                Cursor.lockState = visible ? CursorLockMode.Locked : CursorLockMode.None;
            }
        }

        private void HandleHoldProgressChanged(float progress)
        {
            if (progress > 0.2f)
            {
                if (!holdProgressImage.gameObject.activeSelf)
                {
                    holdProgressImage.gameObject.SetActive(true);
                }
                holdProgressImage.fillAmount = Mathf.Clamp01(progress);
            }
            else
            {
                holdProgressImage.gameObject.SetActive(false);
            }
        }

        private void HandleInteractionAvailable(bool available)
        {
            if (available && !isMobileControl) return;

            interactButton.gameObject.SetActive(available);
        }

        private void OnNextDeliveryStringChanged(string value)
        {
            if (value.Contains("{0}")) return;
            deliveryTimerText.text = value;
        }

        private void HandleDeliveryTimeChanged(int time)
        {
            if (time <= 0)
            {
                deliveryTimerText.text = "";
                return;
            }

            System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(time);

            nextDeliveryFormat.Arguments = new object[]
            {
                timeSpan.ToString(@"mm\:ss")
            };

            nextDeliveryFormat.RefreshString();
        }

        private void HandleActionUIToggled(ActionType actionType, bool active, System.Action action)
        {
            var actionUI = actionUIs.FirstOrDefault(a => a.ActionType == actionType);

            actionUI.SetActive(active);

            actionUI.OnClick.RemoveAllListeners();
            actionUI.OnClick.AddListener(() => action?.Invoke());
        }
    }
}
