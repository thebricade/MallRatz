using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class CheckoutItem : MonoBehaviour
    {
        public Product Product { get; private set; }

        private event System.Action onScan;

        /// <summary>
        /// Initializes the CheckoutItem with a product and a callback for scanning.
        /// </summary>
        public void Initialize(Product product, System.Action onScanCallback)
        {
            gameObject.layer = GameConfig.Instance.CheckoutItemLayer.ToSingleLayer();

            Product = product;
            onScan += onScanCallback;
        }

        /// <summary>
        /// Invokes the scan event.
        /// </summary>
        public void Scan()
        {
            if (onScan != null)
            {
                onScan.Invoke();
            }
            else
            {
                Debug.LogWarning("No onScan callback registered for this item.");
            }
        }
    }
}
