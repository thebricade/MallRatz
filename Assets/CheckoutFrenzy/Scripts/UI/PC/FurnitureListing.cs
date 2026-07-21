using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class FurnitureListing : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("Image displaying the furniture icon.")]
        private Image iconImage;

        [SerializeField, Tooltip("Text displaying the furniture name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the furniture section.")]
        private TMP_Text sectionText;

        [SerializeField, Tooltip("Text displaying the furniture price.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the selected amount of furniture.")]
        private TMP_Text amountText;

        [SerializeField, Tooltip("Text displaying the total price of the selected furniture amount.")]
        private TMP_Text totalText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button to decrease the selected amount.")]
        private Button decreaseButton;

        [SerializeField, Tooltip("Button to increase the selected amount.")]
        private Button increaseButton;

        [SerializeField, Tooltip("Button to add the selected furniture to the cart.")]
        private Button addToCartButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString sectionLabel;
        [SerializeField] private LocalizedString priceLabel;
        [SerializeField] private LocalizedString amountLabel;
        [SerializeField] private LocalizedString totalLabel;

        public DisplaySection Section { get; private set; }

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private Furniture furniture;
        private int amount;
        private decimal price;

        private void OnEnable()
        {
            sectionLabel.StringChanged += OnSectionLabelChanged;
            priceLabel.StringChanged += OnPriceLabelChanged;
            amountLabel.StringChanged += OnAmountLabelChanged;
            totalLabel.StringChanged += OnTotalLabelChanged;
        }

        private void OnDisable()
        {
            sectionLabel.StringChanged -= OnSectionLabelChanged;
            priceLabel.StringChanged -= OnPriceLabelChanged;
            amountLabel.StringChanged -= OnAmountLabelChanged;
            totalLabel.StringChanged -= OnTotalLabelChanged;
        }

        /// <summary>
        /// Initializes the furniture listing with the furniture's details.
        /// </summary>
        /// <param name="furniture">The Furniture object to display.</param>
        public void Initialize(Furniture furniture)
        {
            this.furniture = furniture;
            this.Section = furniture.Section;

            iconImage.sprite = furniture.Icon;
            nameText.text = furniture.Name;

            // Hide the section text if the furniture is in the General section (e.g., Trash Can, Decorations).
            if (furniture.Section == DisplaySection.General)
                sectionText.gameObject.SetActive(false);
            else
                sectionText.gameObject.SetActive(true);

            price = furniture.Price;
            amount = 1; // Default starting amount

            RefreshLocalization();

            decreaseButton.onClick.AddListener(() => UpdateAmount(-1));
            increaseButton.onClick.AddListener(() => UpdateAmount(1));
            addToCartButton.onClick.AddListener(() => PC.Instance.AddToCart(furniture, amount));
        }

        /// <summary>
        /// Updates the selected amount of furniture and the total price.
        /// </summary>
        /// <param name="value">The amount to change the selected quantity by (positive or negative).</param>
        /// <param name="playSFX">Whether to play a sound effect (defaults to true).</param>
        private void UpdateAmount(int value, bool playSFX = true)
        {
            amount = Mathf.Clamp(amount + value, 1, 10);

            RefreshLocalization();

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void RefreshLocalization()
        {
            if (furniture == null) return;

            sectionLabel.Arguments = new object[] { furniture.Section };
            sectionLabel.RefreshString();

            priceLabel.Arguments = new object[] { currencySymbol, price };
            priceLabel.RefreshString();

            amountLabel.Arguments = new object[] { amount };
            amountLabel.RefreshString();

            totalLabel.Arguments = new object[] { currencySymbol, price * amount };
            totalLabel.RefreshString();
        }

        private void OnSectionLabelChanged(string s) => sectionText.text = s;
        private void OnPriceLabelChanged(string s) => priceText.text = s;
        private void OnAmountLabelChanged(string s) => amountText.text = s;
        private void OnTotalLabelChanged(string s) => totalText.text = s;
    }
}
