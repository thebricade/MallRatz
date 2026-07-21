using System.Collections;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Security : Employee
    {
        private const float WALK_SPEED = 1.5f;
        private const float RUN_SPEED = 2.8f;

        public override EmployeeType Type => EmployeeType.Security;

        protected override Box targetBox { get => null; set { } } // Unused

        private Guard guard;
        private Customer thief;

        protected override void Awake()
        {
            base.Awake();

            guard = GetComponentInChildren<Guard>();
            if (guard == null)
            {
                Debug.LogWarning("No Guard component found on Security.");
            }
        }

        protected override IEnumerator Work()
        {
            while (true)
            {
                yield return ChaseThief();
                yield return Rest();
            }
        }

        protected override void LocateTargets()
        {
            if (thief == null)
            {
                thief = StoreManager.Instance.GetNearestThief(transform.position);
            }
        }

        protected override bool IsTargetsLocated()
        {
            return thief != null;
        }

        private IEnumerator ChaseThief()
        {
            if (thief == null) yield break;

            animator.SetFloat("Speed", 1f);
            agent.speed = RUN_SPEED;

            float repathRate = 0.1f;
            float nextRepathTime = 0f;

            while (thief != null && !thief.IsCaught)
            {
                float distanceToThief = Vector3.Distance(transform.position, thief.transform.position);

                if (distanceToThief <= guard.HitDistance)
                {
                    agent.isStopped = true;
                    animator.SetBool("IsMoving", false);
                    animator.SetFloat("Speed", 0f);
                    agent.speed = WALK_SPEED;

                    Vector3 direction = (thief.transform.position - transform.position).Flatten().normalized;
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }

                    guard.TryHit(thief);

                    yield return new WaitForSeconds(3f);
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;

                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("Speed", 1f);
                    agent.speed = RUN_SPEED;

                    if (Time.time >= nextRepathTime)
                    {
                        agent.SetDestination(thief.transform.position);
                        nextRepathTime = Time.time + repathRate;
                    }
                }

                yield return null;
            }

            thief = null;
            if (agent.isOnNavMesh) agent.isStopped = false;
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);
            agent.speed = WALK_SPEED;
        }
    }
}
