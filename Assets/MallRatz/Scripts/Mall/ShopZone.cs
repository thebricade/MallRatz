using System.Collections.Generic;
using UnityEngine;

namespace MallRatz.Mall
{
    // The inside of a shop. Outside every zone is the concourse, where Friends can't grab.
    [RequireComponent(typeof(BoxCollider))]
    public class ShopZone : MonoBehaviour
    {
        static readonly List<ShopZone> all = new();

        [SerializeField] string shopName = "Shop";

        BoxCollider zone;

        public string ShopName => shopName;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public static ShopZone At(Vector3 position)
        {
            foreach (var shop in all)
                if (shop.zone.bounds.Contains(position)) return shop;
            return null;
        }
    }
}
