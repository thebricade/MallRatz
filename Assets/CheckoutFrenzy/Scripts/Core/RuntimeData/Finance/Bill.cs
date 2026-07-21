namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class Bill
    {
        public BillType Type { get; set; }
        public int IssueDay { get; set; }
        public int DueDay { get; set; }
        public int GracePeriodDays { get; set; }
        public decimal Amount { get; set; }
        public decimal LatePenalty { get; set; }

        public BillStatus Status { get; set; } = BillStatus.Unpaid;

        public bool IsPaid => Status == BillStatus.Paid;

        public bool IsOverdue(int currentDay) =>
            !IsPaid && currentDay > DueDay + GracePeriodDays;

        public bool IsInGracePeriod(int currentDay) =>
            !IsPaid && currentDay > DueDay && currentDay <= DueDay + GracePeriodDays;

        public decimal GetTotalAmountDue(int currentDay)
        {
            if (IsPaid) return 0m;
            return IsOverdue(currentDay) ? Amount + LatePenalty : Amount;
        }
    }
}
