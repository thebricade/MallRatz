namespace CryingSnow.CheckoutFrenzy.Core
{
    public enum PaymentStyle
    {
        Random,          // Will resolve to one of the below at the counter
        ExactChange,     // Pays the exact decimal amount
        RoundUp,         // Nearest denomination (e.g., $1.45 -> $2.00)
        SmallestExcess,  // Next available higher bill/coin (e.g., $14 -> $20)
        BigBills,        // Significant jump (e.g., $14 -> $50 or $100)
        ChangeOptimizer  // Pays extra cents to get a cleaner bill back (e.g., $5.30 -> $10.30 for a $5 change)
    }
}
