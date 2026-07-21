using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class MissionTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("TextMeshPro component to display the mission details.")]
        private TMP_Text missionText;

        [SerializeField, Tooltip("Button to claim the reward for completing the mission.")]
        private Button rewardButton;

        [SerializeField, Tooltip("Text component to display the reward amount.")]
        private TMP_Text rewardText;

        [SerializeField, Tooltip("The key used to collect mission reward.")]
        private KeyCode rewardKey = KeyCode.M;

        [SerializeField, Tooltip("Image showing the icon of the collect reward key.")]
        private Image keyIcon;

        [SerializeField, Tooltip("Toggle used to show and hide the tracker on mobile contol mode.")]
        private PanelToggle panelToggle;

        [Header("Localization")]
        [SerializeField] private LocalizedString missionTitle;
        [SerializeField] private LocalizedString missionUnavailable;
        [SerializeField] private LocalizedString missionCollectReward;

        [SerializeField] private LocalizedString goalCheckout;
        [SerializeField] private LocalizedString goalRevenue;
        [SerializeField] private LocalizedString goalSell;
        [SerializeField] private LocalizedString goalRestock;
        [SerializeField] private LocalizedString goalFurnish;
        [SerializeField] private LocalizedString goalLicense;

        private LocalizedString[] localizedStrings;
        private bool isMobileControl;

        private void Awake()
        {
            localizedStrings = new[]
            {
                missionTitle,
                missionUnavailable,
                missionCollectReward,
                goalCheckout,
                goalRevenue,
                goalSell,
                goalRestock,
                goalFurnish,
                goalLicense
            };
        }

        private void Start()
        {
            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;

            keyIcon.gameObject.SetActive(!isMobileControl);
            panelToggle.gameObject.SetActive(isMobileControl);

            foreach (var ls in localizedStrings)
                ls.StringChanged += OnLocalizationChanged;

            MissionManager.Instance.OnMissionUpdated += UpdateDisplay; // Subscribe to mission update events.

            // Get the current mission and mission data.
            var mission = MissionManager.Instance.GetCurrentMission();
            var missionData = DataManager.Instance.Data.CurrentMission;
            UpdateDisplay(mission, missionData); // Initial display update.
        }

        private void OnDestroy()
        {
            foreach (var ls in localizedStrings)
                ls.StringChanged -= OnLocalizationChanged;

            if (MissionManager.Instance != null)
                MissionManager.Instance.OnMissionUpdated -= UpdateDisplay;
        }

        private void OnLocalizationChanged(string _)
        {
            var mission = MissionManager.Instance.GetCurrentMission();
            var missionData = DataManager.Instance.Data.CurrentMission;

            UpdateDisplay(mission, missionData);
        }

        private void Update()
        {
            if (Input.GetKeyDown(rewardKey))
            {
                if (!rewardButton.gameObject.activeSelf) return;
                MissionManager.Instance.AdvanceMission();
            }
        }

        /// <summary>
        /// Updates the displayed mission information based on the provided mission and mission data.
        /// </summary>
        /// <param name="mission">The Mission object containing mission details.</param>
        /// <param name="missionData">The MissionData object containing mission progress.</param>
        private void UpdateDisplay(Mission mission, MissionData missionData)
        {
            // Handle cases where mission or mission data is not available.
            if (mission == null || missionData == null)
            {
                missionText.text = $"<align=left>{missionUnavailable.GetLocalizedString()}";
                rewardButton.gameObject.SetActive(false);
                return;
            }

            string displayText = "";

            // Mission Title
            displayText = $"<u>{missionTitle.GetLocalizedString(mission.missionId.ToString("D3"))}</u>";

            // Goal Type
            displayText += $"\n<align=left>{GetFormattedGoalLocalized(mission.goalType)}:";

            // Add target information based on the goal type.
            if (mission.goalType == MissionGoal.Sell || mission.goalType == MissionGoal.Restock)
            {
                var product = DataManager.Instance.GetProductById(mission.targetId);
                displayText += $"\n{product.Name}"; // Add product name.
            }
            else if (mission.goalType == MissionGoal.Furnish)
            {
                var furniture = DataManager.Instance.GetFurnitureById(mission.targetId);
                if (furniture == null) Debug.Log($"Furniture ID: {mission.targetId}"); // Log if furniture not found.
                displayText += $"\n{furniture.Name}"; // Add furniture name.
            }
            else if (mission.goalType == MissionGoal.License)
            {
                var license = DataManager.Instance.GetLicenseById(mission.targetId);
                displayText += $"\n{license.Name}";
            }

            // Add progress information based on the goal type.
            if (mission.goalType == MissionGoal.Revenue)
            {
                displayText += $"\n<align=center>${missionData.Progress / 100m:N2} / ${mission.goalAmount / 100m:N2}"; // Format revenue progress.
            }
            else
            {
                displayText += $"\n<align=center>{missionData.Progress} / {mission.goalAmount}"; // Format other progress.
            }

            // Handle reward display and button interactability based on mission completion.
            if (missionData.IsComplete)
            {
                displayText += $"\n<align=left>{missionCollectReward.GetLocalizedString()}";
                rewardText.text = $"${mission.reward:N2}"; // Display reward amount.

                rewardButton.gameObject.SetActive(true); // Show the reward button.

                if (isMobileControl)
                {
                    rewardButton.onClick.RemoveAllListeners(); // Remove previous listeners.
                    rewardButton.onClick.AddListener(MissionManager.Instance.AdvanceMission); // Add listener to advance mission.
                }
            }
            else
            {
                rewardButton.gameObject.SetActive(false); // Hide the reward button if mission is not complete.
            }

            missionText.text = displayText; // Set the mission text.
        }

        private string GetFormattedGoalLocalized(MissionGoal goalType)
        {
            switch (goalType)
            {
                case MissionGoal.Checkout:
                    return goalCheckout.GetLocalizedString();

                case MissionGoal.Revenue:
                    return goalRevenue.GetLocalizedString();

                case MissionGoal.Sell:
                    return goalSell.GetLocalizedString();

                case MissionGoal.Restock:
                    return goalRestock.GetLocalizedString();

                case MissionGoal.Furnish:
                    return goalFurnish.GetLocalizedString();

                case MissionGoal.License:
                    return goalLicense.GetLocalizedString();
            }

            return "";
        }
    }
}
