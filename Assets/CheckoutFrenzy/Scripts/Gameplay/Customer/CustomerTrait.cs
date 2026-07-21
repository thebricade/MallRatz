using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [CreateAssetMenu(fileName = "NewCustomerTrait", menuName = "Checkout Frenzy/Customer/Trait")]
    public class CustomerTrait : ScriptableObject
    {
        [Header("Movement Settings")]
        [Tooltip("The standard walking speed (Default: 1.5). Affects NavMeshAgent speed during shopping.")]
        public float walkSpeed = 1.5f;

        [Tooltip("Speed used when fleeing the store after stealing (Default: 2.5).")]
        public float runSpeed = 2.5f;

        [Header("Shopping Logic")]
        [Range(0f, 1f)]
        [Tooltip("Probability (0 to 1) that the customer will look for another item after picking one. Higher values mean more items per basket.")]
        public float continueShoppingChance = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Probability (0 to 1) that the customer will attempt to steal an item instead of buying it.")]
        public float stealChance = 0.1f;

        [Tooltip("Price sensitivity. X is the min and Y is the max multiplier of the Market Price. If the store price exceeds 'MarketPrice * Random(X, Y)', the customer won't buy.")]
        public Vector2 priceToleranceRange = new Vector2(1.0f, 2.0f);

        [Header("Payment Personality")]
        [Tooltip("How the customer chooses cash denominations. 'Random' will pick a specific style (Exact, RoundUp, or BigBills) at the counter.")]
        public PaymentStyle preferredPaymentStyle = PaymentStyle.Random;

        [Range(0f, 1f)]
        [Tooltip("Weighting for payment method. 1.0 is always Cash, 0.0 is always Card.")]
        public float cashPreference = 0.5f;

        [Header("Visuals & Social")]
        [Tooltip("Override the default animator controller (e.g., to give this trait a specific walk cycle like 'Elderly' or 'Hustle').")]
        public RuntimeAnimatorController animatorOverride;

        [Space]
        [Tooltip("Lines used when the customer cannot find a specific shelving unit or product.")]
        public Dialogue notFoundDialogue;

        [Tooltip("Lines used when the customer finds the price too high based on their Tolerance Range.")]
        public Dialogue overpricedDialogue;

        [Tooltip("Lines spoken when the player successfully catches this thief.")]
        public Dialogue caughtThiefDialogue;
    }
}
