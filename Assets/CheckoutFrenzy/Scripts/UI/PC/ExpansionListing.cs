using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class ExpansionListing : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("Text displaying the expansion name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the expansion price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the expansion description.")]
        private TMP_Text descriptionText;

        [SerializeField, Tooltip("Text displaying the expansion requirements.")]
        private TMP_Text requirementText;

        [SerializeField, Tooltip("Button to purchase the expansion.")]
        private Button purchaseButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString priceLabel;
        [SerializeField] private LocalizedString unlockWarehouseLabel;
        [SerializeField] private LocalizedString addRoomLabel;
        [SerializeField] private LocalizedString customerCapacityLabel;
        [SerializeField] private LocalizedString availabilityLabel;
        [SerializeField] private LocalizedString requiresLevelLabel;
        [SerializeField] private LocalizedString previousPurchasedLabel;
        [SerializeField] private LocalizedString purchasePreviousLabel;
        [SerializeField] private LocalizedString alreadyOwnedLabel;

        private Expansion expansion;
        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        // Cached localized strings to build composite text blocks
        private string priceStr, warehouseStr, roomStr, capacityStr, availStr, levelStr, prevPurchasedStr, prevReqStr, ownedStr;

        private void OnEnable()
        {
            priceLabel.StringChanged += OnPriceChanged;
            unlockWarehouseLabel.StringChanged += OnWarehouseChanged;
            addRoomLabel.StringChanged += OnRoomChanged;
            customerCapacityLabel.StringChanged += OnCapacityChanged;
            availabilityLabel.StringChanged += OnAvailabilityChanged;
            requiresLevelLabel.StringChanged += OnLevelReqChanged;
            previousPurchasedLabel.StringChanged += OnPrevPurchasedChanged;
            purchasePreviousLabel.StringChanged += OnPurchasePrevChanged;
            alreadyOwnedLabel.StringChanged += OnOwnedChanged;
        }

        private void OnDisable()
        {
            priceLabel.StringChanged -= OnPriceChanged;
            unlockWarehouseLabel.StringChanged -= OnWarehouseChanged;
            addRoomLabel.StringChanged -= OnRoomChanged;
            customerCapacityLabel.StringChanged -= OnCapacityChanged;
            availabilityLabel.StringChanged -= OnAvailabilityChanged;
            requiresLevelLabel.StringChanged -= OnLevelReqChanged;
            previousPurchasedLabel.StringChanged -= OnPrevPurchasedChanged;
            purchasePreviousLabel.StringChanged -= OnPurchasePrevChanged;
            alreadyOwnedLabel.StringChanged -= OnOwnedChanged;

            if (DataManager.Instance != null)
                DataManager.Instance.OnLevelUp -= UpdateRequirements;

            if (StoreManager.Instance != null)
                StoreManager.Instance.OnExpansionPurchased -= UpdateRequirements;
        }

        private void OnPriceChanged(string s) { priceStr = s; UpdateUI(); }
        private void OnWarehouseChanged(string s) { warehouseStr = s; UpdateUI(); }
        private void OnRoomChanged(string s) { roomStr = s; UpdateUI(); }
        private void OnCapacityChanged(string s) { capacityStr = s; UpdateUI(); }
        private void OnAvailabilityChanged(string s) { availStr = s; UpdateUI(); }
        private void OnLevelReqChanged(string s) { levelStr = s; UpdateUI(); }
        private void OnPrevPurchasedChanged(string s) { prevPurchasedStr = s; UpdateUI(); }
        private void OnPurchasePrevChanged(string s) { prevReqStr = s; UpdateUI(); }
        private void OnOwnedChanged(string s) { ownedStr = s; UpdateUI(); }

        public void Initialize(Expansion expansion)
        {
            this.expansion = expansion;
            nameText.text = expansion.Name;

            priceLabel.Arguments = new object[] { currencySymbol, expansion.UnlockPrice };
            customerCapacityLabel.Arguments = new object[] { expansion.AdditionalCustomers };
            requiresLevelLabel.Arguments = new object[] { expansion.RequiredLevel };

            RefreshAvailabilityArgs();

            if (StoreManager.Instance.IsExpansionPurchased(expansion))
            {
                UpdateUI();
            }
            else
            {
                DataManager.Instance.OnLevelUp += UpdateRequirements;
                StoreManager.Instance.OnExpansionPurchased += UpdateRequirements;
                UpdateRequirements(0);
            }
        }

        private void UpdateRequirements(int _)
        {
            RefreshAvailabilityArgs();
            UpdateUI();
        }

        private void RefreshAvailabilityArgs()
        {
            if (expansion == null) return;

            bool isLevelMet = DataManager.Instance.Data.CurrentLevel >= expansion.RequiredLevel;
            bool isCurrentExpansion = StoreManager.Instance.IsCurrentExpansion(expansion);
            bool isAvailable = isLevelMet && isCurrentExpansion;

            availabilityLabel.Arguments = new object[] { isAvailable };

            availabilityLabel.RefreshString();
            priceLabel.RefreshString();
            customerCapacityLabel.RefreshString();
            requiresLevelLabel.RefreshString();
        }

        private void UpdateUI()
        {
            if (expansion == null) return;

            priceText.text = priceStr;

            string desc = expansion.UnlockWarehouse ? $"\u2022 {warehouseStr}" : $"\u2022 {roomStr}";
            if (expansion.AdditionalCustomers > 0) desc += $"\n\u2022 {capacityStr}";
            descriptionText.text = desc;

            bool isPurchased = StoreManager.Instance.IsExpansionPurchased(expansion);
            bool isLevelMet = DataManager.Instance.Data.CurrentLevel >= expansion.RequiredLevel;
            bool isCurrentExpansion = StoreManager.Instance.IsCurrentExpansion(expansion);
            bool isAvailable = isLevelMet && isCurrentExpansion;

            if (isPurchased)
            {
                requirementText.text = GetColoredRequirement(ownedStr, true);
                purchaseButton.gameObject.SetActive(false);
            }
            else
            {
                string coloredAvail = GetColoredRequirement(availStr, isAvailable);
                string coloredLevel = GetColoredRequirement(levelStr, isLevelMet);

                string prevText = isCurrentExpansion ? prevPurchasedStr : prevReqStr;
                string coloredPrev = GetColoredRequirement(prevText, isCurrentExpansion);

                requirementText.text = $"{coloredAvail}\n{coloredLevel}\n{coloredPrev}";

                purchaseButton.gameObject.SetActive(true);
                purchaseButton.interactable = isAvailable;

                purchaseButton.onClick.RemoveAllListeners();
                if (isAvailable)
                {
                    purchaseButton.onClick.AddListener(HandlePurchase);
                }
            }
        }

        private string GetColoredRequirement(string text, bool isMet)
        {
            string colorTag = isMet ? "<color=#2ECC71>" : "<color=#E74C3C>";
            return $"{colorTag}{text}</color>";
        }

        private void HandlePurchase()
        {
            if (StoreManager.Instance.PurchaseExpansion())
            {
                UpdateUI();

                if (DataManager.Instance != null) DataManager.Instance.OnLevelUp -= UpdateRequirements;
                if (StoreManager.Instance != null) StoreManager.Instance.OnExpansionPurchased -= UpdateRequirements;
            }
        }
    }
}
