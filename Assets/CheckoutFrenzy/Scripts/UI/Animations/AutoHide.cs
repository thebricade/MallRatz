using System.Collections;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AutoHide : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float waitDuration = 5f;
        [SerializeField] private float fadeDuration = 1f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            // Reset alpha when the object is re-enabled
            canvasGroup.alpha = 1f;
            StartCoroutine(WaitAndFade());
        }

        private IEnumerator WaitAndFade()
        {
            yield return new WaitForSeconds(waitDuration);

            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
