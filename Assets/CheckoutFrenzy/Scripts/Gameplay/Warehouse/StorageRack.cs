using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class StorageRack : Furniture
    {
        public List<Rack> Racks { get; private set; }

        public Vector3 Front => transform.TransformPoint(Vector3.forward);

        protected override void Awake()
        {
            base.Awake();

            Racks = GetComponentsInChildren<Rack>(true).ToList();
        }

        protected override void Start()
        {
            base.Start();

            WarehouseManager.Instance.ValidateStorageRack(this);
        }

        protected override void SetMovingState(bool isMoving)
        {
            base.SetMovingState(isMoving);

            // Disable rack interaction during movement, and enable it when not moving.
            Racks.ForEach(rack => rack.ToggleInteraction(!isMoving));

            // If the storage rack is starting to move, unregister it from the warehouse manager.
            // This prevent employees from targeting it.
            if (isMoving) WarehouseManager.Instance.UnregisterStorageRack(this);
        }

        protected override void Place()
        {
            base.Place();

            WarehouseManager.Instance.ValidateStorageRack(this);
        }

        public void RestoreBoxesOnRacks(List<RackData> savedRacks)
        {
            for (int i = 0; i < savedRacks.Count; i++)
            {
                RackData rackData = savedRacks[i];

                // Skip this rack if it is empty.
                if (rackData.IsEmpty) continue;

                // Retrieve the Product from the DataManager using the saved ID.
                Product product = DataManager.Instance.GetProductById(rackData.ProductID);

                // Get the corresponding Shelf and restore it's products.
                Rack rack = Racks[i];
                rack.RestoreBoxes(product, rackData.Quantities);
            }
        }
    }
}
