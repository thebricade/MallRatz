using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class InteractionBlocker : Interactable
    {
        public override void Interact(IInteractor interactor) { }

        public override void OnFocused()
        {
            UIEvents.RaiseInteractionAvailable(false); // Hide the interact button.
        }
    }
}
