using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class BillNotification : MonoBehaviour
    {
        [SerializeField, Tooltip("The image component used to display the bill warning icon.")]
        private Image warningImage;

        [SerializeField, Tooltip("Color of the warning icon when a bill is due soon.")]
        private Color dueSoonColor = Color.yellow;

        [SerializeField, Tooltip("Color of the warning icon when a bill is in the grace period.")]
        private Color gracePeriodColor = Color.red;

        [Header("Tween Settings")]
        [SerializeField, Tooltip("Delay before starting the shake animation loop.")]
        private float delay = 1f;

        [SerializeField, Tooltip("Duration of each shake animation cycle.")]
        private float duration = 0.75f;

        [SerializeField, Tooltip("Punch intensity applied to rotation on each axis.")]
        private Vector3 punch = new Vector3(0, 0, 15);

        private Tween shakeTween;

        private void Start()
        {
            FinanceManager.Instance.OnBillsUpdated += UpdateNotification;
            FinanceManager.Instance.OnBillPaid += UpdateNotification;
            FinanceManager.Instance.OnBillCreated += UpdateNotification;
            UpdateNotification();
        }

        private void OnDestroy()
        {
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.OnBillsUpdated -= UpdateNotification;
                FinanceManager.Instance.OnBillPaid -= UpdateNotification;
                FinanceManager.Instance.OnBillCreated -= UpdateNotification;
            }

            shakeTween?.Kill();
        }

        private void UpdateNotification()
        {
            var bills = DataManager.Instance.Data.Bills;
            int today = DataManager.Instance.Data.TotalDays;

            bool hasDueSoon = false;
            bool hasInGrace = false;

            foreach (var bill in bills)
            {
                if (bill.IsPaid || bill.Status == BillStatus.Charged)
                    continue;

                if (bill.DueDay == today || bill.DueDay == today + 1)
                    hasDueSoon = true;

                if (today > bill.DueDay && today <= bill.DueDay + bill.GracePeriodDays)
                    hasInGrace = true;
            }

            if (hasInGrace || hasDueSoon)
            {
                warningImage.color = hasInGrace ? gracePeriodColor : dueSoonColor;

                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                    transform.localRotation = Quaternion.identity;
                }

                shakeTween?.Kill();
                transform.localRotation = Quaternion.identity;

                var sequence = DOTween.Sequence();
                sequence.AppendInterval(delay);
                sequence.Append(transform.DOPunchRotation(punch, duration));
                sequence.AppendInterval(delay);
                sequence.SetLoops(-1, LoopType.Restart);
                sequence.SetEase(Ease.Linear);

                shakeTween = sequence;
            }
            else
            {
                shakeTween?.Kill();
                gameObject.SetActive(false);
            }
        }

        private void UpdateNotification(Bill _)
        {
            UpdateNotification();
        }
    }
}
