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
        [Tooltip("The capsule toy that baits a Friend whose weakness is this shop.")]
        [SerializeField] string trinketName = "keychain";
        [SerializeField] Color trinketColor = Color.white;

        BoxCollider zone;

        public string ShopName => shopName;
        public string TrinketName => trinketName;
        public Color TrinketColor => trinketColor;

        void Awake()
        {
            zone = GetComponent<BoxCollider>();
            zone.isTrigger = true;
        }

        // Sorted by name, so an index means the same shop on every machine.
        public static IReadOnlyList<ShopZone> ByName => all;

        void OnEnable()
        {
            all.Add(this);
            all.Sort((a, b) => string.CompareOrdinal(a.shopName, b.shopName));
        }

        void OnDisable() => all.Remove(this);

        public static ShopZone At(Vector3 position)
        {
            foreach (var shop in all)
                if (shop.zone.bounds.Contains(position)) return shop;
            return null;
        }
    }
}
