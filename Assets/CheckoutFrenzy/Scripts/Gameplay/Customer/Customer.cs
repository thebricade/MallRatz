using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        public event System.Action OnLeave;

        [SerializeField] private CustomerTrait trait;
        [SerializeField] private HandAttachments handAttachments;
        [SerializeField] private OverheadUI overheadUI;

        public List<Product> Inventory => inventory;
        public bool PrefersCash { get; private set; }
        public bool IsCaught { get; private set; }

        private CurrencyData activeCurrency => GameConfig.Instance.ActiveCurrency;

        private Animator animator;
        private NavMeshAgent agent;
        private Thief thief;

        private List<Product> inventory = new List<Product>();

        private ShelvingUnit shelvingUnit;
        private CheckoutCounter checkoutCounter;
        private int queueNumber = int.MaxValue;
        private bool isPicking;

        #region Unity Lifecycle
        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (trait.animatorOverride != null)
            {
                animator.runtimeAnimatorController = trait.animatorOverride;
            }

            agent = GetComponent<NavMeshAgent>();

            // Initialize NavMeshAgent parameters
            agent.speed = trait.walkSpeed;
            agent.angularSpeed = 3600f;
            agent.acceleration = 100f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.SetAreaCost(3, 50f);

            PrefersCash = Random.value < trait.cashPreference;
        }

        private void Start()
        {
            StartCoroutine(CheckEnteringStore());
            StartCoroutine(Shopping());
        }

        private void Update()
        {
            CheckStoreDoors();
        }
        #endregion

        #region Core Store Routine
        private void CheckStoreDoors()
        {
            Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1f, GameConfig.Instance.InteractableLayer))
            {
                if (hit.transform.TryGetComponent<EntranceDoor>(out EntranceDoor door))
                {
                    door.OpenIfClosed();
                }
            }
        }

        private IEnumerator CheckEnteringStore()
        {
            while (!StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            AudioManager.Instance.PlaySFX(AudioID.Bell);
        }

        private IEnumerator Shopping()
        {
            bool continueShopping = true;

            while (continueShopping)
            {
                yield return FindShelvingUnit();

                if (Random.value < trait.stealChance)
                {
                    yield return StealProduct();

                    if (inventory.Count > 0)
                    {
                        StartCoroutine(Leave());
                        StartCoroutine(CheckLeavingStoreAsThief());
                        yield break;
                    }
                }

                yield return PickProduct();

                continueShopping = Random.value < trait.continueShoppingChance;
            }

            if (shelvingUnit != null && shelvingUnit.IsOpen)
            {
                // Close the shelving unit if it's open (e.g., Fridges, Freezers)
                shelvingUnit.Close(true, false);
            }

            if (inventory.Count > 0)
            {
                yield return Checkout();
                yield return Leave();
            }
            else
            {
                // Customer leaves without buying anything
                var localizedNotFound = trait.notFoundDialogue.GetRandomLine();

                if (localizedNotFound != null)
                {
                    overheadUI.ShowDialog(localizedNotFound.GetLocalizedString());
                }

                yield return Leave();
            }
        }

        public void AskToLeave()
        {
            // If the customer has items in their inventory, they shouldn't leave yet.
            if (inventory.Count > 0) return;

            StopAllCoroutines();

            // If the customer was interacting with a shelving unit, re-register it with the store manager.
            if (shelvingUnit != null) StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);

            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            if (checkoutCounter != null)
            {
                checkoutCounter.LiningCustomers.Remove(this);
            }

            if (thief != null)
            {
                StoreManager.Instance.UnregisterThief(this);
            }

            OnLeave?.Invoke();

            var exitPoint = StoreManager.Instance.GetExitPoint();
            yield return MoveTo(exitPoint);

            yield return new WaitForEndOfFrame();
            Destroy(gameObject);
        }
        #endregion

        #region Product Interaction
        private IEnumerator FindShelvingUnit()
        {
            // Get a new shelving unit from the store manager.
            var newShelvingUnit = StoreManager.Instance.GetShelvingUnit();

            // If there's a current shelving unit that's different from the new one and is open, close it.
            if (shelvingUnit != null && shelvingUnit != newShelvingUnit && shelvingUnit.IsOpen)
            {
                shelvingUnit.Close(true, false);
            }

            // Assign the new shelving unit.
            shelvingUnit = newShelvingUnit;

            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Unregister the shelving unit from the store manager so other customers don't target it.
            StoreManager.Instance.UnregisterShelvingUnit(shelvingUnit);

            // Set the agent's destination to the front of the shelving unit.
            agent.SetDestination(shelvingUnit.Front);

            // Wait until the agent has arrived at the shelving unit.
            while (!HasArrived())
            {
                // If the shelving unit is moving, stop the agent and exit the coroutine.
                if (shelvingUnit.IsMoving)
                {
                    agent.SetDestination(transform.position);
                    shelvingUnit = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(shelvingUnit.transform);
        }

        private IEnumerator PickProduct()
        {
            // If no shelving unit is available, exit the coroutine.
            if (shelvingUnit == null) yield break;

            // Get a shelf from the shelving unit.
            var shelf = shelvingUnit.GetShelf();

            // If no shelf is available or the shelving unit is moving, re-register the shelving unit and exit.
            if (shelf == null || shelvingUnit.IsMoving)
            {
                StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
                yield break;
            }

            var product = shelf.Product;

            if (IsWillingToBuy(product))
            {
                // Add the product to the customer's inventory.
                inventory.Add(product);

                // Take the product model from the shelf.
                var productObj = shelf.TakeProductModel();

                // Open the shelving unit if it's not already open.
                if (!shelf.ShelvingUnit.IsOpen) shelf.ShelvingUnit.Open(true, false);

                // Determine the picking animation trigger based on the shelf height.
                float height = shelf.transform.position.y;
                string pickTrigger = "PickMedium";
                if (height < 0.5f) pickTrigger = "PickLow";
                else if (height > 1.5f) pickTrigger = "PickHigh";

                // Trigger the picking animation.
                animator.SetTrigger(pickTrigger);

                // Wait until the picking animation is complete.
                yield return new WaitUntil(() => isPicking);

                // Get the grip transform for the hand attachment.
                Transform grip = handAttachments.Grip;

                // Set the picked product's parent to the grip.
                productObj.transform.SetParent(grip);

                // Reset the isPicking flag.
                isPicking = false;

                // Animate the product moving to the hand.
                productObj.transform.DOLocalRotate(Vector3.zero, 0.25f);
                productObj.transform.DOLocalMove(Vector3.zero, 0.25f);

                // Wait until the animation is complete (Idle state).
                bool isIdle = false;
                while (!isIdle)
                {
                    isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                    yield return null;
                }

                // Destroy the temporary product object.
                Destroy(productObj);

                // Wait for a short delay.
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // If not willing to buy, display the "overpriced" dialogue.
                LocalizedString localizedDialog = trait.overpricedDialogue.GetRandomLine();

                if (localizedDialog != null)
                {
                    localizedDialog.Arguments = new object[] { new { productName = product.Name } };

                    string finalDialogText = localizedDialog.GetLocalizedString();

                    overheadUI.ShowDialog(finalDialogText);
                }
            }

            // Re-register the shelving unit with the store manager.
            StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
        }

        private bool IsWillingToBuy(Product product)
        {
            // 1. Calculate a price tolerance factor based on random value.
            float priceToleranceFactor = Random.Range(trait.priceToleranceRange.x, trait.priceToleranceRange.y);

            // 2. Calculate the maximum price this specific customer instance is willing to pay.
            decimal maxAcceptablePrice = product.MarketPrice * (decimal)priceToleranceFactor;

            // 3. Compare against the price the player has set in the store.
            decimal customPrice = DataManager.Instance.GetCustomProductPrice(product);

            // 4. Return true if the price is acceptable, false if it's "overpriced".
            return customPrice <= maxAcceptablePrice;
        }

        public void OnPick(AnimationEvent _)
        {
            isPicking = true;
        }
        #endregion

        #region Stealing Mechanics
        private IEnumerator StealProduct()
        {
            if (shelvingUnit == null) yield break;

            var shelf = shelvingUnit.GetShelf();
            if (shelf == null || shelvingUnit.IsMoving)
            {
                StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
                yield break;
            }

            var product = shelf.Product;

            animator.SetTrigger("Suspicious");
            yield return new WaitForSeconds(3.5f);

            if (product != null)
            {
                inventory.Add(product);
                StoreManager.Instance.RegisterThief(this);

                var productObj = shelf.TakeProductModel();

                if (!shelf.ShelvingUnit.IsOpen) shelf.ShelvingUnit.Open(true, false);

                // Determine the picking animation trigger based on the shelf height.
                float height = shelf.transform.position.y;
                string pickTrigger = "PickMedium";
                if (height < 0.5f) pickTrigger = "PickLow";
                else if (height > 1.5f) pickTrigger = "PickHigh";

                animator.SetTrigger(pickTrigger);

                // Wait until the picking animation is complete.
                yield return new WaitUntil(() => isPicking);

                Transform grip = handAttachments.Grip;
                productObj.transform.SetParent(grip);

                isPicking = false;

                // Animate the product moving to the hand.
                productObj.transform.DOLocalRotate(Vector3.zero, 0.25f);
                productObj.transform.DOLocalMove(Vector3.zero, 0.25f);

                // Wait until the animation is complete (Idle state).
                bool isIdle = false;
                while (!isIdle)
                {
                    isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                    yield return null;
                }

                // Destroy the temporary product object.
                Destroy(productObj);

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield break;
            }

            CreateThiefInstance();
            overheadUI.ShowThiefIcon();

            // Re-register the shelving unit with the store manager.
            StoreManager.Instance.RegisterShelvingUnit(shelvingUnit);
        }

        private IEnumerator CheckLeavingStoreAsThief()
        {
            while (StoreManager.Instance.IsWithinStore(transform.position))
            {
                yield return new WaitForSeconds(0.1f);
            }

            animator.SetFloat("Speed", 1f);
            agent.speed = trait.runSpeed;
        }

        private void CreateThiefInstance()
        {
            var prefab = GameConfig.Instance?.ThiefPrefab;
            if (prefab == null)
            {
                Debug.LogError("Missing ThiefPrefab.");
                return;
            }

            var thiefObj = Instantiate(prefab, transform);
            thiefObj.transform.localPosition = Vector3.zero;
            thiefObj.transform.localRotation = Quaternion.identity;
            thiefObj.transform.localScale = Vector3.one;

            if (!thiefObj.TryGetComponent(out thief))
            {
                Debug.LogError("Thief component missing on prefab.");
                Destroy(thiefObj);
                return;
            }

            thief.Initialize(this);
        }

        public void CatchCustomer()
        {
            if (IsCaught) return;

            IsCaught = true;
            StoreManager.Instance.UnregisterThief(this);
            StopAllCoroutines();
            StartCoroutine(CaughtSequence());
        }

        private IEnumerator CaughtSequence()
        {
            StoreManager.Instance.UnregisterThief(this);

            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);

            agent.speed = trait.walkSpeed;
            agent.SetDestination(transform.position);

            overheadUI.HideThiefIcon();

            var localizedCaught = trait.caughtThiefDialogue.GetRandomLine();
            if (localizedCaught != null)
            {
                overheadUI.ShowDialog(localizedCaught.GetLocalizedString());
            }

            animator.SetBool("IsDucking", true);
            yield return new WaitForSeconds(5f);
            animator.SetBool("IsDucking", false);

            if (thief != null)
                Destroy(thief.gameObject);

            DataManager.Instance.PlayerMoney += inventory.Sum(p => p.Price);
            inventory.Clear();
            AudioManager.Instance.PlaySFX(AudioID.Kaching);

            yield return Leave();
        }
        #endregion

        #region Checkout & Payment
        private IEnumerator Checkout()
        {
            checkoutCounter = StoreManager.Instance.FindOptimalCounter(transform.position);
            checkoutCounter.LiningCustomers.Add(this);
            yield return UpdateQueue();
            yield return checkoutCounter.PlaceProducts(this);
            yield return new WaitUntil(() => checkoutCounter.CurrentState == CounterState.Standby);
        }

        private IEnumerator UpdateQueue()
        {
            // While the customer's queue number is greater than 0 (meaning they are still in the queue).
            while (queueNumber > 0)
            {
                int newQueueNumber = checkoutCounter.GetQueueNumber(this);

                // Check if the customer's queue number has improved (become lower).
                if (newQueueNumber < queueNumber)
                {
                    // Update the customer's queue number.
                    queueNumber = newQueueNumber;

                    Vector3 queuePosition = checkoutCounter.GetQueuePosition(this, out Vector3 lookDirection);

                    // Move the customer to their new queue position.
                    yield return MoveTo(queuePosition);

                    // Make the customer look in the correct direction at their new position.
                    yield return LookAt(lookDirection);
                }
                else
                {
                    // If the queue number hasn't improved, wait briefly before checking again.
                    yield return new WaitForSeconds(0.1f);
                }
            }
            // When the queueNumber is 0, this coroutine will stop.
        }

        public IEnumerator HandsPayment(decimal totalPrice, Cashier cashier, System.Action<decimal> onMoneyHanded)
        {
            bool isPaying = true;
            bool isUsingCash = PrefersCash;
            decimal amountToGive = isUsingCash ? GetPaymentAmount(totalPrice) : totalPrice;

            animator.SetBool("IsPaying", isPaying);
            handAttachments.ActivatePaymentObject(isUsingCash);

            Camera mainCamera = Camera.main;

            // Continue the payment process until isPaying is false.
            while (isPaying)
            {
                // If a cashier is available, simulate payment with the cashier (auto-scan).
                if (cashier != null)
                {
                    yield return new WaitForSeconds(0.3f);
                    cashier.TakePayment();
                    yield return new WaitForSeconds(0.7f);
                    isPaying = false;
                }
                // Otherwise, allow the player to manually process the payment (e.g., started by clicking on a payment object).
                else if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, 10f, GameConfig.Instance.PaymentLayer))
                    {
                        isPaying = false;
                    }
                }

                yield return null;
            }

            onMoneyHanded?.Invoke(amountToGive);

            animator.SetBool("IsPaying", false);
            handAttachments.DeactivatePaymentObjects();
        }

        private decimal GetPaymentAmount(decimal totalPrice)
        {
            PaymentStyle styleToUse = trait.preferredPaymentStyle;

            if (styleToUse == PaymentStyle.Random)
            {
                styleToUse = (PaymentStyle)Random.Range(1, System.Enum.GetValues(typeof(PaymentStyle)).Length);
            }

            int totalCents = Mathf.RoundToInt((float)totalPrice * 100);

            switch (styleToUse)
            {
                case PaymentStyle.ExactChange:
                    // 30% chance they actually HAVE the exact change they need
                    if (activeCurrency.CanPayExactly(totalCents) && Random.value < 0.3f)
                    {
                        return totalPrice;
                    }
                    // Fallback to Smallest Excess if they "don't have it"
                    return activeCurrency.GetSmallestExcessAmount(totalCents) / 100m;

                case PaymentStyle.ChangeOptimizer:
                    int optimizedTotal = activeCurrency.GetOptimizedTotal(totalCents);
                    int baseBill = activeCurrency.GetSmallestExcessAmount(totalCents);
                    int extraCents = optimizedTotal - baseBill;

                    // They only optimize if they "have" the extra cents (30% chance)
                    if (extraCents > 0 && activeCurrency.CanPayExactly(extraCents) && Random.value < 0.3f)
                    {
                        return optimizedTotal / 100m;
                    }
                    return baseBill / 100m;

                case PaymentStyle.RoundUp:
                    return activeCurrency.GetRoundedUpAmount(totalCents) / 100m;

                case PaymentStyle.SmallestExcess:
                    return activeCurrency.GetSmallestExcessAmount(totalCents) / 100m;

                case PaymentStyle.BigBills:
                    // Rounds up to the nearest $5.00 increment
                    int nearestHigher = Mathf.CeilToInt(totalCents / 500f) * 500;
                    return (nearestHigher > totalCents ? nearestHigher : totalCents) / 100m;

                default:
                    return totalPrice;
            }
        }
        #endregion

        #region Movement Utilities
        private IEnumerator MoveTo(Vector3 position)
        {
            agent.SetDestination(position);

            yield return new WaitUntil(() => HasArrived());

            // Wait for the end of the frame.
            // This can be useful for ensuring animations or other visual updates have taken place.
            yield return new WaitForEndOfFrame();
        }

        private IEnumerator LookAt(Transform target)
        {
            var lookDirection = (target.position - transform.position).Flatten();
            var lookRotation = Quaternion.LookRotation(lookDirection);
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private IEnumerator LookAt(Vector3 lookDirection)
        {
            var lookRotation = Quaternion.LookRotation(lookDirection.Flatten());
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        private bool HasArrived()
        {
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator.SetBool("IsMoving", false);
                        return true;
                    }
                }
            }

            animator.SetBool("IsMoving", true);
            return false;
        }
        #endregion
    }
}
