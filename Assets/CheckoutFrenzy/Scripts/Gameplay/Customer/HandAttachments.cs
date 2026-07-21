using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class HandAttachments : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform used to position and orient products held in the customer's hand.")]
        private Transform grip;

        [SerializeField, Tooltip("GameObject representing the cash payment option.")]
        private GameObject cash;

        [SerializeField, Tooltip("GameObject representing the card payment option.")]
        private GameObject card;

        public Transform Grip => grip;
        public GameObject Cash => cash;
        public GameObject Card => card;

        private void Awake()
        {
            cash.layer = card.layer = GameConfig.Instance.PaymentLayer.ToSingleLayer();

            DeactivatePaymentObjects();
        }

        /// <summary>
        /// Activates the appropriate payment method GameObject (either cash or card) based on the isUsingCash boolean.
        /// </summary>
        /// <param name="isUsingCash">True to activate the cash payment object, false to activate the card payment object.</param>
        public void ActivatePaymentObject(bool isUsingCash)
        {
            if (isUsingCash) cash.SetActive(true);
            else card.SetActive(true);
        }

        /// <summary>
        /// Deactivates both the cash and card payment method GameObjects.
        /// </summary>
        public void DeactivatePaymentObjects()
        {
            cash.SetActive(false);
            card.SetActive(false);
        }
    }
}
