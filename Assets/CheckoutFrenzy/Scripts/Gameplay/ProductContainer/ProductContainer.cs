using System.Collections.Generic;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    /// <summary>
    /// Abstract base class for containers that can hold products, 
    /// such as shelves and boxes.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class ProductContainer : Interactable
    {
        /// <summary>
        /// The product currently stored in the container.
        /// </summary>
        public Product Product { get; protected set; }

        private BoxCollider m_boxCollider;
        protected BoxCollider boxCollider
        {
            get
            {
                if (m_boxCollider == null)
                    m_boxCollider = GetComponent<BoxCollider>();

                return m_boxCollider;
            }
        }

        /// <summary>
        /// Gets the size of the container based on the size of the box collider.
        /// </summary>
        public virtual Vector3 Size => boxCollider.size;

        /// <summary>
        /// A list of possible product positions within the container.
        /// </summary>
        protected List<Vector3> productPositions = new List<Vector3>();

        /// <summary>
        /// A list of product models currently placed within the container.
        /// </summary>
        protected List<GameObject> productModels = new List<GameObject>();

        /// <summary>
        /// Gets the maximum capacity of the container (number of possible product positions).
        /// </summary>
        public int Capacity => productPositions.Count;

        /// <summary>
        /// Gets the current number of products in the container.
        /// </summary>
        public int Quantity => productModels.Count;

        public bool IsFull => Quantity >= Capacity;

        /// <summary>
        /// Initializes the container with the specified product.
        /// This method calculates and stores the possible product positions within the container.
        /// </summary>
        /// <param name="product">The product to store in the container.</param>
        public virtual void Initialize(Product product)
        {
            Product = product;

            Vector3Int fit = Product.FitOnContainer(Size);

            if (this is Box && product.OverrideBoxQuantity)
            {
                fit = product.BoxQuantity;
            }
            else if (this is Shelf && product.OverrideShelfQuantity)
            {
                fit = product.ShelfQuantity;
            }

            float cellWidth = Size.x / fit.x;
            float cellDepth = Size.z / fit.z;

            productPositions.Clear();

            for (int x = 0; x < fit.x; x++)
            {
                for (int y = 0; y < fit.y; y++)
                {
                    for (int z = 0; z < fit.z; z++)
                    {
                        Vector3 productPosition = new Vector3(
                            (x * cellWidth) + (cellWidth / 2) - (Size.x / 2),
                            (y * Product.Size.y),
                            (z * cellDepth) + (cellDepth / 2) - (Size.z / 2)
                        );

                        productPositions.Add(productPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Restores the specified number of products to the container.
        /// </summary>
        /// <param name="product">The product to restore.</param>
        /// <param name="quantity">The number of products to restore.</param>
        public virtual void RestoreProducts(Product product, int quantity)
        {
            Initialize(product);

            for (int i = 0; i < quantity; i++)
            {
                var productModel = Instantiate(product.Model, transform);
                productModel.transform.localPosition = productPositions[i];
                productModel.transform.localRotation = Quaternion.identity;
                productModels.Add(productModel);
            }
        }
    }
}
