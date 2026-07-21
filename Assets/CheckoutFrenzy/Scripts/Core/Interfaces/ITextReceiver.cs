using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface ITextReceiver
    {
        string Text { get; set; }
        Color Color { get; set; }
    }
}
