using UnityEngine.Events;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IActionUI
    {
        ActionType ActionType { get; }
        UnityEvent OnClick { get; }
        void SetActive(bool active);
    }
}
