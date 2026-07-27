using UnityEngine;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class StoreSign : Interactable
    {
        private bool isOpen;

        // StoreSign is a fixed child within a shop's hierarchy, so a cached hierarchy lookup is sufficient here.
        private StoreManager storeManager;

        protected override void Awake()
        {
            base.Awake();
            storeManager = GetComponentInParent<StoreManager>();
        }

        public override void Interact(IInteractor interactor)
        {
            if (DOTween.IsTweening(transform)) return;

            isOpen = !isOpen;
            storeManager?.ToggleOpenState(isOpen);

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
