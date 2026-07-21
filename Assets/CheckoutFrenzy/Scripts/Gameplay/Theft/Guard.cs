using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public class Guard : MonoBehaviour
    {
        [SerializeField] private float hitDistance = 1.0f;
        [SerializeField] private GameObject batObject;

        public float HitDistance => hitDistance;

        private Animator animator;
        private Customer customer;

        private bool isHitting;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            batObject.SetActive(false);
        }

        public void TryHit(Customer customer)
        {
            if (isHitting) return;

            this.customer = customer;

            isHitting = true;
            batObject.SetActive(true);

            animator.SetTrigger("Hit");
        }

        public void OnSwing(AnimationEvent _)
        {
            AudioManager.Instance.PlaySFX(AudioID.Swing, transform.position);
        }

        public void OnHit(AnimationEvent _)
        {
            if (customer == null) return;

            if (Vector3.Distance(transform.position, customer.transform.position) <= hitDistance)
            {
                customer.CatchCustomer();
                customer = null;
                AudioManager.Instance.PlaySFX(AudioID.Hit, transform.position);
            }
        }

        public void OnHitEnd(AnimationEvent _)
        {
            isHitting = false;
            batObject.SetActive(false);
        }
    }
}
