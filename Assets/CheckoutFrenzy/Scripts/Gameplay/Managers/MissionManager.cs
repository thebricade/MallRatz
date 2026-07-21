using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Messages")]
        [SerializeField, Tooltip("Shown when the player collects the reward for completing a mission.")]
        private LocalizedString rewardCollectedMessage;

        public event System.Action<Mission, MissionData> OnMissionUpdated;

        private Dictionary<int, Mission> missionDict;

        private void Awake()
        {
            Instance = this;

            // Create a dictionary with missionId as the key and Mission object as the value
            missionDict = Resources.LoadAll<Mission>("Missions")
                .OrderBy(mission => mission.missionId)
                .ToDictionary(mission => mission.missionId);
        }

        /// <summary>
        /// Updates the progress of the current mission based on the provided goal type, progress amount, and optional item id.
        /// </summary>
        /// <param name="goalType">The type of mission goal (Sell, Restock, Furnish, etc.).</param>
        /// <param name="progressAmount">The amount of progress to add.</param>
        /// <param name="itemId">Optional item id relevant to the goal (e.g., for Sell or Furnish goals).</param>
        public void UpdateMission(MissionGoal goalType, int progressAmount, int itemId = 0)
        {
            Mission currentMission = GetCurrentMission();

            // Check if the current mission exists and has the matching goal type
            if (currentMission == null || currentMission.goalType != goalType) return;

            // Exit if the mission is already completed
            if (DataManager.Instance.Data.CurrentMission.IsComplete) return;

            // Flag to track if the target item matches
            bool targetMatch = false;

            // Check if the target item matches the mission's target item id (applicable for Sell, Restock, Furnish, goals)
            if (currentMission.goalType == MissionGoal.Sell || currentMission.goalType == MissionGoal.Restock || currentMission.goalType == MissionGoal.Furnish)
            {
                targetMatch = currentMission.targetId == itemId;
            }
            else if (currentMission.goalType == MissionGoal.License)
            {
                targetMatch = DataManager.Instance.Data.OwnedLicenses.Contains(currentMission.targetId);
            }
            else
            {
                // For other goal types, any item is considered a target
                targetMatch = true;
            }

            // Update progress if the target matches
            if (targetMatch) DataManager.Instance.Data.CurrentMission.Progress += progressAmount;

            // Clamp the progress to the goal amount
            if (DataManager.Instance.Data.CurrentMission.Progress >= currentMission.goalAmount)
            {
                DataManager.Instance.Data.CurrentMission.Progress = currentMission.goalAmount;
                DataManager.Instance.Data.CurrentMission.IsComplete = true;
            }

            // Trigger the OnMissionUpdated event with the current and updated mission data
            OnMissionUpdated?.Invoke(currentMission, DataManager.Instance.Data.CurrentMission);
        }

        /// <summary>
        /// Gets the current mission from the game data.
        /// </summary>
        /// <returns>The current mission object or null if not found.</returns>
        public Mission GetCurrentMission()
        {
            // Check if the current mission data exists
            if (DataManager.Instance.Data.CurrentMission == null) return null;

            int missionId = DataManager.Instance.Data.CurrentMission.MissionID;

            // Try to get the mission from the mission DB using the mission id
            if (missionDict.TryGetValue(missionId, out Mission mission))
            {
                return mission;
            }

            // Log a warning if the mission with the id is not found
            Debug.LogWarning($"Mission with ID {missionId} not found.");
            return null;
        }

        /// <summary>
        /// Advances the mission to the next one in the sequence.
        /// </summary>
        public void AdvanceMission()
        {
            Mission currentMission = GetCurrentMission();
            if (currentMission == null) return;

            DataManager.Instance.PlayerMoney += currentMission.reward;
            UIEvents.RaiseMessage(rewardCollectedMessage.GetLocalizedString());
            AudioManager.Instance.PlaySFX(AudioID.Kaching);

            int? nextMissionId = missionDict.Keys
                .Where(id => id > DataManager.Instance.Data.CurrentMission.MissionID)
                .OrderBy(id => id)
                .Cast<int?>()
                .FirstOrDefault();

            Mission nextMission = nextMissionId.HasValue ? missionDict[nextMissionId.Value] : null;

            DataManager.Instance.Data.CurrentMission =
                nextMission != null ? new MissionData(nextMission.missionId) : null;

            OnMissionUpdated?.Invoke(nextMission, DataManager.Instance.Data.CurrentMission);

            if (nextMission != null && nextMission.goalType == MissionGoal.License)
            {
                UpdateMission(MissionGoal.License, 1, nextMission.targetId);
            }
        }
    }
}
