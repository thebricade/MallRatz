namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IInteractable
    {
        void Interact(IInteractor interactor);
        void OnFocused();
        void OnDefocused();
    }
}
