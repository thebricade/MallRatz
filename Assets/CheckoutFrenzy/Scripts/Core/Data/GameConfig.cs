using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    public class GameConfig : ScriptableObject
    {
        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                GameConfig[] configs = Resources.LoadAll<GameConfig>("");

                if (configs.Length == 0)
                {
                    Debug.LogError(
                        "GameConfig not found in Resources!\n" +
                        "Please create one using Tools > Checkout Frenzy > Game Config."
                    );
                    return null;
                }

                if (configs.Length > 1)
                {
                    Debug.LogWarning("Multiple GameConfig assets found. Using the first one.");
                }

                _instance = configs[0];
                return _instance;
            }
        }

        [Header("Store Settings")]
        [SerializeField, Tooltip("The default name for the store.")]
        private string defaultStoreName = "AWESOME MART";

        [SerializeField, Tooltip("The maximum number of characters allowed for the store name.")]
        private int storeNameMaxCharacters = 15;

        [SerializeField, Tooltip("The initial amount of money the player starts with.")]
        private int startingMoney = 2000;

        [SerializeField, Tooltip("The time range during which the store is open for business.")]
        private TimeRange openTime;



        [Header("Customer Settings")]
        [SerializeField, Tooltip("The minimum time (in seconds) between customer spawns.")]
        private float minSpawnTime = 5f;

        [SerializeField, Tooltip("The maximum time (in seconds) between customer spawns.")]
        private float maxSpawnTime = 15f;

        [SerializeField, Tooltip("The base maximum number of customers that can be in the store at the same time.")]
        private int baseMaxCustomers = 5;

        [SerializeField, Tooltip("The thief prefab attached to the customers when the stole goods.")]
        private GameObject thiefPrefab;



        [Header("Currency Settings")]
        [SerializeField, Tooltip("Currency configuration defining the symbol, bill denominations, and sprites used in the game.")]
        private CurrencyData activeCurrency;



        [Header("Level System")]
        [SerializeField, Tooltip("Base experience required for level 1.")]
        private int baseExperience = 5;

        [SerializeField, Tooltip("Exponential growth factor for experience requirements.")]
        private float growthFactor = 1.1f;



        [Header("Game Layers")]
        [SerializeField, Tooltip("Layer used for interactable objects (Furnitures, Counter, PC, etc.)")]
        private LayerMask interactableLayer;

        [SerializeField, Tooltip("Layer used for items currently being processed at the checkout counter.")]
        private LayerMask checkoutItemLayer;

        [SerializeField, Tooltip("Layer used for payment method GameObjects (either cash or card).")]
        private LayerMask paymentLayer;

        [SerializeField, Tooltip("Layer used to determine valid placement locations for furniture objects.")]
        private LayerMask groundLayer;

        [SerializeField, Tooltip("Layer used by Shelves in Shelving Units.")]
        private LayerMask shelfLayer;

        [SerializeField, Tooltip("Layer used by Racks in Storage Racks.")]
        private LayerMask rackLayer;

        [SerializeField, Tooltip("Layer used by objects that can be held by the player. Objects on this layer are rendered on top of everything else using a special camera to prevent clipping when held.")]
        private LayerMask heldObjectLayer;

        [SerializeField, Tooltip("Layer used by the player.")]
        private LayerMask playerLayer;



        [Header("Cleaning Settings")]
        [SerializeField, Tooltip("How many Cleanables can be spawned in the game.")]
        private int maxCleanables = 10;



        [Header("Control Settings")]
        [SerializeField, Tooltip("Selected control mode for the game.")]
        private ControlMode controlMode;

        // Store Settings
        public string DefaultStoreName => defaultStoreName;
        public int StoreNameMaxCharacters => storeNameMaxCharacters;
        public int StartingMoney => startingMoney;
        public TimeRange OpenTime => openTime;

        // Customer Settings
        public float GetRandomSpawnTime => Random.Range(minSpawnTime, maxSpawnTime);
        public int BaseMaxCustomers => baseMaxCustomers;
        public GameObject ThiefPrefab => thiefPrefab;

        // Currency Settings
        public CurrencyData ActiveCurrency => activeCurrency;

        // Level System
        public int BaseExperience => baseExperience;
        public float GrowthFactor => growthFactor;

        // Game Layers
        public LayerMask InteractableLayer => interactableLayer;
        public LayerMask CheckoutItemLayer => checkoutItemLayer;
        public LayerMask PaymentLayer => paymentLayer;
        public LayerMask GroundLayer => groundLayer;
        public LayerMask ShelfLayer => shelfLayer;
        public LayerMask RackLayer => rackLayer;
        public LayerMask HeldObjectLayer => heldObjectLayer;
        public LayerMask PlayerLayer => playerLayer;

        // Cleaning
        public int MaxCleanables => maxCleanables;

        // Control Mode
        public ControlMode ControlMode => controlMode;
    }
}
