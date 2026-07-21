using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IPurchasable
    {
        string Name { get; }
        Sprite Icon { get; }
        decimal Price { get; }
        int OrderTime { get; }
        DisplaySection Section { get; }
    }
}
