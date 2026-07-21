using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class BillUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("Displays the type of the bill.")]
        private TextMeshProUGUI typeText;

        [SerializeField, Tooltip("Displays the issue day of the bill.")]
        private TextMeshProUGUI issueText;

        [SerializeField, Tooltip("Displays the due day of the bill.")]
        private TextMeshProUGUI dueText;

        [SerializeField, Tooltip("Displays the current status of the bill.")]
        private TextMeshProUGUI statusText;

        [SerializeField, Tooltip("Displays any penalties or warnings about the bill.")]
        private TextMeshProUGUI penaltyText;

        [SerializeField, Tooltip("Displays the amount due for the bill.")]
        private TextMeshProUGUI amountText;

        [SerializeField, Tooltip("Button that triggers the bill payment.")]
        private Button payButton;

        [Header("Status Colors")]
        [SerializeField] private Color paidColor;
        [SerializeField] private Color overdueColor;
        [SerializeField] private Color graceColor;
        [SerializeField] private Color dueSoonColor;
        [SerializeField] private Color dueNormalColor;

        [Header("Localization")]
        [SerializeField] private LocalizedString localizedIssue;
        [SerializeField] private LocalizedString localizedDue;
        [SerializeField] private LocalizedString localizedStatus;
        [SerializeField] private LocalizedString localizedPenalty;

        public Bill Bill { get; private set; }

        private int currentDay => DataManager.Instance.Data.TotalDays;
        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;

        private void OnEnable()
        {
            localizedIssue.StringChanged += UpdateIssueText;
            localizedDue.StringChanged += UpdateDueText;
            localizedStatus.StringChanged += UpdateStatusText;
            localizedPenalty.StringChanged += UpdatePenaltyText;
        }

        private void OnDisable()
        {
            localizedIssue.StringChanged -= UpdateIssueText;
            localizedDue.StringChanged -= UpdateDueText;
            localizedStatus.StringChanged -= UpdateStatusText;
            localizedPenalty.StringChanged -= UpdatePenaltyText;
        }

        private void UpdateIssueText(string value) => issueText.text = value;
        private void UpdateDueText(string value) => dueText.text = value;
        private void UpdateStatusText(string value) => statusText.text = value;
        private void UpdatePenaltyText(string value) => penaltyText.text = value;

        public void Initialize(Bill bill)
        {
            this.Bill = bill;

            typeText.text = bill.Type.ToString();
            amountText.text = $"{currencySymbol}{bill.Amount:F0}";

            // Issue Date Localization
            localizedIssue.Arguments = new object[] { bill.IssueDay };
            localizedIssue.RefreshString();

            // Due Date Localization
            localizedDue.Arguments = new object[] { bill.DueDay };
            localizedDue.RefreshString();

            payButton.onClick.AddListener(() =>
            {
                if (FinanceManager.Instance.PayBill(Bill))
                {
                    AudioManager.Instance.PlaySFX(AudioID.Kaching);
                    UpdateUI();
                }
            });

            UpdateUI();
        }

        public void UpdateUI()
        {
            UpdateStatusLocalization();
            UpdatePenaltyLocalization();

            statusText.color = GetStatusColor();
            payButton.interactable = Bill.Status == BillStatus.Unpaid;
        }

        private void UpdateStatusLocalization()
        {
            localizedStatus.Arguments = null;

            if (Bill.IsPaid)
            {
                localizedStatus.TableEntryReference = "Bill_Status_Paid";
            }
            else if (currentDay < Bill.DueDay)
            {
                int daysLeft = Bill.DueDay - currentDay;
                if (daysLeft == 1)
                {
                    localizedStatus.TableEntryReference = "Bill_Status_DueTomorrow";
                }
                else
                {
                    localizedStatus.TableEntryReference = "Bill_Status_DaysLeft";
                    localizedStatus.Arguments = new object[] { daysLeft };
                }
            }
            else if (currentDay == Bill.DueDay)
            {
                localizedStatus.TableEntryReference = "Bill_Status_DueToday";
            }
            else if (Bill.IsInGracePeriod(currentDay))
            {
                int graceLeft = (Bill.DueDay + Bill.GracePeriodDays) - currentDay;
                if (graceLeft == 1)
                {
                    localizedStatus.TableEntryReference = "Bill_Status_GraceTomorrow";
                }
                else if (graceLeft == 0)
                {
                    localizedStatus.TableEntryReference = "Bill_Status_GraceToday";
                }
                else
                {
                    localizedStatus.TableEntryReference = "Bill_Status_GraceDaysLeft";
                    localizedStatus.Arguments = new object[] { graceLeft };
                }
            }
            else
            {
                localizedStatus.TableEntryReference = "Bill_Status_Overdue";
            }

            localizedStatus.RefreshString();
        }

        private void UpdatePenaltyLocalization()
        {
            if (Bill.IsOverdue(currentDay))
            {
                penaltyText.gameObject.SetActive(true);
                decimal totalDue = Bill.GetTotalAmountDue(currentDay);

                localizedPenalty.TableEntryReference = "Bill_Penalty_Overdue";
                localizedPenalty.Arguments = new object[] { currencySymbol, Bill.LatePenalty, totalDue };
                localizedPenalty.RefreshString();

                penaltyText.color = GetStatusColor();
            }
            else if (Bill.IsInGracePeriod(currentDay))
            {
                penaltyText.gameObject.SetActive(true);

                localizedPenalty.TableEntryReference = "Bill_Penalty_Grace";
                localizedPenalty.Arguments = new object[] { currencySymbol, Bill.LatePenalty };
                localizedPenalty.RefreshString();

                penaltyText.color = GetStatusColor();
            }
            else
            {
                penaltyText.gameObject.SetActive(false);
            }
        }

        private Color GetStatusColor()
        {
            if (Bill.IsPaid) return paidColor;
            if (Bill.IsOverdue(currentDay)) return overdueColor;
            if (Bill.IsInGracePeriod(currentDay)) return graceColor;

            if (currentDay <= Bill.DueDay)
            {
                return (Bill.DueDay - currentDay <= 1) ? dueSoonColor : dueNormalColor;
            }

            return Color.magenta;
        }
    }
}
