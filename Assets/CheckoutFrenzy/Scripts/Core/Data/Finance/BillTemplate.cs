using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [CreateAssetMenu(fileName = "NewBill", menuName = "Checkout Frenzy/Finance/Bill Template")]
    public class BillTemplate : ScriptableObject
    {
        [SerializeField, Tooltip("The type of the bill.")]
        private BillType type;

        [SerializeField, Tooltip("How often this bill is issued, in in-game days.")]
        private int frequencyInDays = 1;

        [SerializeField, Tooltip("How many days after the bill is issued it becomes due.")]
        private int dueOffset = 1;

        [SerializeField, Tooltip("Number of grace period days after the due date before a penalty is applied.")]
        private int gracePeriodDays = 1;

        [SerializeField, Tooltip("Base amount the player must pay for this bill.")]
        private float amount = 100f;

        [SerializeField, Range(0f, 1f), Tooltip("Percentage of the base amount added as a late penalty if not paid during the grace period.")]
        public float penaltyMultiplier = 0.25f;

        public BillType Type => type;
        public int FrequencyInDays => frequencyInDays;
        public int DueOffset => dueOffset;
        public int GracePeriodDays => gracePeriodDays;
        public decimal Amount => (decimal)amount;
        public decimal LatePenalty => (decimal)(amount * penaltyMultiplier);
    }
}
