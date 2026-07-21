using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class ProductListing : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("Image displaying the product icon.")]
        private Image iconImage;

        [SerializeField, Tooltip("Text displaying the product name.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the product category.")]
        private TMP_Text categoryText;

        [SerializeField, Tooltip("Text displaying the quantity per box/pack.")]
        private TMP_Text quantityText;

        [SerializeField, Tooltip("Text displaying the product section.")]
        private TMP_Text sectionText;

        [SerializeField, Tooltip("Text displaying the price per box/pack.")]
        private TMP_Text priceText;

        [SerializeField, Tooltip("Text displaying the selected amount of boxes/packs.")]
        private TMP_Text amountText;

        [SerializeField, Tooltip("Text displaying the total price of the selected amount.")]
        private TMP_Text totalText;

        [Header("Buttons")]
        [SerializeField, Tooltip("Button to decrease the selected amount.")]
        private Button decreaseButton;

        [SerializeField, Tooltip("Button to increase the selected amount.")]
        private Button increaseButton;

        [SerializeField, Tooltip("Button to add the selected product amount to the cart.")]
        private Button addToCartButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString sectionLabel;
        [SerializeField] private LocalizedString priceLabel;
        [SerializeField] private LocalizedString amountLabel;
        [SerializeField] private LocalizedString totalLabel;
        [SerializeField] private LocalizedString quantityLabel;

        public ProductCategory Category { get; private set; }

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private Product product;
        private int amount = 1;
        private int boxQuantity;
        private decimal singlePrice;

        private void OnEnable()
        {
            // Subscribe to the change events
            sectionLabel.StringChanged += OnSectionLabelChanged;
            priceLabel.StringChanged += OnPriceLabelChanged;
            amountLabel.StringChanged += OnAmountLabelChanged;
            totalLabel.StringChanged += OnTotalLabelChanged;
            quantityLabel.StringChanged += OnQuantityLabelChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks/errors
            sectionLabel.StringChanged -= OnSectionLabelChanged;
            priceLabel.StringChanged -= OnPriceLabelChanged;
            amountLabel.StringChanged -= OnAmountLabelChanged;
            totalLabel.StringChanged -= OnTotalLabelChanged;
            quantityLabel.StringChanged -= OnQuantityLabelChanged;
        }

        /// <summary>
        /// Initializes the product listing with the product's details.
        /// </summary>
        /// <param name="product">The Product object to display.</param>
        public void Initialize(Product product)
        {
            this.product = product;
            Category = product.Category;

            iconImage.sprite = product.Icon;
            nameText.text = product.Name;

            // Format the category name for display (e.g., "ProductCategory" to "Product Category").
            var categoryName = product.Category.ToString();
            var formattedName = Regex.Replace(categoryName, @"([a-z])([A-Z])", "$1 $2"); // Add spaces between words.
            formattedName = Regex.Replace(formattedName, @"\bAnd\b", "&"); // Replace "And" with "&".
            categoryText.text = formattedName;

            boxQuantity = product.GetBoxQuantity();
            singlePrice = product.Price * boxQuantity;

            RefreshLocalization();

            decreaseButton.onClick.AddListener(() => UpdateAmount(-1));
            increaseButton.onClick.AddListener(() => UpdateAmount(1));
            addToCartButton.onClick.AddListener(() => PC.Instance.AddToCart(product, amount));
        }

        /// <summary>
        /// Updates the selected amount and the total price.
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
            if (product == null) return;

            sectionLabel.Arguments = new object[] { product.Section };
            sectionLabel.RefreshString();

            priceLabel.Arguments = new object[] { currencySymbol, singlePrice };
            priceLabel.RefreshString();

            amountLabel.Arguments = new object[] { amount };
            amountLabel.RefreshString();

            totalLabel.Arguments = new object[] { currencySymbol, singlePrice * amount };
            totalLabel.RefreshString();

            quantityLabel.Arguments = new object[] { boxQuantity };
            quantityLabel.RefreshString();
        }

        private void OnSectionLabelChanged(string s) => sectionText.text = s;
        private void OnPriceLabelChanged(string s) => priceText.text = s;
        private void OnAmountLabelChanged(string s) => amountText.text = s;
        private void OnTotalLabelChanged(string s) => totalText.text = s;
        private void OnQuantityLabelChanged(string s) => quantityText.text = s;
    }
}
