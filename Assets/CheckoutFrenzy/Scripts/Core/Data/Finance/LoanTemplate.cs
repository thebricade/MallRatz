using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [CreateAssetMenu(fileName = "NewLoan", menuName = "Checkout Frenzy/Finance/Loan Template")]
    public class LoanTemplate : ScriptableObject
    {
        [SerializeField, Tooltip("The name displayed for this loan option in the UI.")]
        private string displayName = "Loan";

        [SerializeField, Tooltip("The initial amount of money given to the player when the loan is taken.")]
        private float principal;

        [SerializeField, Range(0f, 1f), Tooltip("The interest rate applied to the loan. For example, 0.5 means 50% interest.")]
        private float interestRate = 0.5f;

        [SerializeField, Tooltip("The total number of repayments the player must make.")]
        private int totalPayments;

        [SerializeField, Tooltip("How many in-game days between each loan payment.")]
        private int paymentInterval = 1;

        [SerializeField, Range(0f, 1f), Tooltip("The penalty percentage applied to each missed or late installment.")]
        private float penaltyMultiplier = 0.25f;

        [SerializeField, Tooltip("The player / store level required to unlock this loan.")]
        private int levelRequirement;

        public string DisplayName => displayName;
        public decimal Principal => (decimal)principal;
        public decimal InterestRate => (decimal)interestRate;
        public int TotalPayments => totalPayments;
        public int PaymentInterval => paymentInterval;
        public int LevelRequirement => levelRequirement;

        public decimal LateFeePerInstallment =>
            ((Principal + (Principal * InterestRate)) / TotalPayments) * (decimal)penaltyMultiplier;
    }
}
