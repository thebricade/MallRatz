using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Rounds each component of the <see cref="Vector3"/> down to the nearest tenth (0.1).
        /// </summary>
        /// <param name="vector">The source vector to floor.</param>
        /// <returns>A new <see cref="Vector3"/> where each axis is snapped to a 0.1 increment.</returns>
        /// <example>
        /// A vector of (1.27, 3.14, 0.59) becomes (1.2, 3.1, 0.5).
        /// </example>
        public static Vector3 FloorToTenth(this Vector3 vector)
        {
            return new Vector3(
                Mathf.Floor(vector.x * 10) / 10,
                Mathf.Floor(vector.y * 10) / 10,
                Mathf.Floor(vector.z * 10) / 10
            );
        }

        /// <summary>
        /// Removes the vertical component of the vector by setting the Y-axis to zero.
        /// </summary>
        /// <remarks>
        /// Useful for calculating distances or directions on a 2D horizontal plane (XZ plane) 
        /// while ignoring height differences.
        /// </remarks>
        /// <param name="vector">The source vector to flatten.</param>
        /// <returns>A new <see cref="Vector3"/> containing only the X and Z components.</returns>
        public static Vector3 Flatten(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }
    }
}
