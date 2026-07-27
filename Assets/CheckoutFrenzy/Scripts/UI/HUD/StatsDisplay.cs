using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class StatsDisplay : MonoBehaviour
    {
        [SerializeField] private LocalizedString levelFormat;

        [Header("Component References")]
        [SerializeField, Tooltip("Text component to display the player's money.")]
        private TMP_Text moneyDisplay;

        [SerializeField, Tooltip("Text component to display the player / store current level.")]
        private TMP_Text levelDisplay;

        [SerializeField, Tooltip("Image used as a fill bar to visualize level progress.")]
        private Image levelFill;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;
        private DataManager dataManager;

        private void Start()
        {
            dataManager = ClientShopContext.Instance.MyStore?.DataManager;
            if (dataManager == null) return;

            levelFormat.StringChanged += OnLevelStringChanged;

            dataManager.OnMoneyChanged += UpdateMoneyDisplay;  // Subscribe to money changed events.
            dataManager.OnLevelUp += UpdateLevelDisplay;       // Subscribe to level up events.
            dataManager.OnExperienceGain += UpdateLevelFill;   // Subscribe to experience gain events.

            UpdateMoneyDisplay(dataManager.PlayerMoney);       // Initial update of money display.
            UpdateLevelDisplay(dataManager.Data.CurrentLevel); // Initial update of level display.

            // Calculate and update the level fill bar.
            float currentExp = (float)dataManager.Data.CurrentExperience;
            int expForNextLevel = dataManager.CalculateExperienceForNextLevel();
            float progress = currentExp / expForNextLevel;
            UpdateLevelFill(progress);
        }

        private void OnDestroy()
        {
            levelFormat.StringChanged -= OnLevelStringChanged;

            if (dataManager != null)
            {
                dataManager.OnMoneyChanged -= UpdateMoneyDisplay;
                dataManager.OnLevelUp -= UpdateLevelDisplay;
                dataManager.OnExperienceGain -= UpdateLevelFill;
            }
        }

        private void OnLevelStringChanged(string value)
        {
            levelDisplay.text = value;
        }

        /// <summary>
        /// Updates the displayed money amount.
        /// </summary>
        /// <param name="amount">The player's current money.</param>
        private void UpdateMoneyDisplay(decimal amount)
        {
            moneyDisplay.text = $"{currencySymbol} {amount:N2}"; // Format and set the money text.
            moneyDisplay.color = amount < 0m ? Color.red : Color.white;
        }

        /// <summary>
        /// Updates the displayed player / store level.
        /// </summary>
        /// <param name="level">The player / store current level.</param>
        private void UpdateLevelDisplay(int level)
        {
            levelFormat.Arguments = new object[] { level };
            levelFormat.RefreshString();
        }

        /// <summary>
        /// Updates the level fill bar based on experience progress.
        /// </summary>
        /// <param name="progress">The normalized experience progress (0-1).</param>
        private void UpdateLevelFill(float progress)
        {
            levelFill.fillAmount = progress;
        }
    }
}
