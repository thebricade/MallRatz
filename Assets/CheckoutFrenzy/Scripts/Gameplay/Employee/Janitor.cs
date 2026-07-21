using System.Collections;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Janitor : Employee
    {
        public override EmployeeType Type => EmployeeType.Janitor;

        private Box emptyBox;
        private TrashCanLid trashCan;

        protected override Box targetBox
        {
            get => emptyBox;
            set => emptyBox = value;
        }

        private Cleaner cleaner;
        private Cleanable cleanable;

        protected override void Awake()
        {
            base.Awake();

            cleaner = GetComponentInChildren<Cleaner>();
            if (cleaner == null)
            {
                Debug.LogWarning("No Cleaner component found on Janitor.");
            }
        }

        protected override IEnumerator Work()
        {
            while (true)
            {
                yield return GoToAndCleanCleanable();

                yield return FindEmptyBox();
                yield return DisposeEmptyBox();

                yield return Rest();
            }
        }

        protected override void LocateTargets()
        {
            if (emptyBox == null)
            {
                emptyBox = FindObjectsByType<Box>(FindObjectsSortMode.None)
                    .FirstOrDefault(box =>
                        box.Quantity == 0 &&
                        box.transform.parent == null &&
                        !box.IsCheckingCollision);
            }

            if (trashCan == null)
            {
                trashCan = FindFirstObjectByType<TrashCanLid>();
            }

            if (cleanable == null)
            {
                cleanable = StoreManager.Instance.GetNearestCleanable(transform.position);
            }
        }

        protected override bool IsTargetsLocated()
        {
            return (emptyBox != null && trashCan != null) || cleanable != null;
        }

        private IEnumerator FindEmptyBox()
        {
            if (emptyBox == null) yield break;

            Vector3 direction = (emptyBox.transform.position - transform.position).normalized;
            Vector3 targetPos = emptyBox.transform.position - direction;
            agent.SetDestination(targetPos);

            while (!HasArrived())
            {
                if (emptyBox == null || emptyBox.transform.parent != null)
                {
                    agent.SetDestination(transform.position);
                    emptyBox = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(emptyBox.transform);

            if (emptyBox == null || emptyBox.transform.parent != null)
            {
                emptyBox = null;
                yield break;
            }

            yield return PickupBox();
        }

        private IEnumerator DisposeEmptyBox()
        {
            if (emptyBox == null) yield break;

            if (trashCan == null)
            {
                DropBox();
                yield break;
            }

            agent.SetDestination(trashCan.transform.position);

            while (!HasArrived())
            {
                if (trashCan == null || trashCan.Furniture.IsMoving)
                {
                    agent.SetDestination(transform.position);
                    DropBox();
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(trashCan.transform);

            SetIKWeight(0f);

            trashCan.Open(playAudio: false);

            float disposeDuration = 0.5f;

            emptyBox.transform.DOMove(trashCan.transform.position + Vector3.up, disposeDuration);
            emptyBox.transform.DOScale(Vector3.zero, disposeDuration);

            yield return new WaitForSeconds(disposeDuration);

            Destroy(emptyBox.gameObject);
            emptyBox = null;

            yield return new WaitForEndOfFrame();
        }

        private IEnumerator GoToAndCleanCleanable()
        {
            if (cleanable == null) yield break;

            Vector3 direction = (cleanable.transform.position - transform.position).normalized.Flatten();
            Vector3 targetPos = cleanable.transform.position - direction * cleaner.GetStoppingDistanceForTool(cleanable.ToolType);

            agent.SetDestination(targetPos);

            while (!HasArrived())
            {
                if (cleanable == null || cleanable.HasCleaner)
                {
                    agent.SetDestination(transform.position);
                    cleanable = null;
                    yield break;
                }

                yield return null;
            }

            yield return LookAt(cleanable.transform);

            if (cleanable == null || cleanable.HasCleaner) yield break;

            cleanable.StartCleaningStep(cleaner);

            yield return new WaitUntil(() => cleanable == null);
        }
    }
}
