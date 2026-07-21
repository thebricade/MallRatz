using System.Collections;
using System.Linq;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Sorter : Employee
    {
        public override EmployeeType Type => EmployeeType.Sorter;

        private Box filledBox;
        private Rack emptyRack;

        protected override Box targetBox
        {
            get => filledBox;
            set => filledBox = value;
        }

        protected override IEnumerator Work()
        {
            while (true)
            {
                yield return FindFilledBox();
                yield return StoreFilledBox();
                yield return Rest();
            }
        }

        protected override void LocateTargets()
        {
            if (filledBox == null)
            {
                filledBox = FindObjectsByType<Box>(FindObjectsSortMode.None)
                    .FirstOrDefault(box =>
                        box.Quantity > 0 &&
                        box.transform.parent == null &&
                        !WarehouseManager.Instance.IsWithinWarehouse(box.transform.position));
            }

            if (emptyRack == null && filledBox != null)
            {
                emptyRack = WarehouseManager.Instance.GetAvailableRack(filledBox.Product);
                if (emptyRack != null) emptyRack.IsTargeted = true;
            }
        }

        protected override bool IsTargetsLocated()
        {
            return filledBox != null && emptyRack != null;
        }

        private IEnumerator FindFilledBox()
        {
            if (filledBox == null) yield break;

            Vector3 direction = (filledBox.transform.position - transform.position).normalized;
            Vector3 targetPos = filledBox.transform.position - direction;
            agent.SetDestination(targetPos);

            while (!HasArrived())
            {
                if (filledBox == null || filledBox.transform.parent != null)
                {
                    agent.SetDestination(transform.position);
                    filledBox = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(filledBox.transform);

            if (filledBox == null || filledBox.transform.parent != null)
            {
                filledBox = null;
                yield break;
            }

            yield return PickupBox();
            filledBox.CloseIfOpened();
        }

        private IEnumerator StoreFilledBox()
        {
            if (filledBox == null) yield break;

            if (emptyRack == null)
            {
                DropBox();
                yield break;
            }

            agent.SetDestination(emptyRack.StorageRack.Front);

            while (!HasArrived())
            {
                if (emptyRack == null || emptyRack.StorageRack.IsMoving)
                {
                    agent.SetDestination(transform.position);
                    DropBox();
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(emptyRack.StorageRack.transform);

            SetIKWeight(0f);

            if (!filledBox.Store(emptyRack, false))
            {
                DropBox();
            }

            yield return new WaitForSeconds(0.5f);

            filledBox = null;

            if (emptyRack != null)
            {
                emptyRack.IsTargeted = false;
                emptyRack = null;
            }
        }
    }
}
