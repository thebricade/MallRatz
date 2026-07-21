using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class WarehouseManager : MonoBehaviour
    {
        public static WarehouseManager Instance { get; private set; }

        [SerializeField, Tooltip("The 3D bounding box of the warehouse.")]
        private Bounds warehouseBounds;

        // List of valid storage racks within the warehouse's boundaries.
        private HashSet<StorageRack> storageRacks = new HashSet<StorageRack>();
        public void RegisterStorageRack(StorageRack storageRack) => storageRacks.Add(storageRack);
        public void UnregisterStorageRack(StorageRack storageRack) => storageRacks.Remove(storageRack);

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Validates the position of a StorageRack. 
        /// 
        /// If the StorageRack is within the warehouse bounds, it is registered;
        /// otherwise, it is unregistered.
        /// </summary>
        /// <param name="storageRack">The StorageRack to validate.</param>
        public void ValidateStorageRack(StorageRack storageRack)
        {
            Vector3 position = storageRack.transform.position;

            if (IsWithinWarehouse(position)) RegisterStorageRack(storageRack);
            else UnregisterStorageRack(storageRack);
        }

        /// <summary>
        /// Checks if the given position is within the bounds of the warehouse.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <returns>True if the position is within the warehouse bounds, otherwise false.</returns>
        public bool IsWithinWarehouse(Vector3 position)
        {
            return warehouseBounds.Contains(position);
        }

        public Dictionary<Product, int> GetProducts(DisplaySection section)
        {
            return storageRacks
                .SelectMany(storageRack => storageRack.Racks)
                .Where(rack => rack.Product != null && rack.Product.Section == section)
                .GroupBy(rack => rack.Product)
                .ToDictionary(group => group.Key, group => group.Sum(rack => rack.ProductQuantity));
        }

        public Rack GetAvailableRack(Product product)
        {
            return storageRacks
                .SelectMany(storageRack => storageRack.Racks)
                .FirstOrDefault(rack =>
                    !rack.IsTargeted &&
                    (rack.Product == null || (rack.Product == product && !rack.IsFull))
                );
        }

        public Rack GetRackWithProduct(Product product)
        {
            return storageRacks
                .SelectMany(storageRack => storageRack.Racks)
                .FirstOrDefault(rack => rack.Product == product);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(warehouseBounds.center, warehouseBounds.size);
        }
#endif
    }
}
