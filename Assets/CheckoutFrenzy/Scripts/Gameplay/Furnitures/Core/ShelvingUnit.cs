using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class ShelvingUnit : Furniture
    {
        // Point in front of the shelving unit where customers stand to pick products.
        public Vector3 Front => transform.TransformPoint(Vector3.forward);

        // All child shelves of this shelving unit.
        public List<Shelf> Shelves { get; private set; } = new List<Shelf>();

        // Indicates whether the door is open for units with doors (e.g., Fridges, Freezers).
        public virtual bool IsOpen { get; protected set; } = true;

        protected override void Awake()
        {
            base.Awake();

            // Get all child Shelf components (including inactive ones) and store them in the Shelves list.
            Shelves = GetComponentsInChildren<Shelf>(true).ToList();

            // Set the ShelvingUnit property of each child Shelf to this ShelvingUnit.
            Shelves.ForEach(shelf => shelf.ShelvingUnit = this);
        }

        protected override void Start()
        {
            base.Start();

            StoreManager.Instance.ValidateShelvingUnit(this);
        }

        public virtual void Open(bool forced, bool playSFX) { }

        public virtual void Close(bool forced, bool playSFX) { }

        protected override void SetMovingState(bool isMoving)
        {
            base.SetMovingState(isMoving);

            // Disable shelf interaction during movement, and enable it when not moving.
            Shelves.ForEach(shelf => shelf.ToggleInteraction(!isMoving));

            // If the shelving unit is starting to move, unregister it from the store manager.
            // This prevent other customers from targeting it.
            if (isMoving) StoreManager.Instance.UnregisterShelvingUnit(this);
        }

        protected override void Place()
        {
            base.Place();

            StoreManager.Instance.ValidateShelvingUnit(this);
        }

        /// <summary>
        /// Returns a random child Shelf that has a Product assigned to it.
        /// </summary>
        /// <returns>A random Shelf with a Product, or null if no shelves have products.</returns>
        public Shelf GetShelf()
        {
            var validShelves = Shelves.Where(shelf => shelf.Product != null).ToList();

            if (validShelves.Count == 0) return null;

            return validShelves[Random.Range(0, validShelves.Count)];
        }

        /// <summary>
        /// Restores the products on the shelves based on saved shelf data.
        /// </summary>
        /// <param name="savedShelves">A list of ShelfData objects containing the saved product information.</param>
        public void RestoreProductsOnShelves(List<ShelfData> savedShelves)
        {
            for (int i = 0; i < savedShelves.Count; i++)
            {
                Shelf shelf = Shelves[i];
                ShelfData shelfData = savedShelves[i];

                Product assignedProduct = DataManager.Instance.GetProductById(shelfData.AssignedProductID);
                shelf.SetLabel(assignedProduct);

                // Skip this shelf if it is empty.
                if (shelfData.IsEmpty) continue;

                // Retrieve the Product from the DataManager using the saved ID.
                Product product = DataManager.Instance.GetProductById(shelfData.ProductID);

                // Restore shelf's products.
                shelf.RestoreProducts(product, shelfData.Quantity);
            }
        }
    }
}
