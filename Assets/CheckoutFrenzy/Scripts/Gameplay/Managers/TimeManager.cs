using System.Globalization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class TimeManager : MonoBehaviour
    {
        public static TimeManager Instance { get; private set; }

        [Tooltip("The total in-game minutes (0-1439 for 24 hours).")]
        [SerializeField, Range(0f, 1439f)] private float totalMinutes = 0;

        [Tooltip("The scale at which time progresses. 1 means 1 real second equals 1 in-game minute.")]
        [SerializeField, Range(1, 60)] private float timeScale = 1.0f;

        [SerializeField, Tooltip("Sets the sun's x-axis rotation at midnight, defining its initial position in the sky. (-90f places the sun directly below the horizon).")]
        private float sunMidnightOffset = -90f;

        [SerializeField, Tooltip("Sets the sun's y-axis rotation, determining the direction of its path. (-90f aligns the sun to rise in the east and set in the west).")]
        private float sunDirectionOffset = -90f;

        [SerializeField, Tooltip("Defines the range for nighttime.")]
        private TimeRange nightTime;

        [SerializeField, Tooltip("Materials for objects with night-time emission effects.")]
        private Material[] emissiveMaterials;

        [Header("Fog Colors")]
        [SerializeField] private Color nightFogColor = Color.grey;
        [SerializeField] private Color dayFogColor = Color.blue;

        public bool AllowTimeUpdate { get; set; } = true;

        /// <summary>
        /// Gets the current hour (0-23).
        /// </summary>
        public int Hour => Mathf.FloorToInt(totalMinutes / 60);

        /// <summary>
        /// Gets the current minute (0-59).
        /// </summary>
        public int Minute => Mathf.FloorToInt(totalMinutes % 60);

        public int TotalMinutes => Mathf.FloorToInt(totalMinutes);

        private Light sun;

        private bool wasNightTime;
        private int previousMinute;

        private const float MinutesPerDay = 24 * 60;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            var sunObj = new GameObject("Sun");
            sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.bounceIntensity = 0f;
            sun.shadows = LightShadows.None;
            sun.renderMode = LightRenderMode.ForceVertex;
            sun.cullingMask = 0;
            RenderSettings.sun = sun;

            totalMinutes = DataManager.Instance.Data.TotalMinutes;
            wasNightTime = !nightTime.IsWithinRange(TotalMinutes);
            UpdateSunRotation();
        }

        private void Update()
        {
            if (!AllowTimeUpdate) return;

            totalMinutes = totalMinutes + Time.deltaTime * timeScale;

            if (totalMinutes >= MinutesPerDay)
            {
                totalMinutes = 0f;
                DataManager.Instance.Data.TotalDays++;
            }

            UpdateSunRotation();
            UpdateTimeState();
        }

        private void UpdateSunRotation()
        {
            // Calculate the normalized time for the current day cycle.
            float timeNormalized = totalMinutes / MinutesPerDay;

            var targetRotation = Quaternion.Euler(
                360f * timeNormalized + sunMidnightOffset,
                sunDirectionOffset,
                0f
            );

            sun.transform.rotation = targetRotation;
        }

        private void UpdateTimeState()
        {
            // Check if the current time is within the night range.
            bool isCurrentlyNightTime = IsNightTime();

            // Trigger the event if the nighttime state changes.
            if (isCurrentlyNightTime != wasNightTime)
            {
                wasNightTime = isCurrentlyNightTime;

                TimeEvents.RaiseNightTimeChanged(isCurrentlyNightTime);

                UpdateEmissiveMaterials(isCurrentlyNightTime);
                UpdateFogColor(isCurrentlyNightTime);
            }

            if (previousMinute != Minute)
            {
                previousMinute = Minute;
                TimeEvents.RaiseMinutePassed();
            }
        }

        private void UpdateEmissiveMaterials(bool isNight)
        {
            for (int i = 0; i < emissiveMaterials.Length; i++)
            {
                var emissiveMat = emissiveMaterials[i];

                if (isNight) emissiveMat.EnableKeyword("_EMISSION");
                else emissiveMat.DisableKeyword("_EMISSION");
            }
        }

        private void UpdateFogColor(bool isNight)
        {
            Color targetColor = isNight ? nightFogColor : dayFogColor;

            DOTween.To(() => RenderSettings.fogColor, x => RenderSettings.fogColor = x, targetColor, 3f)
               .SetEase(Ease.Linear);
        }

        public bool IsNightTime()
        {
            return nightTime.IsWithinRange(TotalMinutes);
        }

        /// <summary>
        /// Manually set the current time.
        /// </summary>
        /// <param name="newHour">The new hour (0-23).</param>
        /// <param name="newMinute">The new minute (0-59).</param>
        public void SetTime(int newHour, int newMinute)
        {
            totalMinutes = Mathf.Clamp(newHour, 0, 23) * 60 + Mathf.Clamp(newMinute, 0, 59);
            UpdateSunRotation();
        }

        /// <summary>
        /// Adjusts the time scale at runtime.
        /// </summary>
        /// <param name="newTimeScale">The new time scale.</param>
        public void SetTimeScale(float newTimeScale)
        {
            timeScale = Mathf.Max(0, newTimeScale); // Prevent negative time scale.
        }

        /// <summary>
        /// Gets the current time as a formatted string.
        /// </summary>
        /// <returns>A string in "HH:MM" format.</returns>
        public string GetFormattedTime()
        {
            System.DateTime currentTime = new System.DateTime(1, 1, 1, Hour, Minute, 0);

            // return currentTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            return currentTime.ToString("t", LocalizationSettings.SelectedLocale.Identifier.CultureInfo);
        }
    }
}
