using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [CreateAssetMenu(fileName = "NewLicense", menuName = "Checkout Frenzy/Store/License")]
    public class License : ScriptableObject
    {
        [SerializeField, Tooltip("Unique identifier for this license used in lookups.")]
        private int licenseId;

        [SerializeField, Tooltip("The display name of the license.")]
        private new string name;

        [SerializeField, Min(0), Tooltip("The cost to purchase this license in the store.")]
        private int price;

        [SerializeField, Min(0), Tooltip("The player / store level required to make this license available for purchase.")]
        private int level;

        [SerializeField, Tooltip("If true, the player starts the game with this license already unlocked.")]
        private bool isOwnedByDefault;

        [SerializeField, Tooltip("A prerequisite license that must be owned before this one can be purchased.")]
        private License requiredLicense;

        [SerializeField, Tooltip("The list of products that become available for ordering once this license is acquired.")]
        private List<Product> products;

        public int LicenseID => licenseId;
        public string Name => name;
        public int Price => price;
        public int Level => level;
        public bool IsOwnedByDefault => isOwnedByDefault;
        public License RequiredLicense => requiredLicense;
        public List<Product> Products => products;
    }
}
