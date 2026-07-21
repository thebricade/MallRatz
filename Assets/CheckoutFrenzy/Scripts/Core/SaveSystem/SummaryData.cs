namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class SummaryData
    {
        public int TotalCustomers { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal TotalRevenues { get; set; }
        public decimal TotalSpending { get; set; }

        public SummaryData() { }

        public SummaryData(decimal currentBalance)
        {
            PreviousBalance = currentBalance;
        }
    }
}
