using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public interface IPersistentEntity
    {
        int EntityID { get; }
        Transform EntityTransform { get; }
    }
}
