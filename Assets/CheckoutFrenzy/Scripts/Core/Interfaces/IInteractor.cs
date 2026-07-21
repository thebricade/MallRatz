using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IInteractor
    {
        Vector3 Position { get; }
        Transform HoldPoint { get; }
        PlayerStateManager StateManager { get; }
        void SetFOVSmooth(float target, float duration = 0.5f);
        Vector3 GetFrontPosition();
        T GetInteractorComponent<T>() where T : class;
    }
}
