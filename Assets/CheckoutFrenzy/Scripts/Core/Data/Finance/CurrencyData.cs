using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class Denomination
    {
        [Tooltip("The value in cents (e.g., 500 for $5.00)")]
        public int value;

        [Tooltip("The visual representation of this bill or coin")]
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "NewCurrencyData", menuName = "Checkout Frenzy/Finance/Currency Data")]
    public class CurrencyData : ScriptableObject
    {
        [Tooltip("The symbol (e.g., $, €, £) used to prefix monetary values on the counter monitor.")]
        public string currencySymbol = "$";

        [Tooltip("List of available bills and coins, ordered from highest to lowest")]
        public List<Denomination> denominations;

        #region Public Financial API

        /// <summary>
        /// Checks if a value is within the range of the largest available denomination.
        /// </summary>
        public bool CanPayExactly(int amountCents)
        {
            if (denominations == null || denominations.Count == 0) return false;

            // Since list is highest to lowest, denominations[0] is the max possible bill/coin.
            return amountCents <= denominations[0].value;
        }

        /// <summary>
        /// Finds the closest denomination that is greater than or equal to the amount.
        /// (e.g., $4.50 becomes $5.00)
        /// </summary>
        public int GetRoundedUpAmount(int amountCents)
        {
            // Iterate backwards (lowest to highest) to find the smallest bill that covers the cost.
            for (int i = denominations.Count - 1; i >= 0; i--)
            {
                if (denominations[i].value >= amountCents)
                {
                    return denominations[i].value;
                }
            }
            return amountCents;
        }

        /// <summary>
        /// Finds the smallest denomination strictly higher than the amount.
        /// (e.g., $5.00 becomes $10.00)
        /// </summary>
        public int GetSmallestExcessAmount(int amountCents)
        {
            for (int i = denominations.Count - 1; i >= 0; i--)
            {
                if (denominations[i].value > amountCents)
                {
                    return denominations[i].value;
                }
            }
            return amountCents;
        }

        /// <summary>
        /// Calculates a payment total that aims to result in a "clean" bill as change.
        /// </summary>
        public int GetOptimizedTotal(int totalCents)
        {
            int baseBill = GetSmallestExcessAmount(totalCents);
            int targetChange = 0;

            // Find the largest denomination smaller than the base bill to use as "target change".
            foreach (var denom in denominations)
            {
                if (denom.value < baseBill)
                {
                    targetChange = denom.value;
                    break;
                }
            }

            return totalCents + targetChange;
        }

        #endregion
    }
}
