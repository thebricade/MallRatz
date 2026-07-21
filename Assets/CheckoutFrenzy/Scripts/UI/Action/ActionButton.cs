using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class ActionButton : MonoBehaviour, IActionUI, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("The type of action that will be performed on click.")]
        [SerializeField] private ActionType actionType;

        [Tooltip("Allow repeated clicks when holding.")]
        [SerializeField] private bool allowHold;

        [Tooltip("Initial interval between repeated clicks when holding.")]
        [SerializeField] private float initialHoldInterval = 0.5f;

        [Tooltip("The minimum interval between repeated clicks.")]
        [SerializeField] private float minHoldInterval = 0.1f;

        [Tooltip("How much to decrease the interval over time.")]
        [SerializeField] private float intervalDecreaseRate = 0.05f;

        public ActionType ActionType => actionType;

        private UnityEvent onClick = new UnityEvent();
        public UnityEvent OnClick => onClick;

        private bool isHolding = false;
        private float holdTimer = 0f;
        private float currentHoldInterval;
        private bool isClicked = false;

        private void Start()
        {
            currentHoldInterval = initialHoldInterval;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            ResetHoldState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHolding = true;
            isClicked = true;
            currentHoldInterval = initialHoldInterval;
            holdTimer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isClicked)
            {
                onClick?.Invoke();
            }
            ResetHoldState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetHoldState();
        }

        private void Update()
        {
            if (isHolding && allowHold)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= currentHoldInterval)
                {
                    onClick?.Invoke();
                    holdTimer = 0f;
                    currentHoldInterval = Mathf.Max(currentHoldInterval - intervalDecreaseRate, minHoldInterval);
                    isClicked = false;
                }
            }
        }

        private void ResetHoldState()
        {
            isHolding = false;
            isClicked = false;
            holdTimer = 0f;
            currentHoldInterval = initialHoldInterval;
        }
    }
}
