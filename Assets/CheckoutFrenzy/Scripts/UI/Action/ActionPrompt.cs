using UnityEngine;
using UnityEngine.Events;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class ActionPrompt : MonoBehaviour, IActionUI
    {
        [Tooltip("The key that triggers the action when pressed.")]
        [SerializeField] private KeyCode key = KeyCode.K;

        [Tooltip("The type of action that will be performed on key down.")]
        [SerializeField] private ActionType actionType;

        [Tooltip("Determines whether holding the key will repeatedly invoke the action.")]
        [SerializeField] private bool allowHold;

        [Tooltip("Initial interval between repeated invokes when holding.")]
        [SerializeField] private float initialHoldInterval = 0.5f;

        [Tooltip("The minimum interval between repeated invokes.")]
        [SerializeField] private float minHoldInterval = 0.1f;

        [Tooltip("How much to decrease the interval over time.")]
        [SerializeField] private float intervalDecreaseRate = 0.05f;

        public ActionType ActionType => actionType;

        private UnityEvent onClick = new UnityEvent();
        public UnityEvent OnClick => onClick;

        private bool isGamePaused => Time.timeScale < 1f;
        private bool isHolding = false;
        private float holdTimer = 0f;
        private float currentHoldInterval;

        private void Start()
        {
            currentHoldInterval = initialHoldInterval;
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            ResetHoldState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(key))
            {
                if (isGamePaused) return;

                onClick?.Invoke();

                if (allowHold)
                {
                    isHolding = true;
                    currentHoldInterval = initialHoldInterval;
                    holdTimer = 0f;
                }
            }

            if (Input.GetKeyUp(key))
            {
                ResetHoldState();
            }

            if (isHolding && allowHold)
            {
                holdTimer += Time.deltaTime;

                if (holdTimer >= currentHoldInterval)
                {
                    if (isGamePaused) return;

                    onClick?.Invoke();
                    holdTimer = 0f;
                    currentHoldInterval = Mathf.Max(currentHoldInterval - intervalDecreaseRate, minHoldInterval);
                }
            }
        }

        private void ResetHoldState()
        {
            isHolding = false;
            holdTimer = 0f;
            currentHoldInterval = initialHoldInterval;
        }
    }
}
