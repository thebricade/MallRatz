using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [Tooltip("If true, opening this panel sets the player state to Busy.")]
        [SerializeField] protected bool pausesPlayer = true;

        protected CanvasGroup canvasGroup;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Makes the UI visible and interactable.
        /// </summary>
        public virtual void ShowUI()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            if (pausesPlayer)
            {
                UIEvents.RaiseUIPanelOpened();
            }
        }

        /// <summary>
        /// Hides the UI and disables interaction.
        /// </summary>
        public virtual void HideUI()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (pausesPlayer)
            {
                UIEvents.RaiseUIPanelClosed();
            }
        }

        /// <summary>
        /// Check if this specific UI is currently on screen.
        /// </summary>
        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0;
    }
}
