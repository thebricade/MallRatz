using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(Image))]
    public class ScreenFader : MonoBehaviour
    {
        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.color = Color.black;
        }

        private void Start()
        {
            FadeOut();
        }

        /// <summary>
        /// Fades the screen in.
        /// </summary>
        /// <param name="duration">The duration of the fade-in animation (in seconds). Defaults to 1 second.</param>
        /// <param name="onComplete">An optional action to be performed when the fade-in is complete.</param>
        public void FadeIn(float duration = 1f, System.Action onComplete = null)
        {
            image.raycastTarget = true; // Enable raycast target during fade-in.
            image.DOFade(1f, duration) // Fade the image to opaque.
                .SetUpdate(UpdateType.Normal, true)
                .OnComplete(() => onComplete?.Invoke()); // Invoke the onComplete action when the fade-in is finished.
        }

        /// <summary>
        /// Fades the screen out.
        /// </summary>
        /// <param name="duration">The duration of the fade-out animation (in seconds). Defaults to 1 second.</param>
        /// <param name="onComplete">An optional action to be performed when the fade-out is complete.</param>
        public void FadeOut(float duration = 1f, System.Action onComplete = null)
        {
            image.DOFade(0f, duration) // Fade the image to transparent.
                .SetUpdate(UpdateType.Normal, true)
                .OnComplete(() =>
                {
                    onComplete?.Invoke(); // Invoke the onComplete action.
                    image.raycastTarget = false; // Disable raycast target after fade-out.
                });

        }
    }
}
