using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Cinemachine;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class CheckoutCounter : Interactable
    {
        [Header("Messages")]
        [SerializeField, Tooltip("The localized text shown when a cashier is already assigned to this counter.")]
        private LocalizedString hasCashierMessage;

        [SerializeField, Tooltip("The localized text shown when the given change is less than the required amount.")]
        private LocalizedString insufficientChangeMessage;

        [SerializeField, Tooltip("The localized text shown when the entered card payment amount is incorrect.")]
        private LocalizedString invalidAmountMessage;

        [Header("Counter Settings")]
        [SerializeField, Tooltip("The position where the first customer in line stands to check out.")]
        private Vector3 checkoutPoint;

        [SerializeField, Tooltip("The direction of the customer queue.")]
        private Vector3 liningDirection = Vector3.left;

        [SerializeField, Tooltip("The position where items are moved after being scanned.")]
        private Vector3 packingPoint;

        [SerializeField, Tooltip("The position where change is given to the customer.")]
        private Vector3 moneyPoint;

        [SerializeField, Tooltip("The area within these bounds defines where products can be placed.")]
        private Bounds placementBounds;

        [SerializeField, Tooltip("The maximum number of attempts to find a valid placement position for the products.")]
        private int maxPlacementAttempts = 100;

        [SerializeField, Tooltip("The time required for the cashier (if one is hired) to scan a product (in seconds).")]
        private float autoScanTime = 1f;

        [SerializeField, Tooltip("Reference to the component that manages the physical monitor's UI.")]
        private CounterMonitor monitor;

        [SerializeField, Tooltip("The position where the cashier stands while working at this counter.")]
        private Transform cashierPoint;

        [SerializeField, Tooltip("The Cinemachine Virtual Camera used to focus on the counter during transactions.")]
        private CinemachineVirtualCamera cashierCamera;

        public CounterState CurrentState { get; private set; }

        public bool HasCashier { get; private set; }

        public List<Customer> LiningCustomers { get; private set; } = new List<Customer>();
        public int GetQueueNumber(Customer customer) => LiningCustomers.IndexOf(customer);

        private CurrencyData activeCurrency => GameConfig.Instance.ActiveCurrency;

        private IInteractor interactor;
        private Customer currentCustomer;
        private List<CheckoutItem> checkoutItems = new List<CheckoutItem>();

        // Total cost of the customer's items.
        private decimal totalPrice;
        // Amount of money the customer paid.
        private decimal customerMoney;
        // Change given to the customer.
        private Stack<ChangeMoney> givenChange = new Stack<ChangeMoney>();
        // True only when the amount paid meets or exceeds the total price.
        private bool isPaymentComplete;

        private void Start()
        {
            UpdateMonitorText();
            StoreManager.Instance.RegisterCounter(this);
        }

        private void OnDestroy()
        {
            UnsubscribeFromPaymentEvents();
        }

        public override void Interact(IInteractor interactor)
        {
            if (HasCashier)
            {
                if (interactionHint != null && !interactionHint.IsEmpty)
                {
                    UIEvents.RaiseMessage(hasCashierMessage.GetLocalizedString(), Color.red);
                }

                return;
            }

            this.interactor = interactor;

            cashierCamera.gameObject.SetActive(true);

            SubscribeToPaymentEvents();

            ActivateReturnButton();
            interactor.StateManager.PushState(PlayerState.Working);
            UIEvents.RaiseInteractMessage("");
        }

        private void SubscribeToPaymentEvents()
        {
            CheckoutEvents.OnCashRegisterDraw += UpdateGivenChange;
            CheckoutEvents.OnCashRegisterUndo += UndoGivenChange;
            CheckoutEvents.OnCashRegisterClear += ClearGivenChange;
            CheckoutEvents.OnCashRegisterConfirm += TryProcessCashPayment;
            CheckoutEvents.OnPaymentTerminalConfirm += TryProcessCardPayment;
        }

        private void UnsubscribeFromPaymentEvents()
        {
            CheckoutEvents.OnCashRegisterDraw -= UpdateGivenChange;
            CheckoutEvents.OnCashRegisterUndo -= UndoGivenChange;
            CheckoutEvents.OnCashRegisterClear -= ClearGivenChange;
            CheckoutEvents.OnCashRegisterConfirm -= TryProcessCashPayment;
            CheckoutEvents.OnPaymentTerminalConfirm -= TryProcessCardPayment;
        }

        public void AssignCashier(Cashier cashier)
        {
            HasCashier = cashier != null;

            if (HasCashier)
            {
                cashier.transform.SetParent(cashierPoint);
                cashier.transform.localPosition = Vector3.zero;
                cashier.transform.localRotation = Quaternion.identity;

                if (CurrentState == CounterState.Scanning)
                    StartCoroutine(AutoScan());
            }
            else
            {
                Destroy(cashierPoint.GetChild(0).gameObject);
            }
        }

        public Vector3 GetQueuePosition(Customer customer, out Vector3 lookDirection)
        {
            Vector3 worldCheckoutPoint = transform.TransformPoint(checkoutPoint);

            int queueNumber = GetQueueNumber(customer);

            if (queueNumber > 0) lookDirection = -liningDirection;
            else lookDirection = (cashierPoint.position - worldCheckoutPoint).normalized;

            return worldCheckoutPoint + liningDirection * queueNumber * 0.5f;
        }

        /// <summary>
        /// Places the customer's products onto the checkout counter.
        /// </summary>
        /// <param name="customer">The customer whose products are being placed.</param>
        /// <returns>An IEnumerator for coroutine execution, allowing for placement attempts and animation delays.</returns>
        public IEnumerator PlaceProducts(Customer customer)
        {
            SetCurrentState(CounterState.Placing);

            currentCustomer = customer;

            var products = customer.Inventory;

            foreach (var product in products)
            {
                int attempts = 0;
                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                bool placementSuccessful = false;

                while (attempts < maxPlacementAttempts)
                {
                    // Generate a random position within table's placement bounds
                    position.x = Random.Range(placementBounds.min.x, placementBounds.max.x);
                    position.y = placementBounds.min.y;
                    position.z = Random.Range(placementBounds.min.z, placementBounds.max.z);
                    position = transform.TransformPoint(position);

                    // Generate a random rotation on Y-axis
                    rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    // Check for overlaps at the new position and rotation
                    Collider[] colliders = Physics.OverlapBox(position, product.Size / 2, rotation, GameConfig.Instance.CheckoutItemLayer);

                    if (colliders.Length == 0)
                    {
                        placementSuccessful = true;
                        break;
                    }

                    attempts++;
                    yield return null;
                }

                if (!placementSuccessful)
                {
                    Debug.LogWarning($"Could not place product '{product.Model.name}' after {maxPlacementAttempts} attempts. Placing at last attempted position.");
                }

                // Calculate the position in front of the customer where the product will initially appear.
                var customerFront = customer.transform.TransformPoint(new Vector3(0f, 1f, 0.5f));

                // Instantiate the product model at the calculated position, without any initial rotation.
                var productModel = Instantiate(product.Model, customerFront, Quaternion.identity);

                // Initially set the scale of the product model to zero, so it can be animated to its final size.
                productModel.transform.localScale = Vector3.zero;

                // Set the duration of the animation.
                float duration = 0.3f;

                // Animate the product model's jump to the target position, rotation, and scale.
                productModel.transform.DOJump(position, 0.5f, 1, duration);
                productModel.transform.DORotateQuaternion(rotation, duration);
                productModel.transform.DOScale(Vector3.one, duration);

                yield return new WaitForSeconds(duration);

                // Add the CheckoutItem component to the instantiated product model.
                var checkoutItem = productModel.AddComponent<CheckoutItem>();

                // Add the checkout item to the list of checkout items.
                checkoutItems.Add(checkoutItem);

                // Initialize the CheckoutItem component with the product data and the scanning handler.
                checkoutItem.Initialize(product, () => HandleScanning(checkoutItem));
            }

            SetCurrentState(CounterState.Scanning);

            if (HasCashier) StartCoroutine(AutoScan());
        }

        private void HandleScanning(CheckoutItem item)
        {
            UIEvents.RaiseActionUI(ActionType.Return, false, null);

            ScanItem(item);

            DataManager.Instance.AddExperience();

            if (checkoutItems.Count == 0)
            {
                StartCoroutine(ProcessPayment());
            }
        }

        private void ScanItem(CheckoutItem item)
        {
            checkoutItems.Remove(item);

            // Move the scanned item to the packing point and then destroy it.
            item.transform.DOMove(transform.TransformPoint(packingPoint), 0.3f)
                .OnComplete(() => Destroy(item.gameObject));

            decimal price = DataManager.Instance.GetCustomProductPrice(item.Product);
            totalPrice += price;
            UpdateMonitorText();

            if (!HasCashier) AudioManager.Instance.PlaySFX(AudioID.Scanner);

            MissionManager.Instance.UpdateMission(MissionGoal.Revenue, (int)(price * 100));
            MissionManager.Instance.UpdateMission(MissionGoal.Sell, 1, item.Product.ProductID);
        }

        private IEnumerator AutoScan()
        {
            yield return new WaitForSeconds(autoScanTime);

            while (checkoutItems.Count > 0)
            {
                var item = checkoutItems.FirstOrDefault();
                if (item != null) ScanItem(item);

                yield return new WaitForSeconds(autoScanTime);
            }

            StartCoroutine(ProcessPayment());
        }

        private IEnumerator ProcessPayment()
        {
            isPaymentComplete = false;

            // Determine payment method
            bool isUsingCash = currentCustomer.PrefersCash;
            CurrentState = isUsingCash ? CounterState.CashPay : CounterState.CardPay;

            // Wait for the customer to hand over payment, and capture the amount they give
            yield return currentCustomer.HandsPayment(
                totalPrice,
                HasCashier ? cashierPoint.GetComponentInChildren<Cashier>() : null,
                (amountHandedOver) => customerMoney = amountHandedOver
            );

            if (!HasCashier)
            {
                // Setup manual payment system (player as cashier)
                if (isUsingCash)
                {
                    CheckoutEvents.RaiseCashRegisterToggleRequested(true);
                }
                else
                {
                    CheckoutEvents.RaisePaymentTerminalToggleRequested(true);
                }
            }
            else
            {
                // If there's an AI cashier, we assume the transaction balances perfectly
                customerMoney = totalPrice;
            }

            UpdateMonitorText();

            if (HasCashier)
            {
                // Auto-complete transaction if there is a cashier
                yield return new WaitForSeconds(1f);
                DataManager.Instance.PlayerMoney += totalPrice;
                isPaymentComplete = true;
            }
            else
            {
                yield return new WaitUntil(() => isPaymentComplete);
                AudioManager.Instance.PlaySFX(AudioID.Kaching);
            }

            // Finalize transaction
            currentCustomer.Inventory.Clear();
            currentCustomer = null;
            totalPrice = 0m;
            customerMoney = 0m;

            // Clean up UI and close manual payment interfaces
            if (!HasCashier)
            {
                ActivateReturnButton();

                var endPosition = transform.TransformPoint(checkoutPoint + Vector3.up * 1.2f);
                yield return ClearGivenChangeCoroutine(endPosition);
            }

            SetCurrentState(CounterState.Standby);
        }

        private void UpdateGivenChange(int amount)
        {
            decimal playerBalance = DataManager.Instance.PlayerMoney + customerMoney;
            decimal totalChange = givenChange.Sum(change => change.amount) / 100m;

            if (playerBalance < totalChange + (amount / 100m)) return;

            givenChange.Push(new ChangeMoney(amount, SpawnMoney(amount)));

            UpdateMonitorText();
        }

        private void UndoGivenChange()
        {
            if (givenChange.Count == 0) return;

            var change = givenChange.Pop();

            var endPosition = cashierPoint.position + Vector3.up;
            change.money.transform.DOMove(endPosition, 0.3f)
                .OnComplete(() => Destroy(change.money));

            UpdateMonitorText();
        }

        private void ClearGivenChange()
        {
            var endPosition = cashierPoint.position + Vector3.up;
            StartCoroutine(ClearGivenChangeCoroutine(endPosition));
        }

        private void TryProcessCashPayment()
        {
            decimal totalChange = givenChange.Sum(change => change.amount) / 100m;
            isPaymentComplete = totalChange >= customerMoney - totalPrice;

            if (isPaymentComplete)
            {
                decimal paymentAmount = customerMoney - totalChange;
                DataManager.Instance.PlayerMoney += paymentAmount;
                MissionManager.Instance.UpdateMission(MissionGoal.Checkout, 1);
                CheckoutEvents.RaiseCashRegisterToggleRequested(false);
            }
            else
            {
                UIEvents.RaiseMessage(insufficientChangeMessage.GetLocalizedString(), Color.red);
            }
        }

        private void TryProcessCardPayment(decimal amount)
        {
            isPaymentComplete = totalPrice == amount;

            if (isPaymentComplete)
            {
                DataManager.Instance.PlayerMoney += amount;
                MissionManager.Instance.UpdateMission(MissionGoal.Checkout, 1);
                CheckoutEvents.RaisePaymentTerminalToggleRequested(false);
            }
            else
            {
                UIEvents.RaiseMessage(invalidAmountMessage.GetLocalizedString(), Color.red);
            }
        }

        private GameObject SpawnMoney(int amount)
        {
            var denom = activeCurrency.denominations.Find(d => d.value == amount);
            if (denom == null) return null;

            var money = new GameObject("Money_" + amount);
            money.transform.position = cashierPoint.position + Vector3.up;
            money.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            money.transform.localScale = Vector3.one * 0.05f;

            var moneyRend = money.AddComponent<SpriteRenderer>();
            moneyRend.sprite = denom.sprite;
            moneyRend.sortingOrder = givenChange.Count;

            var center = transform.TransformPoint(moneyPoint);
            var position = center + Random.insideUnitSphere.Flatten() * 0.15f;
            money.transform.DOMove(position, 0.3f);

            return money;
        }

        private IEnumerator ClearGivenChangeCoroutine(Vector3 endPosition, float duration = 0.3f)
        {
            var changeToClear = new List<ChangeMoney>(givenChange);
            givenChange.Clear();
            UpdateMonitorText();

            changeToClear.ForEach(change =>
            {
                change.money.transform.DOMove(endPosition, duration)
                    .OnComplete(() => Destroy(change.money));
            });

            yield return new WaitForSeconds(duration);
        }

        private void SetCurrentState(CounterState state)
        {
            CurrentState = state;
            UpdateMonitorText();
        }

        private void UpdateMonitorText()
        {
            decimal totalChange = givenChange.Sum(change => change.amount) / 100m;
            monitor.UpdateDisplay(CurrentState, totalPrice, customerMoney, totalChange);
        }

        private void ActivateReturnButton()
        {
            UIEvents.RaiseActionUI(ActionType.Return, true, () =>
            {
                cashierCamera.gameObject.SetActive(false);

                UIEvents.RaiseActionUI(ActionType.Return, false, null);

                UnsubscribeFromPaymentEvents();

                interactor.StateManager.PopState();
                interactor = null;
            });
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Vector3 worldCheckoutPoint = transform.TransformPoint(checkoutPoint);
            Gizmos.DrawWireSphere(worldCheckoutPoint, 0.2f);
            DrawArrow.ForGizmo(worldCheckoutPoint, liningDirection * 3f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.TransformPoint(packingPoint), 0.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(moneyPoint), 0.15f);

            Vector3 worldCenter = transform.TransformPoint(placementBounds.center);
            Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, placementBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
