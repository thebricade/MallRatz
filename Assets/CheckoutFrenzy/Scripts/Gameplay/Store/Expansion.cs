using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Expansion : MonoBehaviour
    {
        [SerializeField, Tooltip("The display name of the expansion.")]
        private new string name;

        [SerializeField, Tooltip("The price in currency to unlock this expansion.")]
        private int unlockPrice;

        [SerializeField, Tooltip("The minimum level required to unlock this expansion.")]
        private int requiredLevel;

        [SerializeField, Tooltip("The number of additional customers that this expansion adds to the store.")]
        private int additionalCustomers;

        [SerializeField, Tooltip("Whether this expansion unlocks the warehouse (storage area).")]
        private bool unlockWarehouse;

        public string Name => name;
        public int UnlockPrice => unlockPrice;
        public int RequiredLevel => requiredLevel;
        public int AdditionalCustomers => additionalCustomers;
        public bool UnlockWarehouse => unlockWarehouse;

        /// <summary>
        /// Sets the active state of all child GameObjects based on the `isPurchased` flag.
        /// 
        /// If `isPurchased` is true:
        ///     - Initially active children are deactivated.
        ///     - Initially inactive children are activated.
        /// 
        /// If `isPurchased` is false:
        ///     - Initially active children are activated.
        ///     - Initially inactive children are deactivated.
        /// </summary>
        /// <param name="isPurchased">Indicates whether the item is purchased.</param>
        public void SetPurchasedState(bool isPurchased)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(isPurchased != child.gameObject.activeSelf);
            }
        }
    }
}
