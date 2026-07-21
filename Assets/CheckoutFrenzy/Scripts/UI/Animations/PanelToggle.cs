using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class PanelToggle : MonoBehaviour, IPointerClickHandler
    {
        [Header("Panel Settings")]
        [Tooltip("The RectTransform of the panel to be toggled.")]
        [SerializeField] private RectTransform panel;

        [Tooltip("The slide offset for showing or hiding the panel.")]
        [SerializeField] private Vector2 slideOffset;

        [Tooltip("Should the panel be visible by default?")]
        [SerializeField] private bool startVisible = true;

        [Header("Icon Settings")]
        [Tooltip("Image that displays the toggle icon.")]
        [SerializeField] private Image toggleImage;

        [Tooltip("Icon to show when the panel is visible.")]
        [SerializeField] private Sprite visibleIcon;

        [Tooltip("Icon to show when the panel is hidden.")]
        [SerializeField] private Sprite hiddenIcon;

        [Header("Animation Settings")]
        [Tooltip("Duration of the toggle animation in seconds.")]
        [SerializeField] private float animationDuration = 0.5f;

        private bool isAnimating;
        private bool isPanelVisible;
        private Tween currentTween;

        private void Awake()
        {
            // Initialize the panel visibility based on the default setting
            isPanelVisible = startVisible;
            panel.anchoredPosition += isPanelVisible ? Vector2.zero : -slideOffset;
            UpdateIcon();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TogglePanel();
        }

        private void TogglePanel()
        {
            if (isAnimating)
            {
                // Cancel the ongoing animation if any
                currentTween?.Kill();
            }

            isAnimating = true;
            Vector2 targetPosition = panel.anchoredPosition + (isPanelVisible ? -slideOffset : slideOffset);

            // Animate the panel's position
            currentTween = panel.DOAnchorPos(targetPosition, animationDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(OnToggleComplete);

            // Update the panel state
            isPanelVisible = !isPanelVisible;
            UpdateIcon();

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void OnToggleComplete()
        {
            isAnimating = false;
        }

        private void UpdateIcon()
        {
            toggleImage.sprite = isPanelVisible ? visibleIcon : hiddenIcon;
        }
    }
}
