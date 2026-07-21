using UnityEngine;
using UnityEngine.Localization;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField, Tooltip("The localized text displayed in the UI when the player focuses on this object.")]
        protected LocalizedString interactionHint;

        protected virtual void Awake()
        {
            gameObject.layer = GameConfig.Instance.InteractableLayer.ToSingleLayer();
        }

        public abstract void Interact(IInteractor interactor);

        public virtual void OnFocused()
        {
            if (interactionHint != null && !interactionHint.IsEmpty)
            {
                UIEvents.RaiseInteractMessage(interactionHint.GetLocalizedString());
            }
        }

        public virtual void OnDefocused()
        {
            UIEvents.RaiseInteractMessage("");
        }
    }
}
