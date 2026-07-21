using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class VirtualKeyboard : UIPanel, IPointerClickHandler
    {
        [SerializeField] private bool disableVirtualKeyboardOnPC;

        [SerializeField] private Slider redSlider;
        [SerializeField] private Slider greenSlider;
        [SerializeField] private Slider blueSlider;

        private ITextReceiver target;
        private RectTransform mainPanel;
        private bool isMobileControl;

        private System.Action onComplete;

        private List<KeyCode> alphabetKeys = new List<KeyCode>();

        protected override void Awake()
        {
            base.Awake();

            // Add A-Z
            for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++)
            {
                alphabetKeys.Add(k);
            }

            alphabetKeys.AddRange(new[] { KeyCode.Backspace, KeyCode.Space, KeyCode.Return, KeyCode.KeypadEnter });

            // Add a listener to each key button.
            foreach (var key in GetComponentsInChildren<Button>())
            {
                key.onClick.AddListener(() => Append(key.name)); // Pass the button's name as input.
            }

            mainPanel = transform.GetChild(0).GetComponent<RectTransform>(); // Get the main panel.
            mainPanel.anchoredPosition = Vector2.zero; // Center the panel.
        }

        private void Start()
        {
            if (disableVirtualKeyboardOnPC && GameConfig.Instance.ControlMode == ControlMode.PC)
            {
                for (int i = 1; i < mainPanel.childCount; i++)
                {
                    mainPanel.GetChild(i).gameObject.SetActive(false);
                }

                isMobileControl = false;
            }
            else
            {
                isMobileControl = true;
            }

            InitializeColorSliders();
            HideUI();
        }

        private void Update()
        {
            if (isMobileControl || target == null) return;

            foreach (KeyCode key in alphabetKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    HandleKeyInput(key);
                }
            }
        }

        private void OnEnable() => StoreEvents.OnStoreNameChangeRequested += HandleStoreNameChangeRequested;
        private void OnDisable() => StoreEvents.OnStoreNameChangeRequested -= HandleStoreNameChangeRequested;

        /// <summary>
        /// Handles pointer clicks on the virtual keyboard. Closes the keyboard if clicked outside the main panel.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the click originated from the main panel.
            if (RectTransformUtility.RectangleContainsScreenPoint(mainPanel, eventData.position))
            {
                // Clicked on the main panel, do nothing.
                return;
            }

            // Clicked outside the main panel, deactivate the game object.
            Close();
        }

        private void HandleStoreNameChangeRequested(ITextReceiver target, System.Action onComplete)
        {
            this.target = target;
            this.onComplete = onComplete;

            ShowUI();
        }

        /// <summary>
        /// Appends the input to the target TextMeshPro component.
        /// </summary>
        /// <param name="input">The input string (key name).</param>
        private void Append(string input)
        {
            if (input == "Back") // Handle backspace.
            {
                if (target.Text.Length > 0)
                {
                    target.Text = target.Text.Substring(0, target.Text.Length - 1); // Remove the last character.
                }
            }
            else if (input == "Enter") // Handle enter key.
            {
                Close();
            }
            else if (target.Text.Length < GameConfig.Instance.StoreNameMaxCharacters) // Limit text length.
            {
                if (input == "Space") target.Text += " "; // Handle space key.
                else target.Text += input; // Append other characters.
            }

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void HandleKeyInput(KeyCode key)
        {
            if (key == KeyCode.Backspace)
            {
                if (target.Text.Length > 0)
                {
                    target.Text = target.Text.Substring(0, target.Text.Length - 1);
                }
            }
            else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            {
                Close();
            }
            else if (key == KeyCode.Space)
            {
                if (target.Text.Length < GameConfig.Instance.StoreNameMaxCharacters)
                    target.Text += " ";
            }
            else
            {
                string keyString = key.ToString();
                if (keyString.Length == 1 && char.IsLetterOrDigit(keyString[0]))
                {
                    if (target.Text.Length < GameConfig.Instance.StoreNameMaxCharacters)
                        target.Text += keyString;
                }
            }

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void InitializeColorSliders()
        {
            redSlider.onValueChanged.AddListener(UpdateColor);
            greenSlider.onValueChanged.AddListener(UpdateColor);
            blueSlider.onValueChanged.AddListener(UpdateColor);

            if (ColorUtility.TryParseHtmlString(DataManager.Instance.Data.NameColor, out var nameColor))
            {
                redSlider.value = nameColor.r;
                greenSlider.value = nameColor.g;
                blueSlider.value = nameColor.b;
            }
        }

        private void UpdateColor(float _)
        {
            if (target == null) return;

            Color newColor = new Color(
                redSlider.value,
                greenSlider.value,
                blueSlider.value
            );

            target.Color = newColor;
        }

        private void Close()
        {
            onComplete?.Invoke();
            onComplete = null;
            HideUI();
        }
    }
}
