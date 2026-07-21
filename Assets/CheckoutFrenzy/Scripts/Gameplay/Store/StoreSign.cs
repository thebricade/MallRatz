using UnityEngine;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoreSign : Interactable
    {
        private bool isOpen;

        public override void Interact(IInteractor interactor)
        {
            if (DOTween.IsTweening(transform)) return;

            isOpen = !isOpen;
            StoreManager.Instance.ToggleOpenState(isOpen);

            UpdateSignRotation();
            UIEvents.RaiseInteractMessage("");
        }

        private void UpdateSignRotation()
        {
            float targetAngle = isOpen ? 180f : 0f;
            transform.DOLocalRotate(Vector3.up * targetAngle, 0.5f);
            AudioManager.Instance.PlaySFX(AudioID.Flip);
        }
    }
}
