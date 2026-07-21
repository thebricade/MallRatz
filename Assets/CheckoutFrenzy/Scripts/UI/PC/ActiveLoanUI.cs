using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class ActiveLoanUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text loanNameText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private LocalizedString progressFormat;

        public void Initialize(Loan loan)
        {
            loanNameText.text = $"{loan.DisplayName}";

            progressFormat.Arguments = new object[] { loan.PaymentsMade, loan.TotalPayments };
            progressFormat.RefreshString();

            progressFormat.StringChanged += (val) => progressText.text = val;
        }
    }
}
