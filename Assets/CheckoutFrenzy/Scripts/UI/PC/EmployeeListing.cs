using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class EmployeeListing : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image avatar;
        [SerializeField] private TMP_Text typeLabel;
        [SerializeField] private TMP_Text costLabel;
        [SerializeField] private TMP_Text salaryLabel;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Button hireButton;
        [SerializeField] private TMP_Text hireLabel;

        [Header("Colors")]
        [SerializeField] private Color hireColor = Color.green;
        [SerializeField] private Color fireColor = Color.red;

        [Header("Localization")]
        [SerializeField] private LocalizedString localizedCost;
        [SerializeField] private LocalizedString localizedSalary;
        [SerializeField] private LocalizedString localizedHireStatus;

        private string currencySymbol => GameConfig.Instance.ActiveCurrency.currencySymbol;
        private EmployeeData employeeData;

        public void Initialize(EmployeeData employeeData)
        {
            this.employeeData = employeeData;

            var employee = EmployeeManager.Instance.GetEmployeePrefab(employeeData);

            avatar.sprite = employee.Avatar;
            typeLabel.text = employee.Type.ToString();
            description.text = employee.Description;

            localizedCost.Arguments = new object[] { currencySymbol, employee.Cost };
            localizedSalary.Arguments = new object[] { currencySymbol, employee.SalaryBill.Amount, employee.SalaryBill.FrequencyInDays };
            localizedHireStatus.Arguments = new object[] { DataManager.Instance.Data.HiredEmployees.Contains(employeeData) };

            localizedCost.StringChanged += UpdateCostText;
            localizedSalary.StringChanged += UpdateSalaryText;
            localizedHireStatus.StringChanged += UpdateHireText;

            EmployeeManager.Instance.OnUnpaidEmployeeFired += UpdateHireButton;

            UpdateHireButton();
        }

        private void OnDestroy()
        {
            localizedCost.StringChanged -= UpdateCostText;
            localizedSalary.StringChanged -= UpdateSalaryText;
            localizedHireStatus.StringChanged -= UpdateHireText;

            if (EmployeeManager.Instance != null)
            {
                EmployeeManager.Instance.OnUnpaidEmployeeFired -= UpdateHireButton;
            }
        }

        private void UpdateCostText(string value) => costLabel.text = value;
        private void UpdateSalaryText(string value) => salaryLabel.text = value;
        private void UpdateHireText(string value) => hireLabel.text = value;

        private void UpdateHireButton()
        {
            bool hired = DataManager.Instance.Data.HiredEmployees.Contains(employeeData);

            hireButton.image.color = hired ? fireColor : hireColor;

            localizedHireStatus.Arguments = new object[] { hired };
            localizedHireStatus.RefreshString();

            hireButton.onClick.RemoveAllListeners();
            hireButton.onClick.AddListener(() =>
            {
                if (hired) EmployeeManager.Instance.FireEmployee(employeeData);
                else EmployeeManager.Instance.HireEmployee(employeeData);

                UpdateHireButton();
            });
        }
    }
}
