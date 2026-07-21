using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class LicenseListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Text displaying the license name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the license price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Parent container where product entries unlocked by this license are displayed.")]
        private Transform productsContent;

        [SerializeField, Tooltip("Button to purchase the license.")]
        private Button purchaseButton;

        [Header("Requirements")]
        [SerializeField, Tooltip("Text indicating whether this license is available for purchase.")]
        private TMP_Text availabilityText;

        [SerializeField, Tooltip("Text displaying the level required to purchase this license.")]
        private TMP_Text levelReqText;

        [SerializeField, Tooltip("Text displaying the prerequisite license required for purchase.")]
        private TMP_Text licenseReqText;

        [SerializeField, Tooltip("UI element shown when this license is already owned.")]
        private GameObject ownedText;

        [Header("Localization")]
        [SerializeField] private LocalizedString priceLabel;
        [SerializeField] private LocalizedString availabilityLabel;
        [SerializeField] private LocalizedString levelReqLabel;
        [SerializeField] private LocalizedString licenseReqLabel;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private License license;

        private void OnEnable()
        {
            priceLabel.StringChanged += OnPriceChanged;
            availabilityLabel.StringChanged += OnAvailabilityChanged;
            levelReqLabel.StringChanged += OnLevelReqChanged;
            licenseReqLabel.StringChanged += OnLicenseReqChanged;
        }

        private void OnDisable()
        {
            priceLabel.StringChanged -= OnPriceChanged;
            availabilityLabel.StringChanged -= OnAvailabilityChanged;
            levelReqLabel.StringChanged -= OnLevelReqChanged;
            licenseReqLabel.StringChanged -= OnLicenseReqChanged;
        }

        /// <summary>
        /// Initializes the license listing with the license's details.
        /// </summary>
        /// <param name="license">The License object to display.</param>
        public void Initialize(License license)
        {
            this.license = license;
            nameText.text = license.Name;

            foreach (var product in license.Products)
            {
                if (product == null)
                {
                    Debug.LogWarning($"[LicenseListing] Null product found in license '{license.Name}'. Please check the license asset.");
                    continue;
                }

                var entryObj = new GameObject(product.Name);
                var entry = entryObj.AddComponent<TextMeshProUGUI>();
                entry.transform.SetParent(productsContent, false);
                entry.text += $"\u2022 {product.Name} ({product.Section})";
                entry.fontSize = 24;
            }

            // Disable purchasing if the license is already owned.
            if (license.IsOwnedByDefault || IsLicensePurchased(license))
            {
                DisablePurchasing();
            }
            else
            {
                // Subscribe to the OnLevelUp event to update purchase availability.
                DataManager.Instance.OnLevelUp += UpdatePurchaseAvailability;
                UpdatePurchaseAvailability(DataManager.Instance.Data.CurrentLevel);

                StoreManager.Instance.OnLicensePurchased += HandleLicensePurchased;
            }

            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            if (license == null) return;

            bool meetsLevelRequirement = DataManager.Instance.Data.CurrentLevel >= license.Level;

            bool hasRequiredLicense = license.RequiredLicense == null ||
                                      license.RequiredLicense.IsOwnedByDefault ||
                                      IsLicensePurchased(license.RequiredLicense);

            bool isAvailable = meetsLevelRequirement && hasRequiredLicense;

            priceLabel.Arguments = new object[]
            {
                currencySymbol,
                license.Price
            };
            priceLabel.RefreshString();

            availabilityLabel.Arguments = new object[]
            {
                isAvailable
            };
            availabilityLabel.RefreshString();

            levelReqLabel.Arguments = new object[]
            {
                license.Level
            };
            levelReqLabel.RefreshString();

            if (license.RequiredLicense == null)
            {
                licenseReqText.gameObject.SetActive(false);
            }
            else
            {
                licenseReqText.gameObject.SetActive(true);

                licenseReqLabel.Arguments = new object[]
                {
                    license.RequiredLicense.Name
                };

                licenseReqLabel.RefreshString();
            }

            availabilityText.color = isAvailable ? Color.green : Color.red;
            levelReqText.color = meetsLevelRequirement ? Color.green : Color.red;
            licenseReqText.color = hasRequiredLicense ? Color.green : Color.red;

            purchaseButton.interactable = isAvailable;
        }

        private bool IsLicensePurchased(License license)
        {
            foreach (var product in license.Products)
            {
                if (!DataManager.Instance.Data.LicensedProducts.Contains(product.ProductID))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Disables the purchase button and updates the requirement text when a license is already owned.
        /// </summary>
        private void DisablePurchasing()
        {
            ownedText.SetActive(true);

            purchaseButton.gameObject.SetActive(false);

            availabilityText.gameObject.SetActive(false);
            levelReqText.gameObject.SetActive(false);
            licenseReqText.gameObject.SetActive(false);
        }

        private void HandleLicensePurchased(License _)
        {
            UpdatePurchaseAvailability(DataManager.Instance.Data.CurrentLevel);
        }

        /// <summary>
        /// Updates the purchase availability (interactability of the purchase button) based on the player / store level.
        /// </summary>
        /// <param name="level">The player / store current level.</param>
        private void UpdatePurchaseAvailability(int level)
        {
            RefreshLocalization();

            if (purchaseButton.interactable)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(() => HandlePurchase(license));
                DataManager.Instance.OnLevelUp -= UpdatePurchaseAvailability;
                StoreManager.Instance.OnLicensePurchased -= HandleLicensePurchased;
            }
        }

        /// <summary>
        /// Handles the purchase of the license.
        /// </summary>
        /// <param name="license">The License being purchased.</param>
        private void HandlePurchase(License license)
        {
            bool isPurchased = StoreManager.Instance.PurchaseLicense(license); // Attempt purchase.
            if (isPurchased) DisablePurchasing(); // Update UI if purchase successful.
        }

        private void OnPriceChanged(string s) => priceText.text = s;
        private void OnAvailabilityChanged(string s) => availabilityText.text = s;
        private void OnLevelReqChanged(string s) => levelReqText.text = s;
        private void OnLicenseReqChanged(string s) => licenseReqText.text = s;
    }
}
