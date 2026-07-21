using System.Collections.Generic;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class LightEmitter : MonoBehaviour
    {
        [SerializeField, Tooltip("List of GameObjects representing the lights (fake) to be controlled.")]
        private List<GameObject> lightObjects;

        private void Start()
        {
            TimeEvents.OnNightTimeChanged += HandleNightTimeChanged; // Subscribe to the night time change event.

            HandleNightTimeChanged(TimeManager.Instance.IsNightTime()); // Set initial light states.
        }

        private void OnDisable()
        {
            TimeEvents.OnNightTimeChanged -= HandleNightTimeChanged; // Unsubscribe from the event.
        }

        /// <summary>
        /// Handles the night time changed event and activates/deactivates the lights.
        /// </summary>
        /// <param name="isNightTime">Whether it is currently night time.</param>
        void HandleNightTimeChanged(bool isNightTime)
        {
            lightObjects.ForEach(light => light.SetActive(isNightTime)); // Activate lights during night, deactivate during day.
        }
    }
}
