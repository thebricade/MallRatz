using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class LoanListing : MonoBehaviour
    {
        [SerializeField, Tooltip("Text element displaying the loan's name.")]
        private TextMeshProUGUI nameText;

        [SerializeField, Tooltip("Text element displaying the principal amount of the loan.")]
        private TextMeshProUGUI principalText;

        [SerializeField, Tooltip("Text element displaying interest, payment schedule, total repayment, and penalties.")]
        private TextMeshProUGUI infoText;

        [SerializeField, Tooltip("Button used to take this loan.")]
        private Button takeButton;

        [Header("Localization Strings")]
        [SerializeField] private LocalizedString infoTextLocalizedString;
        [SerializeField] private LocalizedString unlockLevelLocalizedString;

        [Header("Locker")]
        [SerializeField, Tooltip("UI element shown when the loan is locked (not yet available).")]
        private GameObject locker;

        [SerializeField, Tooltip("Text element for the unlock requirement.")]
        private TextMeshProUGUI lockerLabel;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;
        private LoanTemplate loanTemplate;

        public void Initialize(LoanTemplate template)
        {
            loanTemplate = template;

            nameText.text = template.DisplayName;
            principalText.text = $"{currencySymbol}{template.Principal:F0}";

            decimal totalToRepay = template.Principal + (template.Principal * template.InterestRate);
            decimal paymentAmount = totalToRepay / template.TotalPayments;
            decimal penaltyAmount = template.LateFeePerInstallment;

            // Info Text Arguments
            infoTextLocalizedString.Arguments = new object[]
            {
                template.InterestRate * 100m, // {0}
                template.TotalPayments,       // {1}
                currencySymbol,               // {2}
                paymentAmount,                // {3}
                template.PaymentInterval,     // {4}
                totalToRepay,                 // {5}
                penaltyAmount                 // {6}
            };

            // Locker Label Arguments
            unlockLevelLocalizedString.Arguments = new object[] { template.LevelRequirement };

            infoTextLocalizedString.StringChanged += OnInfoTextChanged;
            unlockLevelLocalizedString.StringChanged += OnUnlockLabelChanged;

            takeButton.onClick.RemoveAllListeners();
            takeButton.onClick.AddListener(() => FinanceManager.Instance.AddLoanFromTemplate(template));

            DataManager.Instance.OnLevelUp += TryUnlock;
            TryUnlock(DataManager.Instance.Data.CurrentLevel);
        }

        private void OnDestroy()
        {
            infoTextLocalizedString.StringChanged -= OnInfoTextChanged;
            unlockLevelLocalizedString.StringChanged -= OnUnlockLabelChanged;

            takeButton.onClick.RemoveAllListeners();

            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnLevelUp -= TryUnlock;
            }
        }

        private void OnInfoTextChanged(string localizedText)
        {
            if (infoText != null)
            {
                infoText.text = localizedText;
            }
        }

        private void OnUnlockLabelChanged(string localizedText)
        {
            if (lockerLabel != null)
            {
                lockerLabel.text = localizedText;
            }
        }

        private void TryUnlock(int currentLevel)
        {
            bool levelMet = currentLevel >= loanTemplate.LevelRequirement;

            if (locker != null)
            {
                locker.SetActive(!levelMet);
            }

            if (levelMet)
            {
                DataManager.Instance.OnLevelUp -= TryUnlock;
            }
        }
    }
}
