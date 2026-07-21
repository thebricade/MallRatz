using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Unity.AI.Navigation;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class StoreManager : MonoBehaviour
    {
        public static StoreManager Instance { get; private set; }

        [Header("Messages")]
        [SerializeField, Tooltip("Shown when the player doesn't have enough money.")]
        private LocalizedString insufficientFundsMessage;

        [Header("Store Settings")]
        [SerializeField, Tooltip("The 3D bounding box of the store.")]
        private Bounds storeBounds;

        [SerializeField, Tooltip("The point where deliveries are made (e.g., Products, Furnitures).")]
        private Transform deliveryPoint;

        [SerializeField, Tooltip("The points where customers spawn then walk into the store.")]
        private List<Transform> spawnPoints;

        [SerializeField, Tooltip("The prefabs of the different customer types that can spawn.")]
        private List<Customer> customerPrefabs;

        [SerializeField, Tooltip("The list of available expansions for the store.")]
        private List<Expansion> expansions;

        public Transform DeliveryPoint => deliveryPoint;
        public List<Expansion> Expansions => expansions;

        private FurnitureBox furnitureBoxPrefab;
        public FurnitureBox FurnitureBoxPrefab
        {
            get
            {
                if (furnitureBoxPrefab == null)
                    furnitureBoxPrefab = Resources.Load<FurnitureBox>("Boxes/FurnitureBox");

                return furnitureBoxPrefab;
            }
        }

        public event System.Action<int> OnDayStarted;
        public event System.Action<License> OnLicensePurchased;
        public event System.Action<int> OnExpansionPurchased;
        public event System.Action<float> OnCleanablesChanged;

        private NavMeshSurface navMeshSurface;
        public void UpdateNavMeshSurface() => navMeshSurface.BuildNavMesh();

        private List<CheckoutCounter> checkoutCounters = new List<CheckoutCounter>();
        public void RegisterCounter(CheckoutCounter counter) => checkoutCounters.Add(counter);

        // List of valid shelving units within the store's boundaries where customers can interact with products.
        private HashSet<ShelvingUnit> shelvingUnits = new HashSet<ShelvingUnit>();
        public void RegisterShelvingUnit(ShelvingUnit shelvingUnit) => shelvingUnits.Add(shelvingUnit);
        public void UnregisterShelvingUnit(ShelvingUnit shelvingUnit) => shelvingUnits.Remove(shelvingUnit);

        private List<Cleanable> cleanables = new List<Cleanable>();

        public bool CanSpawnCleanable => cleanables.Count < GameConfig.Instance.MaxCleanables;
        public float Dirtiness => (float)cleanables.Count / GameConfig.Instance.MaxCleanables;

        /// <summary>
        /// Calculates the maximum number of customers that can be in the store at the same time. 
        /// This value is determined by the base maximum number of customers 
        /// plus the sum of additional customers provided by purchased expansions.
        /// </summary>
        private int maxCustomers => GameConfig.Instance.BaseMaxCustomers + expansions
            .Take(DataManager.Instance.Data.ExpansionLevel)
            .Sum(expansion => expansion.AdditionalCustomers);

        private Coroutine spawnCustomerCoroutine;
        private List<Customer> customers = new List<Customer>();
        private List<Customer> thieves = new List<Customer>();

        private TimeRange openTime; // The time range during which the store is open for business.
        private bool isOpen;        // Indicates whether the store is currently open for business (using store sign).          
        private bool isTodayEnded;  // Indicates whether the current in-game day has ended (business time end hour).
        private int lastTotalDays;  // Stores the total number of days that have passed since the game started.

        private void Awake()
        {
            Instance = this;

            navMeshSurface = GetComponent<NavMeshSurface>();

            openTime = GameConfig.Instance.OpenTime;
        }

        private IEnumerator Start()
        {
            // Wait until the DataManager is initialized and its data is loaded.
            yield return new WaitUntil(() =>
                DataManager.Instance != null && DataManager.Instance.IsLoaded
            );

            DataManager.Instance.OnSave += () =>
            {
                // Calculate and store the total value of products currently held by customers but not yet paid for.
                decimal productsValue = 0m;
                foreach (var customer in customers)
                {
                    foreach (var product in customer.Inventory)
                    {
                        decimal price = DataManager.Instance.GetCustomProductPrice(product);
                        productsValue += price;
                    }
                }

                DataManager.Instance.Data.UnpaidProductsValue = productsValue;
            };

            // Restore purchased Expansions
            for (int i = 0; i < expansions.Count; i++)
            {
                bool isPurchased = i < DataManager.Instance.Data.ExpansionLevel;
                expansions[i].SetPurchasedState(isPurchased);
            }

            // Wait a bit to ensure that all scene components have been initialized.
            yield return new WaitForEndOfFrame();

            UpdateNavMeshSurface();

            if (IsOpenTime())
            {
                spawnCustomerCoroutine = StartCoroutine(SpawnCustomer());

                AudioManager.Instance.PlayBGMQueue();
            }
        }

        private void Update()
        {
            if (!isTodayEnded && !IsOpenTime())
            {
                StartCoroutine(EndDay());
            }
        }

        private bool IsOpenTime()
        {
            int totalMinutes = TimeManager.Instance.TotalMinutes;
            return openTime.IsWithinRange(totalMinutes);
        }

        private IEnumerator SpawnCustomer()
        {
            while (true)
            {
                float waitTime = GameConfig.Instance.GetRandomSpawnTime;
                yield return new WaitForSeconds(waitTime);

                if (isOpen && customers.Count < maxCustomers && shelvingUnits.Count > 0 && Random.value > Dirtiness)
                {
                    int randomCustomerIndex = Random.Range(0, customerPrefabs.Count);
                    var customerPrefab = customerPrefabs[randomCustomerIndex];

                    int randomSpawnIndex = Random.Range(0, spawnPoints.Count);
                    var spawnPoint = spawnPoints[randomSpawnIndex];

                    var customer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
                    customers.Add(customer);
                    customer.OnLeave += () => customers.Remove(customer);

                    DataManager.Instance.Data.CurrentSummary.TotalCustomers++;
                }
            }
        }

        /// <summary>
        /// Validates the position of a ShelvingUnit. 
        /// 
        /// If the ShelvingUnit is within the store bounds, it is registered; 
        /// otherwise, it is unregistered.
        /// </summary>
        /// <param name="shelvingUnit">The ShelvingUnit to validate.</param>
        public void ValidateShelvingUnit(ShelvingUnit shelvingUnit)
        {
            Vector3 position = shelvingUnit.transform.position;

            if (IsWithinStore(position)) RegisterShelvingUnit(shelvingUnit);
            else UnregisterShelvingUnit(shelvingUnit);
        }

        /// <summary>
        /// Gets a random ShelvingUnit from the list of registered shelving units.
        /// </summary>
        /// <returns>A randomly selected ShelvingUnit, or null if no shelving units are registered.</returns>
        public ShelvingUnit GetShelvingUnit()
        {
            if (shelvingUnits.Count == 0) return null;

            int randomIndex = Random.Range(0, shelvingUnits.Count);
            var shelvingUnit = shelvingUnits.ElementAt(randomIndex);

            return shelvingUnit;
        }

        /// <summary>
        /// Checks if the given position is within the bounds of the store.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>True if the position is within the store bounds, otherwise false.</returns>
        public bool IsWithinStore(Vector3 position)
        {
            return storeBounds.Contains(position);
        }

        /// <summary>
        /// Attempts to purchase the specified license.
        /// 
        /// Checks if the player has enough money. If so, deducts the license price from the player's money, 
        /// grants the license to the player, and updates the game state accordingly.
        /// </summary>
        /// <param name="license">The license to be purchased.</param>
        /// <returns>True if the license was purchased successfully, otherwise false.</returns>
        public bool PurchaseLicense(License license)
        {
            if (DataManager.Instance.PlayerMoney < license.Price)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            // Grants the purchased license
            foreach (var product in license.Products)
            {
                DataManager.Instance.Data.LicensedProducts.Add(product.ProductID);
            }

            DataManager.Instance.Data.OwnedLicenses.Add(license.LicenseID);

            DataManager.Instance.PlayerMoney -= (decimal)license.Price;

            MissionManager.Instance.UpdateMission(MissionGoal.License, 1, license.LicenseID);

            OnLicensePurchased?.Invoke(license);

            AudioManager.Instance.PlaySFX(AudioID.Kaching);

            return true;
        }

        /// <summary>
        /// Checks if the specified expansion has been purchased.
        /// </summary>
        /// <param name="expansion">The expansion to check.</param>
        /// <returns>True if the expansion has been purchased, otherwise false.</returns>
        public bool IsExpansionPurchased(Expansion expansion)
        {
            int expansionIndex = expansions.IndexOf(expansion);
            return expansionIndex < DataManager.Instance.Data.ExpansionLevel;
        }

        /// <summary>
        /// Checks if the specified expansion is the currently active expansion.
        /// </summary>
        /// <param name="expansion">The expansion to check.</param>
        /// <returns>True if the expansion is the current expansion, otherwise false.</returns>
        public bool IsCurrentExpansion(Expansion expansion)
        {
            int expansionIndex = expansions.IndexOf(expansion);
            return expansionIndex == DataManager.Instance.Data.ExpansionLevel;
        }

        /// <summary>
        /// Attempts to purchase the next available expansion.
        /// 
        /// Checks if the player has enough money. If so, deducts the expansion price, 
        /// updates the expansion level, and updates the game state accordingly.
        /// </summary>
        /// <returns>True if the expansion was purchased successfully, otherwise false.</returns>
        public bool PurchaseExpansion()
        {
            var expansion = expansions[DataManager.Instance.Data.ExpansionLevel];

            if (DataManager.Instance.PlayerMoney < expansion.UnlockPrice)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            expansion.SetPurchasedState(true);
            DataManager.Instance.Data.ExpansionLevel++;

            if (!DataManager.Instance.Data.IsWarehouseUnlocked)
                DataManager.Instance.Data.IsWarehouseUnlocked = expansion.UnlockWarehouse;

            DataManager.Instance.PlayerMoney -= (decimal)expansion.UnlockPrice;

            UpdateNavMeshSurface();

            OnExpansionPurchased?.Invoke(DataManager.Instance.Data.ExpansionLevel);

            AudioManager.Instance.PlaySFX(AudioID.Kaching);
            AudioManager.Instance.PlaySFX(AudioID.Construction);

            return true;
        }

        private IEnumerator EndDay()
        {
            isTodayEnded = true;
            lastTotalDays = DataManager.Instance.Data.TotalDays;

            TimeManager.Instance.AllowTimeUpdate = false;

            if (spawnCustomerCoroutine != null)
            {
                StopCoroutine(spawnCustomerCoroutine);
                spawnCustomerCoroutine = null;
            }

            AskCustomersToLeave();

            yield return new WaitWhile(() => customers.Count > 0);

            StoreEvents.RaiseSummaryRequested(DataManager.Instance.Data.CurrentSummary, (skip) =>
            {
                if (skip) SkipToNextDay();
                else StartCoroutine(ShowSkipDialog());

                TimeManager.Instance.AllowTimeUpdate = true;
            });

            AudioManager.Instance.StopBGMQueue();
        }

        private void SkipToNextDay()
        {
            if (lastTotalDays == DataManager.Instance.Data.TotalDays) DataManager.Instance.Data.TotalDays++;

            int hour = openTime.StartHour;
            int minute = openTime.StartMinute;

            TimeManager.Instance.SetTime(hour, minute);

            RestartDay();
        }

        private IEnumerator ShowSkipDialog()
        {
            bool skipConfirmed = false;
            System.Action closeUIFromLogic = null; // Local variable to hold the UI's hide method

            // We pass two things: the skip logic, and a way for the UI to "hand us" its hide method
            StoreEvents.RaiseSkipDialogRequested(
                () => skipConfirmed = true,
                (hideMethod) => closeUIFromLogic = hideMethod
            );

            // Wait until the user clicks skip OR the store naturally opens
            yield return new WaitUntil(() => skipConfirmed || IsOpenTime());

            // If the code reaches here, the dialog is no longer needed.
            // Tell the UI to close itself using the method it gave us.
            closeUIFromLogic?.Invoke();

            if (skipConfirmed)
            {
                SkipToNextDay();
                yield break;
            }

            RestartDay();
        }

        private void RestartDay()
        {
            isTodayEnded = false;

            DataManager.Instance.Data.CurrentSummary = new SummaryData(DataManager.Instance.PlayerMoney);

            OnDayStarted?.Invoke(DataManager.Instance.Data.TotalDays);

            spawnCustomerCoroutine = StartCoroutine(SpawnCustomer());

            AudioManager.Instance.PlayBGMQueue();
        }

        /// <summary>
        /// Toggles the open/closed state of the store.
        /// 
        /// If the store is opened, customers can enter and shop. 
        /// If the store is closed, customers are asked to leave.
        /// </summary>
        /// <param name="isOpen">Whether to open or close the store.</param>
        public void ToggleOpenState(bool isOpen)
        {
            this.isOpen = isOpen;
            if (!isOpen) AskCustomersToLeave();
        }

        private void AskCustomersToLeave()
        {
            var leavingCustomers = new List<Customer>(customers);
            leavingCustomers.ForEach(customer => customer.AskToLeave());
        }

        public CheckoutCounter GetCounterAtIndex(int index)
        {
            if (checkoutCounters == null || index < 0 || index >= checkoutCounters.Count)
                return null;

            return checkoutCounters[index];
        }

        public CheckoutCounter FindOptimalCounter(Vector3 customerPosition)
        {
            return checkoutCounters
                .OrderBy(counter => counter.LiningCustomers.Count)
                .ThenBy(counter => Vector3.SqrMagnitude(counter.transform.position - customerPosition))
                .FirstOrDefault();
        }

        public Vector3 GetExitPoint()
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return Vector3.zero;

            return spawnPoints[Random.Range(0, spawnPoints.Count)].position;
        }

        public Shelf GetShelfToStock()
        {
            return shelvingUnits
                .SelectMany(shelvingUnit => shelvingUnit.Shelves)
                .Where(shelf =>
                    !shelf.IsTargeted &&
                    shelf.AssignedProduct != null &&
                    (shelf.Product == null || !shelf.IsFull) &&
                    WarehouseManager.Instance.GetRackWithProduct(shelf.AssignedProduct) != null
                )
                .OrderBy(shelf => shelf.Quantity)
                .FirstOrDefault();
        }

        public void RegisterCleanable(Cleanable cleanable)
        {
            cleanables.Add(cleanable);
            OnCleanablesChanged?.Invoke(Dirtiness);
        }

        public void UnregisterCleanable(Cleanable cleanable)
        {
            cleanables.Remove(cleanable);
            OnCleanablesChanged?.Invoke(Dirtiness);
        }

        public Cleanable GetNearestCleanable(Vector3 position)
        {
            return cleanables
                .Where(c => !c.HasCleaner)
                .OrderBy(c => Vector3.Distance(c.transform.position, position))
                .FirstOrDefault();
        }

        public void RegisterThief(Customer customer) => thieves.Add(customer);
        public void UnregisterThief(Customer customer) => thieves.Remove(customer);

        public Customer GetNearestThief(Vector3 position)
        {
            return thieves
                .OrderBy(c => Vector3.Distance(c.transform.position, position))
                .FirstOrDefault();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(storeBounds.center, storeBounds.size);
        }
#endif
    }
}
