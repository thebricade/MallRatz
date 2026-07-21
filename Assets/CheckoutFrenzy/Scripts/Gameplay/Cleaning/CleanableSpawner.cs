using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class CleanableSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Size of the area where Cleanables can randomly spawn (X = width, Y = depth).")]
        private Vector2 spawnArea = Vector2.one;

        [SerializeField, Tooltip("Time interval (in seconds) between spawn attempts.")]
        private float spawnInterval = 30f;

        [SerializeField, Range(0.1f, 1.0f), Tooltip("Probability that a Cleanable will spawn during each spawn attempt.")]
        private float chanceToSpawn = 0.6f;

        [SerializeField, Tooltip("Radius used to check if the spawn position is blocked by other objects.")]
        private float checkRadius = 1f;

        [SerializeField, Tooltip("List of Cleanable prefabs that can be randomly selected and spawned.")]
        private List<Cleanable> cleanablePrefabs;

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (StoreManager.Instance.CanSpawnCleanable && Random.value < chanceToSpawn)
                {
                    var cleanablePrefab = cleanablePrefabs[Random.Range(0, cleanablePrefabs.Count)];

                    Vector3 spawnPos = transform.position + new Vector3(
                        Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                        0f,
                        Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f)
                    );

                    bool isBlocked = Physics.CheckSphere(
                        spawnPos,
                        checkRadius,
                        ~GameConfig.Instance.GroundLayer,
                        QueryTriggerInteraction.Collide
                    );

                    if (!isBlocked)
                    {
                        Instantiate(cleanablePrefab, spawnPos, Quaternion.identity);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            Vector3 center = transform.position;
            Vector3 size = new Vector3(spawnArea.x, 0.1f, spawnArea.y);

            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
