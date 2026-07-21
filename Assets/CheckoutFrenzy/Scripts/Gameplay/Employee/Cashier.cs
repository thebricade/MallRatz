using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Cashier : Employee
    {
        public override EmployeeType Type => EmployeeType.Cashier;

        protected override Box targetBox { get => null; set { } } // Unused

        protected override void InitializeNavMeshAgent()
        {
            // The cashier doesn't need to move, so the NavMeshAgent is disabled.
            agent.enabled = false;
        }

        protected override void Start()
        {
            // Intentionally left blank.  
            // The cashier's "Work" is managed by the CheckoutCounter.
        }

        protected override void CheckDoors()
        {
            // Intentionally left blank.
            // The cashier does not interact with doors.
        }

        /// <summary>
        /// Triggers the "Take" animation on the Cashier,
        /// visually representing the action of receiving payment.
        /// </summary>
        public void TakePayment()
        {
            animator.SetTrigger("Take");
        }
    }
}
