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
        [Tooltip("\"Hold onto this for me.\" A random Friend starts with the card, and she has to get it back.")]
        public bool cardStartsWithFriends = true;
        [Tooltip("When the mall opens, the Spender counts to ten with her eyes shut while the Friends hide the card.")]
        [Min(0)] public float spenderCountdownSeconds = 10f;
        [Tooltip("Her own cash, for capsule toys and small things while the Friends have her card.")]
        [Min(0)] public int spenderPocketCash = 15;
        public List<ItemDefinition> wishList = new();
    }
}
