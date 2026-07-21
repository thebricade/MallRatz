using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class SavedGamesWindow : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Tooltip("RectTransform of the main panel.")]
        private RectTransform mainPanel;

        [Header("Delete Confirmation")]
        [SerializeField] private GameObject confirmationScreen;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString localizedConfirmationMessage;

        private SavedGameUI[] savedGameUIs;

        private void OnEnable()
        {
            localizedConfirmationMessage.StringChanged += UpdateConfirmationText;
        }

        private void OnDisable()
        {
            localizedConfirmationMessage.StringChanged -= UpdateConfirmationText;
        }

        private void UpdateConfirmationText(string translatedText)
        {
            confirmationText.text = translatedText;
        }

        private void Start()
        {
            gameObject.SetActive(false); // Initially hide the saved games window.

            mainPanel.anchoredPosition = Vector2.zero; // Set the panel's anchored position to the center.

            // Set a semi-transparent background color if an Image component exists.
            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.9f);
            }

            cancelButton.onClick.AddListener(() =>
            {
                confirmationScreen.SetActive(false);
                AudioManager.Instance.PlaySFX(AudioID.Click);
            });

            savedGameUIs = GetComponentsInChildren<SavedGameUI>();

            for (int i = 0; i < savedGameUIs.Length; i++)
            {
                int slot = i + 1;
                var gameData = SaveSystem.LoadData<GameData>($"GameData{slot}");
                savedGameUIs[i].Initialize(gameData, ConfirmDeletion);
            }
        }

        /// <summary>
        /// Handles pointer clicks on the saved games window.
        /// Closes the window if clicked outside the main panel.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the click originated from the main panel.
            if (RectTransformUtility.RectangleContainsScreenPoint(mainPanel, eventData.position) || confirmationScreen.activeSelf)
            {
                // Clicked on the main panel, do nothing.
                return;
            }

            // Clicked outside the main panel, toggle (close) the window.
            Toggle();
        }

        /// <summary>
        /// Opens the saved games window.
        /// </summary>
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void ConfirmDeletion(int slot)
        {
            confirmationScreen.SetActive(true);

            localizedConfirmationMessage.Arguments = new object[] { slot };
            localizedConfirmationMessage.RefreshString();

            AudioManager.Instance.PlaySFX(AudioID.Click);

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                string path = Path.Combine(Application.persistentDataPath, $"GameData{slot}.dat");

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                savedGameUIs[slot - 1].Initialize(null, null);

                confirmationScreen.SetActive(false);

                AudioManager.Instance.PlaySFX(AudioID.Click);
            });
        }
    }
}
