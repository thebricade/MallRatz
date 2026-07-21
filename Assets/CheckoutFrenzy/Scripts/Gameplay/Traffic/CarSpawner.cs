using System.Collections;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class CarSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("An array of car prefabs to spawn.")]
        private Car[] carPrefabs;

        [SerializeField, Tooltip("The minimum time between car spawns.")]
        private float minSpawnTime = 3f;

        [SerializeField, Tooltip("The maximum time between car spawns.")]
        private float maxSpawnTime = 10f;

        [SerializeField, Tooltip("The distance the spawned cars will travel.")]
        private float travelDistance = 25f;

        private IEnumerator Start()
        {
            while (true)
            {
                float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
                yield return new WaitForSeconds(waitTime);

                int carIndex = Random.Range(0, carPrefabs.Length);
                var car = Instantiate(carPrefabs[carIndex], transform);
                car.Move(travelDistance);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Vector3 offset = new Vector3(0f, 0.2f, 0f);
            Vector3 start = transform.position + offset;
            Vector3 end = transform.forward * travelDistance + offset;
            Gizmos.DrawWireSphere(start, 0.25f);
            DrawArrow.ForGizmo(start, end, 1f);
        }
#endif
    }
}
