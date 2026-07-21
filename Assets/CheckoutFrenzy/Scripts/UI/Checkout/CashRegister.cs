using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class CashRegister : MonoBehaviour
    {
        [SerializeField, Tooltip("The button used to undo the given change.")]
        private Button undoButton;

        [SerializeField, Tooltip("The button used to clear the given change.")]
        private Button clearButton;

        [SerializeField, Tooltip("The button used to confirm the transaction.")]
        private Button confirmButton;

        private RectTransform rect;
        private float originalPosY;
        private bool allowDrawing;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosY = rect.anchoredPosition.y;

            undoButton.onClick.AddListener(() => CheckoutEvents.RaiseCashRegisterUndo());
            clearButton.onClick.AddListener(() => CheckoutEvents.RaiseCashRegisterClear());
            confirmButton.onClick.AddListener(() => CheckoutEvents.RaiseCashRegisterConfirm());
        }

        private void OnEnable()
        {
            CheckoutEvents.OnCashRegisterToggleRequested += HandleToggleRequest;
        }

        private void OnDisable()
        {
            CheckoutEvents.OnCashRegisterToggleRequested -= HandleToggleRequest;
        }

        private void OnDestroy()
        {
            undoButton.onClick.RemoveAllListeners();
            clearButton.onClick.RemoveAllListeners();
            confirmButton.onClick.RemoveAllListeners();
        }

        private void HandleToggleRequest(bool open)
        {
            if (open) Open();
            else Close();
        }

        private void Draw(int amount)
        {
            if (!allowDrawing) return;

            CheckoutEvents.RaiseCashRegisterDraw(amount);

            AudioID audioId = amount < 100 ? AudioID.Coin : AudioID.Draw;
            AudioManager.Instance.PlaySFX(audioId);
        }

        /// <summary>
        /// Opens the cash register UI, allowing money to be drawn.
        /// </summary>
        private void Open()
        {
            // Use DOTween to smoothly animate the cash register opening.
            rect.DOAnchorPosY(0f, 0.5f)
                .OnComplete(() => allowDrawing = true); // Enable drawing after the animation completes.
        }

        /// <summary>
        /// Closes the cash register UI, preventing further drawing.
        /// </summary>
        private void Close()
        {
            allowDrawing = false; // Disable drawing.
            rect.DOAnchorPosY(originalPosY, 0.5f); // Animate the cash register closing.
        }
    }
}
