namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class Loan
    {
        public string DisplayName { get; set; }
        public decimal Principal { get; set; }
        public decimal InterestRate { get; set; }
        public int TotalPayments { get; set; }
        public int PaymentInterval { get; set; } = 1;
        public int PaymentsMade { get; set; }
        public int StartDay { get; set; }
        public decimal LatePayment { get; set; }

        public bool IsCompleted => PaymentsMade >= TotalPayments;

        public decimal PaymentAmount =>
            (Principal + (Principal * InterestRate)) / TotalPayments;

        public int NextPaymentDay =>
            StartDay + (PaymentsMade * PaymentInterval);
    }
}
