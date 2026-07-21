using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class SettingsWindow : MonoBehaviour, IPointerClickHandler
    {
        [Header("Layout")]

        [SerializeField, Tooltip("RectTransform of the main settings panel. Clicking outside this panel closes the window.")]
        private RectTransform mainPanel;

        [Header("Audio")]

        [SerializeField, Tooltip("Slider controlling the background music (BGM) volume.")]
        private Slider bgmSlider;

        [SerializeField, Tooltip("Slider controlling the sound effects (SFX) volume.")]
        private Slider sfxSlider;

        [Header("Gameplay")]

        [SerializeField, Tooltip("Toggle to enable or disable interaction hint messages.")]
        private Toggle interactMessageToggle;

        [Header("Localization")]

        [SerializeField, Tooltip("Dropdown used to select the game language.")]
        private TMP_Dropdown languageDropdown;

        private void Start()
        {
            // Initially hide the settings window.
            gameObject.SetActive(false);

            // Ensure panel is centered.
            mainPanel.anchoredPosition = Vector2.zero;

            // Apply semi-transparent background if Image exists.
            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.9f);
            }

            InitializeAudio();
            InitializeGameplay();
            InitializeLanguageDropdown();
        }

        private void InitializeAudio()
        {
            bgmSlider.value = SettingsManager.Instance.GetBGM();
            sfxSlider.value = SettingsManager.Instance.GetSFX();

            bgmSlider.onValueChanged.AddListener(value =>
            {
                SettingsManager.Instance.SetBGM(value);
            });

            sfxSlider.onValueChanged.AddListener(value =>
            {
                SettingsManager.Instance.SetSFX(value);
            });
        }

        private void InitializeGameplay()
        {
            interactMessageToggle.isOn = SettingsManager.Instance.GetInteractMessage();

            interactMessageToggle.onValueChanged.AddListener(value =>
            {
                SettingsManager.Instance.SetInteractMessage(value);
                AudioManager.Instance.PlaySFX(AudioID.Click);
            });
        }

        private void InitializeLanguageDropdown()
        {
            languageDropdown.ClearOptions();

            var locales = LocalizationSettings.AvailableLocales.Locales;
            var options = new List<string>();

            for (int i = 0; i < locales.Count; i++)
                options.Add(locales[i].Identifier.CultureInfo.NativeName);

            languageDropdown.AddOptions(options);

            languageDropdown.value = SettingsManager.Instance.GetLanguage();

            languageDropdown.onValueChanged.AddListener(index =>
            {
                SettingsManager.Instance.SetLanguage(index);
                AudioManager.Instance.PlaySFX(AudioID.Click);
            });
        }

        /// <summary>
        /// Handles pointer clicks on the settings window.
        /// Closes the window when clicking outside the main panel.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(mainPanel, eventData.position))
                return;

            Toggle();
        }

        /// <summary>
        /// Toggles the visibility of the settings window.
        /// </summary>
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            AudioManager.Instance.PlaySFX(AudioID.Click);
        }
    }
}
