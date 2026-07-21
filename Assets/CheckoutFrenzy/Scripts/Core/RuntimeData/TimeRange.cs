using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public struct TimeRange
    {
        [Range(0, 23)]
        [Tooltip("The starting hour for this time range.")]
        public int StartHour;

        [Range(0, 59)]
        [Tooltip("The starting minute for this time range.")]
        public int StartMinute;

        [Range(0, 23)]
        [Tooltip("The ending hour for this time range.")]
        public int EndHour;

        [Range(0, 59)]
        [Tooltip("The ending minute for this time range.")]
        public int EndMinute;

        /// <summary>
        /// Converts the time range to total minutes.
        /// </summary>
        /// <param name="hour">The hour component.</param>
        /// <param name="minute">The minute component.</param>
        /// <returns>Total minutes in a day.</returns>
        public static int ToMinutes(int hour, int minute) => hour * 60 + minute;

        /// <summary>
        /// Checks if a given time (in minutes) is within this range.
        /// </summary>
        /// <param name="currentTimeMinutes">The current time in total minutes.</param>
        /// <returns>True if within range, otherwise false.</returns>
        public bool IsWithinRange(int currentTimeMinutes)
        {
            int startMinutes = ToMinutes(StartHour, StartMinute);
            int endMinutes = ToMinutes(EndHour, EndMinute);

            // Handle overnight ranges (e.g., 22:00 - 06:00)
            if (startMinutes > endMinutes)
            {
                return currentTimeMinutes >= startMinutes || currentTimeMinutes < endMinutes;
            }

            return currentTimeMinutes >= startMinutes && currentTimeMinutes < endMinutes;
        }
    }
}
