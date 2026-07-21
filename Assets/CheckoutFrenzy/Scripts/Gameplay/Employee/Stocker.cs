using System.Collections;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Stocker : Employee
    {
        public override EmployeeType Type => EmployeeType.Stocker;

        private Box heldBox;
        private Shelf shelfToStock;
        private Rack sourceRack;

        protected override Box targetBox
        {
            get => heldBox;
            set => heldBox = value;
        }

        protected override IEnumerator Work()
        {
            while (true)
            {
                yield return RetrieveBox();
                yield return StockShelf();
                yield return Rest();
            }
        }

        protected override void LocateTargets()
        {
            if (shelfToStock == null)
            {
                shelfToStock = StoreManager.Instance.GetShelfToStock();
                if (shelfToStock != null) shelfToStock.IsTargeted = true;
            }

            if (sourceRack == null && shelfToStock != null)
            {
                sourceRack = WarehouseManager.Instance.GetRackWithProduct(shelfToStock.AssignedProduct);
            }
        }

        protected override bool IsTargetsLocated()
        {
            return shelfToStock != null && sourceRack != null;
        }

        private IEnumerator RetrieveBox()
        {
            if (sourceRack == null) yield break;

            var storageRack = sourceRack.StorageRack;

            if (storageRack == null) yield break;

            agent.SetDestination(storageRack.Front);

            while (!HasArrived())
            {
                if (sourceRack == null || storageRack?.IsMoving == true)
                {
                    agent.SetDestination(transform.position);
                    sourceRack = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(storageRack.transform);

            heldBox = sourceRack?.RetrieveBox();

            if (heldBox == null)
            {
                sourceRack = null;
                yield break;
            }

            yield return PickupBox();
        }

        private IEnumerator StockShelf()
        {
            if (heldBox == null || shelfToStock == null) yield break;

            var shelvingUnit = shelfToStock.ShelvingUnit;

            if (shelvingUnit == null) yield break;

            agent.SetDestination(shelvingUnit.Front);

            while (!HasArrived())
            {
                if (shelfToStock == null || shelvingUnit?.IsMoving == true)
                {
                    agent.SetDestination(transform.position);
                    StopStocking();
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(shelvingUnit.transform);

            yield return heldBox.OpenLidsSmooth();

            if (shelfToStock?.Product == null)
            {
                // Handle case where the player removed the product label while the stocker was on the way
                if (shelfToStock?.AssignedProduct == null)
                {
                    StopStocking();
                    yield break;
                }

                shelfToStock.Initialize(shelfToStock.AssignedProduct);
            }

            while (heldBox.Quantity > 0 && shelfToStock?.IsFull == false && shelvingUnit?.IsMoving == false)
            {
                heldBox.Stock(shelfToStock);
                yield return new WaitForSeconds(taskInterval);
            }

            StopStocking();
        }

        private void StopStocking()
        {
            DropBox();

            heldBox = null;

            if (shelfToStock != null)
            {
                shelfToStock.IsTargeted = false;
                shelfToStock = null;
            }

            sourceRack = null;
        }
    }
}
