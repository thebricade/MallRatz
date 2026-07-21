using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class Employee : MonoBehaviour
    {
        [SerializeField, Tooltip("Time in seconds between each employee action, such as performing tasks or waiting for the next job.")]
        protected float taskInterval = 0.5f;

        [Header("Listing Settings")]
        [SerializeField, Tooltip("Avatar sprite representing this employee.")]
        private Sprite avatar;

        [SerializeField, TextArea, Tooltip("A brief description of the employee job.")]
        private string description;

        [SerializeField, Tooltip("The hiring cost associated with this employee.")]
        private int cost;

        [SerializeField, Tooltip("Template used to generate salary bills for this employee.")]
        private BillTemplate salaryBill;

        [Header("Rig & IK Settings")]
        [SerializeField, Tooltip("Reference to rig's hips bone transform.")]
        protected Transform hipsBone;

        [SerializeField, Tooltip("Target for the left hand position in IK")]
        private Transform leftHandTarget;

        [SerializeField, Tooltip("Target for the right hand position in IK")]
        private Transform rightHandTarget;

        public abstract EmployeeType Type { get; }
        public Sprite Avatar => avatar;
        public int Cost => cost;
        public string Description => description;
        public BillTemplate SalaryBill => salaryBill;

        protected Animator animator;
        protected NavMeshAgent agent;

        protected abstract Box targetBox { get; set; }

        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private float ikWeight; // Weight of the IK for hand positioning

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            originalPosition = transform.position;
            originalRotation = transform.rotation;

            InitializeNavMeshAgent();

            if (hipsBone != null)
            {
                if (leftHandTarget != null) leftHandTarget.SetParent(hipsBone);
                if (rightHandTarget != null) rightHandTarget.SetParent(hipsBone);
            }
        }

        protected virtual void InitializeNavMeshAgent()
        {
            agent.speed = 1.5f;
            agent.angularSpeed = 3600f;
            agent.acceleration = 100f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        protected virtual void Start()
        {
            StartCoroutine(Work());
        }

        protected virtual IEnumerator Work()
        {
            yield return null;
        }

        protected IEnumerator Rest()
        {
            agent.SetDestination(originalPosition);

            while (!HasArrived())
            {
                LocateTargets();

                if (IsTargetsLocated()) yield break;

                yield return new WaitForSeconds(taskInterval);
            }

            if (Quaternion.Angle(transform.rotation, originalRotation) > 1f)
            {
                yield return transform.DORotateQuaternion(originalRotation, 0.5f).WaitForCompletion();
            }

            while (!IsTargetsLocated())
            {
                LocateTargets();

                yield return new WaitForSeconds(taskInterval);
            }
        }

        protected virtual void LocateTargets() { }

        protected virtual bool IsTargetsLocated() => true;

        protected void DropBox()
        {
            if (targetBox == null) return;

            targetBox.transform.SetParent(null);
            targetBox.SetActivePhysics(true);
            targetBox = null;
            SetIKWeight(0f);
        }

        protected IEnumerator PickupBox()
        {
            if (targetBox == null)
            {
                yield break;
            }

            targetBox.transform.SetParent(hipsBone);

            float pickDuration = 0.5f;

            SetIKWeight(1f);

            targetBox.transform.DOLocalRotate(
                new Vector3(15f, 0f, 0f), // Tilt the box slightly on X-axis
                pickDuration);

            targetBox.transform.DOLocalJump(
                new Vector3(0f, 0f, 0.43f), // Place the box slightly in front of hips
                0.5f, 1, pickDuration);

            // Delay to let Transform-based movement finish before disabling physics
            yield return new WaitForSeconds(0.2f);

            targetBox.SetActivePhysics(false);

            yield return new WaitForSeconds(pickDuration);
        }

        private void Update()
        {
            CheckDoors();
        }

        public void Dismiss()
        {
            StopAllCoroutines();
            DOTween.Kill(transform);
            DropBox();
            Destroy(gameObject);
        }

        protected IEnumerator LookAt(Transform target)
        {
            if (target == null) yield break;

            var lookDirection = (target.position - transform.position).Flatten().normalized;
            var lookRotation = Quaternion.LookRotation(lookDirection);
            yield return transform.DORotateQuaternion(lookRotation, 0.5f).WaitForCompletion();
        }

        protected bool HasArrived()
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                return true; // Prevents further checks if the agent is gone
            }

            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        animator.SetBool("IsMoving", false);
                        return true;
                    }
                }
            }

            animator.SetBool("IsMoving", true);
            return false;
        }

        protected virtual void CheckDoors()
        {
            Ray ray = new Ray(transform.position + Vector3.up, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 1f, GameConfig.Instance.InteractableLayer))
            {
                if (hit.transform.TryGetComponent<EntranceDoor>(out EntranceDoor entrance))
                {
                    entrance.OpenIfClosed();
                }
                else if (hit.transform.TryGetComponent<Door>(out Door door))
                {
                    door.OpenIfClosed();
                }
            }
        }

        protected void SetIKWeight(float targetWeight, float duration = 0.5f)
        {
            DOVirtual.Float(ikWeight, targetWeight, duration, value => ikWeight = value);
        }

        private void OnAnimatorIK()
        {
            // Set the IK position and rotation for the left hand
            if (leftHandTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, ikWeight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }

            // Set the IK position and rotation for the right hand
            if (rightHandTarget != null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, ikWeight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
            }
        }
    }
}
