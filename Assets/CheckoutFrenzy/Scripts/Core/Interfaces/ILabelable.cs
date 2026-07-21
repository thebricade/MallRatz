namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface ILabelable
    {
        Product AssignedProduct { get; }
        DisplaySection DisplaySection { get; }
        void SetLabel(Product product);
    }
}
