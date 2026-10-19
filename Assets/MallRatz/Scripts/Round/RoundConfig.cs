using System.Collections.Generic;
using MallRatz.Items;
using UnityEngine;

namespace MallRatz.Round
{
    [CreateAssetMenu(menuName = "MallRatz/Round Config", fileName = "RoundConfig")]
    public class RoundConfig : ScriptableObject
    {
        [Tooltip("\"The mall closes in 8 minutes.\"")]
        [Min(1)] public float roundSeconds = 480f;
        [Tooltip("The closing announcement plays this long before the end.")]
        [Min(0)] public float closingSeconds = 60f;
        [Tooltip("Share of the wish list's value the Spender has to buy to win.")]
        [Range(0, 1)] public float wishListTarget = 0.6f;
        public List<ItemDefinition> wishList = new();
    }
}
