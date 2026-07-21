using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class CartItem : MonoBehaviour
    {
        [SerializeField, Tooltip("Text displaying the name of the item.")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("Text displaying the quantity of the item.")]
        private TMP_Text amountText;

        [SerializeField, Tooltip("Text displaying the total price of the item(s).")]
        private TMP_Text totalText;

        [SerializeField, Tooltip("Button to remove the item from the cart.")]
        private Button removeButton;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        /// <summary>
        /// Initializes the cart item with the purchasable item's details and quantity.
        /// </summary>
        /// <param name="purchasable">The IPurchasable item to display.</param>
        /// <param name="amount">The quantity of the item in the cart.</param>
        public void Initialize(IPurchasable purchasable, int amount)
        {
            nameText.text = purchasable.Name;

            amountText.text = amount.ToString();

            // Calculate and set the total price based on item type and quantity.
            int quantity = purchasable is Product product ? product.GetBoxQuantity() : 1; // Get box quantity if it's a product.
            decimal total = purchasable.Price * quantity * amount; // Calculate total price.
            totalText.text = $"{currencySymbol}{total:N2}"; // Set the total price text, formatted to two decimal places.

            // Add a listener to the remove button to remove the item from the cart and destroy the cart item GameObject.
            removeButton.onClick.AddListener(() =>
            {
                PC.Instance.RemoveFromCart(purchasable); // Remove from cart data.
                Destroy(gameObject); // Remove the cart item UI element.
            });
        }
    }
}
