using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class OverheadUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image thiefIcon;

        private Camera m_cam;
        private Camera cam
        {
            get
            {
                if (m_cam == null) m_cam = Camera.main;
                return m_cam;
            }
        }

        private void Awake()
        {
            dialogText.text = "";
            thiefIcon.enabled = false;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            transform.forward = cam.transform.forward;
        }

        public void ShowDialog(string dialog, float time = 5f)
        {
            dialogText.text = dialog;
            StartCoroutine(ClearDialog(time));
        }

        private IEnumerator ClearDialog(float delay)
        {
            yield return new WaitForSeconds(delay);
            dialogText.text = "";
        }

        public void ShowThiefIcon() => thiefIcon.enabled = true;
        public void HideThiefIcon() => thiefIcon.enabled = false;
    }
}
