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
        // Registry of every StoreManager currently active, used for spatial lookups.
        // This is intentionally NOT a singleton: each shop plot owns its own StoreManager instance.
        private static readonly List<StoreManager> allStores = new List<StoreManager>();

        /// <summary>
        /// Finds the StoreManager whose bounds contain the given world position.
        /// Used by gameplay objects (furniture, boxes, employees) that are not parented
        /// under a specific shop's hierarchy but still need to resolve which shop they belong to.
        /// </summary>
        /// <param name="worldPosition">The world position to test.</param>
        /// <returns>The owning StoreManager, or null if the position isn't within any shop's bounds.</returns>
        public static StoreManager GetStoreAt(Vector3 worldPosition)
        {
            for (int i = 0; i < allStores.Count; i++)
            {
                if (allStores[i].IsWithinStore(worldPosition)) return allStores[i];
            }

            return null;
        }

        /// <summary>
        /// The DataManager for this specific shop. Both components live on the same ShopPlot root.
        /// </summary>
        public DataManager DataManager { get; private set; }

        /// <summary>
        /// The MissionManager, EmployeeManager, and FinanceManager for this specific shop.
        /// All shop-scoped managers live as sibling components on the same ShopPlot root.
        /// </summary>
        public MissionManager MissionManager { get; private set; }
        public EmployeeManager EmployeeManager { get; private set; }
        public FinanceManager FinanceManager { get; private set; }
        public WarehouseManager WarehouseManager { get; private set; }

        /// <summary>The PC (order terminal) within this shop.</summary>
        public PC PC { get; private set; }

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
            .Take(DataManager.Data.ExpansionLevel)
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
            DataManager = GetComponent<DataManager>();
            MissionManager = GetComponent<MissionManager>();
            EmployeeManager = GetComponent<EmployeeManager>();
            FinanceManager = GetComponent<FinanceManager>();
            WarehouseManager = GetComponent<WarehouseManager>();
            PC = GetComponentInChildren<PC>(true);

            navMeshSurface = GetComponent<NavMeshSurface>();

            openTime = GameConfig.Instance.OpenTime;
        }

        private void OnEnable() => allStores.Add(this);
        private void OnDisable() => allStores.Remove(this);

        private IEnumerator Start()
        {
            // Wait until this shop's DataManager is initialized and its data is loaded.
            yield return new WaitUntil(() => DataManager != null && DataManager.IsLoaded);

            DataManager.OnSave += () =>
            {
                // Calculate and store the total value of products currently held by customers but not yet paid for.
                decimal productsValue = 0m;
                foreach (var customer in customers)
                {
                    foreach (var product in customer.Inventory)
                    {
                        decimal price = DataManager.GetCustomProductPrice(product);
                        productsValue += price;
                    }
                }

                DataManager.Data.UnpaidProductsValue = productsValue;
            };

            // Restore purchased Expansions
            for (int i = 0; i < expansions.Count; i++)
            {
                bool isPurchased = i < DataManager.Data.ExpansionLevel;
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
                    customer.AssignStore(this);
                    customers.Add(customer);
                    customer.OnLeave += () => customers.Remove(customer);

                    DataManager.Data.CurrentSummary.TotalCustomers++;
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
        /// Checks if the given world position is within the bounds of this specific shop.
        /// Bounds are authored in local space so the same prefab produces correct
        /// per-instance world bounds regardless of where each shop plot is placed.
        /// </summary>
        /// <param name="position">The world position to check.</param>
        /// <returns>True if the position is within the store bounds, otherwise false.</returns>
        public bool IsWithinStore(Vector3 position)
        {
            Vector3 localPosition = transform.InverseTransformPoint(position);
            return storeBounds.Contains(localPosition);
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
            if (DataManager.PlayerMoney < license.Price)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            // Grants the purchased license
            foreach (var product in license.Products)
            {
                DataManager.Data.LicensedProducts.Add(product.ProductID);
            }

            DataManager.Data.OwnedLicenses.Add(license.LicenseID);

            DataManager.PlayerMoney -= (decimal)license.Price;

            MissionManager.UpdateMission(MissionGoal.License, 1, license.LicenseID);

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
            return expansionIndex < DataManager.Data.ExpansionLevel;
        }

        /// <summary>
        /// Checks if the specified expansion is the currently active expansion.
        /// </summary>
        /// <param name="expansion">The expansion to check.</param>
        /// <returns>True if the expansion is the current expansion, otherwise false.</returns>
        public bool IsCurrentExpansion(Expansion expansion)
        {
            int expansionIndex = expansions.IndexOf(expansion);
            return expansionIndex == DataManager.Data.ExpansionLevel;
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
            var expansion = expansions[DataManager.Data.ExpansionLevel];

            if (DataManager.PlayerMoney < expansion.UnlockPrice)
            {
                UIEvents.RaiseMessage(insufficientFundsMessage.GetLocalizedString(), Color.red);
                return false;
            }

            expansion.SetPurchasedState(true);
            DataManager.Data.ExpansionLevel++;

            if (!DataManager.Data.IsWarehouseUnlocked)
                DataManager.Data.IsWarehouseUnlocked = expansion.UnlockWarehouse;

            DataManager.PlayerMoney -= (decimal)expansion.UnlockPrice;

            UpdateNavMeshSurface();

            OnExpansionPurchased?.Invoke(DataManager.Data.ExpansionLevel);

            AudioManager.Instance.PlaySFX(AudioID.Kaching);
            AudioManager.Instance.PlaySFX(AudioID.Construction);

            return true;
        }

        private IEnumerator EndDay()
        {
            isTodayEnded = true;
            lastTotalDays = DataManager.Data.TotalDays;

            // The world runs on a single global day/night cycle: every shop closes at the
            // same in-game time. Only the local player's own claimed shop should pause the
            // shared clock and show a summary popup; every other shop silently rolls over
            // to the next day without any UI or player input.
            bool isMyStore = ClientShopContext.Instance != null && ClientShopContext.Instance.MyStore == this;

            if (isMyStore) TimeManager.Instance.AllowTimeUpdate = false;

            if (spawnCustomerCoroutine != null)
            {
                StopCoroutine(spawnCustomerCoroutine);
                spawnCustomerCoroutine = null;
            }

            AskCustomersToLeave();

            yield return new WaitWhile(() => customers.Count > 0);

            if (isMyStore)
            {
                StoreEvents.RaiseSummaryRequested(DataManager.Data.CurrentSummary, (skip) =>
                {
                    if (skip) SkipToNextDay();
                    else StartCoroutine(ShowSkipDialog());

                    TimeManager.Instance.AllowTimeUpdate = true;
                });
            }
            else
            {
                // Not the local player's shop: silently roll this shop over to the next
                // day without ever touching the shared global clock. The clock itself is
                // only ever fast-forwarded by the player's own claimed shop, so a busy
                // shop doesn't jump the world forward while the player is still reading
                // their own summary popup.
                AdvanceToNextDay();
            }

            AudioManager.Instance.StopBGMQueue();
        }

        private void AdvanceToNextDay()
        {
            if (lastTotalDays == DataManager.Data.TotalDays) DataManager.Data.TotalDays++;

            RestartDay();
        }

        private void SkipToNextDay()
        {
            int hour = openTime.StartHour;
            int minute = openTime.StartMinute;

            TimeManager.Instance.SetTime(hour, minute);

            AdvanceToNextDay();
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

            DataManager.Data.CurrentSummary = new SummaryData(DataManager.PlayerMoney);

            OnDayStarted?.Invoke(DataManager.Data.TotalDays);

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
                    WarehouseManager != null && WarehouseManager.GetRackWithProduct(shelf.AssignedProduct) != null
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
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(storeBounds.center, storeBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
