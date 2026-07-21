using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// Checks if the given layer is contained within the LayerMask.
        /// </summary>
        /// <param name="mask">The LayerMask to check against.</param>
        /// <param name="layer">The layer to check for.</param>
        /// <returns>True if the layer is in the mask, false otherwise.</returns>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }

        /// <summary>
        /// Checks if the given GameObject's layer is contained within the LayerMask.
        /// </summary>
        /// <param name="mask">The LayerMask to check against.</param>
        /// <param name="gameObject">The GameObject whose layer to check for.</param>
        /// <returns>True if the GameObject's layer is in the mask, false otherwise.</returns>
        public static bool Contains(this LayerMask mask, GameObject gameObject)
        {
            return mask.Contains(gameObject.layer);
        }

        /// <summary>
        /// Checks if the given Collider's GameObject's layer is contained within the LayerMask.
        /// </summary>
        /// <param name="mask">The LayerMask to check against.</param>
        /// <param name="collider">The Collider whose GameObject's layer to check for.</param>
        /// <returns>True if the Collider's GameObject's layer is in the mask, false otherwise.</returns>
        public static bool Contains(this LayerMask mask, Collider collider)
        {
            return mask.Contains(collider.gameObject);
        }

        /// <summary>
        /// Checks if the given Transform's GameObject's layer is contained within the LayerMask.
        /// </summary>
        /// <param name="mask">The LayerMask to check against.</param>
        /// <param name="transform">The Transform whose GameObject's layer to check for.</param>
        /// <returns>True if the Transform's GameObject's layer is in the mask, false otherwise.</returns>
        public static bool Contains(this LayerMask mask, Transform transform)
        {
            return mask.Contains(transform.gameObject);
        }

        /// <summary>
        /// Returns the index of the first enabled layer in the given LayerMask.
        /// </summary>
        /// <param name="mask">The LayerMask to check.</param>
        /// <returns>The index of the first enabled layer, or 0 if no layers are enabled. Returns -1 if this function is somehow broken.</returns>
        public static int ToSingleLayer(this LayerMask mask)
        {
            int value = mask.value;

            if (value == 0) return 0;

            for (int l = 1; l < 32; l++)
            {
                if ((value & (1 << l)) != 0) return l;
            }

            return -1; // Should not be reachable, but included for completeness.
        }
    }
}