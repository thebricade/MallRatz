using System.Collections.Generic;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [CreateAssetMenu(fileName = "NewProduct", menuName = "Checkout Frenzy/Store/Product")]
    public class Product : ScriptableObject, IPurchasable
    {
        [SerializeField, Tooltip("Unique identifier for this product.")]
        private int productId;

        [SerializeField, Tooltip("The general classification this product falls under (affects profit margins).")]
        private ProductCategory category;

        [SerializeField, Tooltip("The display name of the product.")]
        private new string name;

        [SerializeField, Tooltip("The 2D icon used for user interfaces.")]
        private Sprite icon;

        [SerializeField, Tooltip("The base cost of the product in cents (e.g., 100 = $1.00).")]
        private long priceInCents;

        [SerializeField, Tooltip("Time in seconds it takes for the order to arrive.")]
        private int orderTime = 5;

        [SerializeField, Tooltip("The specific type of display furniture this product requires (e.g., Shelf, Fridge, or Freezer).")]
        private DisplaySection section;

        [SerializeField, Tooltip("The 3D prefab used when the product is displayed on a box/shelf.")]
        private GameObject model;

        [SerializeField, Tooltip("The prefab representing the shipping box container.")]
        private GameObject box;

        [SerializeField, Tooltip("If true, uses the manual Box Quantity vector instead of calculating based on Mesh size.")]
        private bool overrideBoxQuantity;

        [SerializeField, Tooltip("How many items fit in a box (X * Y * Z). Only used if override is enabled.")]
        private Vector3Int boxQuantity;

        [SerializeField, Tooltip("If true, uses the manual Shelf Quantity vector instead of calculating based on Mesh size.")]
        private bool overrideShelfQuantity;

        [SerializeField, Tooltip("How many items fit on a shelf unit (X * Y * Z). Only used if override is enabled.")]
        private Vector3Int shelfQuantity;

        private readonly Dictionary<ProductCategory, decimal> profitMargins = new Dictionary<ProductCategory, decimal>()
        {
            { ProductCategory.FoodAndBeverages, 0.25m },
            { ProductCategory.PersonalCareAndHygiene, 0.30m },
            { ProductCategory.HouseholdItems, 0.35m },
            { ProductCategory.HealthAndWellness, 0.40m },
            { ProductCategory.ElectronicsAndAccessories, 0.45m },
            { ProductCategory.Miscellaneous, 0.50m }
        };

        public int ProductID => productId;
        public ProductCategory Category => category;

        public string Name => name;
        public Sprite Icon => icon;
        public decimal Price => priceInCents / 100m;
        public int OrderTime => orderTime;
        public DisplaySection Section => section;

        public GameObject Model => model;
        public GameObject Box => box;

        public bool OverrideBoxQuantity => overrideBoxQuantity;
        public Vector3Int BoxQuantity => boxQuantity;
        public bool OverrideShelfQuantity => overrideShelfQuantity;
        public Vector3Int ShelfQuantity => shelfQuantity;

        public Vector3 Size
        {
            get
            {
                if (model == null)
                {
                    Debug.LogWarning("Model is not assigned for product " + name);
                    return Vector3.zero;
                }

                var meshRenderer = model.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Debug.LogWarning("MeshRenderer is missing on the model for product " + name);
                    return Vector3.zero;
                }

                return meshRenderer.bounds.size;
            }
        }

        public decimal MarketPrice => CalculateMarketPrice();

        public Vector3Int FitOnContainer(Vector3 containerSize)
        {
            int fitX = Mathf.FloorToInt(containerSize.x / Size.x);
            int fitY = Mathf.FloorToInt(containerSize.y / Size.y);
            int fitZ = Mathf.FloorToInt(containerSize.z / Size.z);

            return new Vector3Int(fitX, fitY, fitZ);
        }

        public int GetBoxQuantity()
        {
            if (box == null) return -1;

            if (overrideBoxQuantity) return boxQuantity.x * boxQuantity.y * boxQuantity.z;

            Vector3Int fit = FitOnContainer(box.GetComponent<IBox>().Size);
            return fit.x * fit.y * fit.z;
        }

        private decimal CalculateMarketPrice()
        {
            decimal profitMargin = profitMargins[category];
            decimal profit = Price * profitMargin;
            return Price + profit;
        }
    }
}
