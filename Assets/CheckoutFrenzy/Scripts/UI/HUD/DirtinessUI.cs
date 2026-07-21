using UnityEngine;
using UnityEngine.UI;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class DirtinessUI : MonoBehaviour
    {
        [SerializeField] private Image dirtinessFill;
        [SerializeField] private Gradient dirtinessGradient;

        private void Start()
        {
            StoreManager.Instance.OnCleanablesChanged += UpdateDirtinessFill;
            UpdateDirtinessFill(StoreManager.Instance.Dirtiness);
        }

        private void UpdateDirtinessFill(float amount)
        {
            dirtinessFill.fillAmount = amount;
            dirtinessFill.color = dirtinessGradient.Evaluate(amount);
        }
    }
}
