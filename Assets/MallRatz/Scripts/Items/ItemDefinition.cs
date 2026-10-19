using UnityEngine;

namespace MallRatz.Items
{
    [CreateAssetMenu(menuName = "MallRatz/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string displayName = "Junk";
        [Min(0)] public int price = 10;
        [Tooltip("How dumb the item is. A giant inflatable shrimp beats a $200 blender.")]
        [Range(0, 10)] public int uselessness = 5;
        [Tooltip("Slows whoever carries it, held or bagged.")]
        [Min(0)] public float carryWeight = 1f;
        [Tooltip("Too big or heavy to run with while it's in your hands.")]
        public bool bulky;
    }
}
