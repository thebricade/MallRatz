using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Thief : Interactable
    {
        private Customer customer;

        public void Initialize(Customer customer)
        {
            this.customer = customer;
        }

        public override void Interact(IInteractor interactor)
        {
            var guard = interactor.GetInteractorComponent<Guard>();
            if (guard == null) return;

            guard.TryHit(customer);
            UIEvents.RaiseInteractMessage("");
        }
    }
}
