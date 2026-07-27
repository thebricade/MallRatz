using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class FinanceManager : MonoBehaviour
    {
        // No longer a singleton: one FinanceManager instance lives on each ShopPlot,
        // managing that shop's own bills and loans via its sibling StoreManager/DataManager.
        private StoreManager storeManager;
        private DataManager dataManager;

        [Header("Messages")]
        [SerializeField, Tooltip("Shown when the player doesn't have enough money to pay bills.")]
        private LocalizedString insufficientFundsMessage;

        [SerializeField, Tooltip("Shown when a loan is already active and cannot be taken again.")]
        private LocalizedString activeLoanMessage;

        [Header("Financial")]
        [SerializeField, Tooltip("A list of bills that are automatically issued on a recurring basis according to their frequency.")]
        private List<BillTemplate> recurringBills;

        public event System.Action<Bill> OnBillCreated;
        public event System.Action OnBillPaid;
        public event System.Action OnBillsUpdated;
        public event System.Action OnLoanTaken;
        public event System.Action OnLoansUpdated;

        private List<Bill> bills => dataManager.Data.Bills;
        private List<Loan> loans => dataManager.Data.Loans;

        private int currentDay => dataManager.Data.TotalDays;

        private void Awake()
        {
            storeManager = GetComponent<StoreManager>();
            dataManager = GetComponent<DataManager>();
        }

        private void Start()
        {
            storeManager.OnDayStarted += HandleDayStarted;
        }

        private void OnDestroy()
        {
            if (storeManager != null)
                storeManager.OnDayStarted -= HandleDayStarted;
        }

        private void HandleDayStarted(int currentDay)
        {
            UpdateBills();
            CreateRecurringBills(currentDay);
            UpdateLoans(currentDay);
        }

        private void UpdateBills()
        {
            for (int i = bills.Count - 1; i >= 0; i--)
            {
                var bill = bills[i];

                if (bill.IsPaid || bill.Status == BillStatus.Charged)
                {
                    bills.RemoveAt(i);
                    continue;
                }
                else if (bill.IsOverdue(currentDay))
                {
                    dataManager.PlayerMoney -= bill.GetTotalAmountDue(currentDay);
                    bill.Status = BillStatus.Charged;
                }
            }

            OnBillsUpdated?.Invoke();
        }

        private void CreateRecurringBills(int currentDay)
        {
            foreach (var template in recurringBills)
            {
                if (currentDay % template.FrequencyInDays == 0)
                {
                    CreateBillFromTemplate(template, currentDay);
                }
            }
        }

        private void UpdateLoans(int currentDay)
        {
            bool loansChanged = false;
            foreach (var loan in loans)
            {
                if (loan.IsCompleted) continue;

                if (loan.NextPaymentDay == currentDay)
                {
                    loan.PaymentsMade++;
                    CreateRepaymentBill(loan, currentDay);
                    loansChanged = true;
                }
            }

            if (loans.Any(l => l.IsCompleted))
            {
                loans.RemoveAll(l => l.IsCompleted);
                loansChanged = true;
            }

            if (loansChanged) OnLoansUpdated?.Invoke();
        }

        public void CreateBillFromTemplate(BillTemplate template, int currentDay)
        {
            var bill = new Bill
            {
                Type = template.Type,
                IssueDay = currentDay,
                DueDay = currentDay + template.DueOffset,
                GracePeriodDays = template.GracePeriodDays,
                Amount = template.Amount,
                LatePenalty = template.LatePenalty
            };

            if (bill.Type == BillType.Rent)
            {
                bill.Amount += dataManager.Data.ExpansionLevel * 10m;
            }
            else if (bill.Type == BillType.Electricity)
            {
                bill.Amount += dataManager.Data.ExpansionLevel * 5m;
            }

            bills.Add(bill);
            OnBillCreated?.Invoke(bill);
        }

        public void AddLoanFromTemplate(LoanTemplate template)
        {
            if (loans.Any(l => l.DisplayName == template.DisplayName))
            {
                UIEvents.RaiseMessage(activeLoanMessage.GetLocalizedString(), Color.red);
                return;
            }

            var loan = new Loan
            {
                DisplayName = template.DisplayName,
                Principal = template.Principal,
                InterestRate = template.InterestRate,
                TotalPayments = template.TotalPayments,
                StartDay = dataManager.Data.TotalDays + 1,
                PaymentInterval = template.PaymentInterval,
                LatePayment = template.LateFeePerInstallment
            };

            loans.Add(loan);
            dataManager.PlayerMoney += loan.Principal;
            AudioManager.Instance.PlaySFX(AudioID.Kaching);

            OnLoanTaken?.Invoke();
        }

        private void CreateRepaymentBill(Loan loan, int currentDay)
        {
            var bill = new Bill
            {
                Type = BillType.Repayment,
                IssueDay = currentDay,
                DueDay = currentDay + 1,
                GracePeriodDays = 2,
                Amount = loan.PaymentAmount,
                LatePenalty = loan.LatePayment
            };

            bills.Add(bill);
            OnBillCreated?.Invoke(bill);
        }

        public bool PayBill(Bill bill)
        {
            if (dataManager.PlayerMoney < bill.Amount)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            dataManager.PlayerMoney -= bill.Amount;
            bill.Status = BillStatus.Paid;

            OnBillPaid?.Invoke();

            return true;
        }

        public bool PayAllBills()
        {
            decimal outstandingAmount = GetTotalOutstandingAmount();

            if (dataManager.PlayerMoney < outstandingAmount)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            dataManager.PlayerMoney -= outstandingAmount;
            GetActiveBills().ForEach(b => b.Status = BillStatus.Paid);

            OnBillsUpdated?.Invoke();

            return true;
        }

        public List<Bill> GetActiveBills()
        {
            return bills.FindAll(b => b.Status == BillStatus.Unpaid);
        }

        public decimal GetTotalOutstandingAmount()
        {
            decimal total = 0m;
            foreach (var bill in GetActiveBills())
                total += bill.GetTotalAmountDue(currentDay);
            return total;
        }
    }
}
