using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PriceCustomizer : UIPanel
    {
        [SerializeField, Tooltip("RectTransform of the main price customizer panel.")]
        private RectTransform mainPanel;

        [Header("Identity")]
        [SerializeField, Tooltip("Image to display the product icon.")]
        private Image productIconImage;

        [SerializeField, Tooltip("Text to display the product name.")]
        private TMP_Text productNameText;

        [Header("Price Labels")]
        [SerializeField, Tooltip("Text to display the current custom price of the product.")]
        private TMP_Text currentPriceText;

        [SerializeField, Tooltip("Text to display the default market price of the product.")]
        private TMP_Text marketPriceText;

        [SerializeField, Tooltip("Text to display the calculated profit based on the custom price.")]
        private TMP_Text profitText;

        [SerializeField, Tooltip("Text to display the currently set custom price.")]
        private TMP_Text customPriceText;

        [Header("Price Controls")]
        [SerializeField, Tooltip("Slider to adjust the custom price.")]
        private Slider priceSlider;

        [SerializeField, Tooltip("Button to decrease the custom price (by one cent).")]
        private Button decreaseButton;

        [SerializeField, Tooltip("Button to increase the custom price (by one cent).")]
        private Button increaseButton;

        [SerializeField, Tooltip("Button to confirm and save the custom price.")]
        private Button confirmButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString currentPriceLabel;
        [SerializeField] private LocalizedString marketPriceLabel;
        [SerializeField] private LocalizedString profitLabel;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private Product product;
        private CustomPrice customPrice;

        protected override void Awake()
        {
            base.Awake();

            priceSlider.onValueChanged.AddListener((value) =>
            {
                customPrice.PriceInCents = (long)value;
                RefreshUI();
            });

            mainPanel.anchoredPosition = Vector2.zero;
            HideUI();
        }

        private void OnEnable()
        {
            StoreEvents.OnPriceCustomizerRequested += Open;

            currentPriceLabel.StringChanged += UpdateCurrentPriceText;
            marketPriceLabel.StringChanged += UpdateMarketPriceText;
            profitLabel.StringChanged += UpdateProfitText;
        }

        private void OnDisable()
        {
            StoreEvents.OnPriceCustomizerRequested -= Open;

            currentPriceLabel.StringChanged -= UpdateCurrentPriceText;
            marketPriceLabel.StringChanged -= UpdateMarketPriceText;
            profitLabel.StringChanged -= UpdateProfitText;
        }

        private void RefreshUI()
        {
            if (product == null || customPrice == null) return;

            decimal price = customPrice.PriceInCents / 100m;
            customPriceText.text = $"{currencySymbol}{price:N2}";

            UpdateCurrentPriceText(currentPriceLabel.GetLocalizedString());
            UpdateMarketPriceText(marketPriceLabel.GetLocalizedString());
            UpdateProfitText(profitLabel.GetLocalizedString());
        }

        private void UpdateCurrentPriceText(string localizedValue)
        {
            if (product == null) return;
            decimal productPrice = DataManager.Instance.GetCustomProductPrice(product);

            currentPriceText.text = currentPriceLabel.GetLocalizedString(
                currencySymbol,
                productPrice.ToString("N2")
            );
        }

        private void UpdateMarketPriceText(string localizedValue)
        {
            if (product == null) return;
            marketPriceText.text = marketPriceLabel.GetLocalizedString(
                currencySymbol,
                product.MarketPrice.ToString("N2")
            );
        }

        private void UpdateProfitText(string localizedValue)
        {
            if (product == null || customPrice == null) return;

            decimal price = customPrice.PriceInCents / 100m;
            decimal profit = price - product.Price;

            profitText.text = profitLabel.GetLocalizedString(
                currencySymbol,
                profit.ToString("N2")
            );

            profitText.color = profit > 0 ? Color.green : profit < 0 ? Color.red : Color.white;
        }

        private void Open(Product product)
        {
            this.product = product;
            customPrice = new CustomPrice { ProductId = product.ProductID };

            productIconImage.sprite = product.Icon;
            productNameText.text = product.Name;

            decimal defaultPrice = product.Price;
            decimal currentSavedPrice = DataManager.Instance.GetCustomProductPrice(product);

            float minValue = Mathf.FloorToInt((float)defaultPrice * 50f);
            float maxValue = Mathf.FloorToInt((float)defaultPrice * 200f);

            priceSlider.minValue = minValue;
            priceSlider.maxValue = maxValue;
            priceSlider.value = (float)currentSavedPrice * 100;
            customPrice.PriceInCents = (long)priceSlider.value;

            SetupButtons(minValue, maxValue);

            RefreshUI();
            UIEvents.RaiseInteractionAvailable(false);
            ShowUI();
        }

        private void SetupButtons(float min, float max)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(() =>
            {
                if (priceSlider.value > min) priceSlider.value--;
                AudioManager.Instance.PlaySFX(AudioID.Click);
            });

            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(() =>
            {
                if (priceSlider.value < max) priceSlider.value++;
                AudioManager.Instance.PlaySFX(AudioID.Click);
            });

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                DataManager.Instance.AddCustomProductPrice(customPrice);
                UIEvents.RaiseActionUI(ActionType.Return, false, null);
                AudioManager.Instance.PlaySFX(AudioID.Click);
                Close();
            });

            UIEvents.RaiseActionUI(ActionType.Return, true, () =>
            {
                UIEvents.RaiseActionUI(ActionType.Return, false, null);
                Close();
            });
        }

        private void Close()
        {
            product = null;
            customPrice = null;
            HideUI();
        }
    }
}
