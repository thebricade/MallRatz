using MallRatz.Mall;
using Unity.Netcode;
using UnityEngine;

namespace MallRatz.Items
{
    // A capsule toy from the gacha machine, themed to one of the shops: a mini inflatable shrimp, a holo
    // card keychain. Waved near a Friend whose weakness it matches, it wears their willpower down (and
    // gives that weakness away), and "treat yourself" with it hits much harder and hands it over.
    // Unpulled ones wait out of play inside the machine; used ones go back in.
    public class Trinket : Holdable
    {
        readonly NetworkVariable<bool> inPlay = new();
        readonly NetworkVariable<int> theme = new(-1);

        MaterialPropertyBlock block;

        public bool InPlay => inPlay.Value;
        // Index into ShopZone.ByName, the same space as a Friend's weakness.
        public int Theme => theme.Value;
        public ShopZone Shop => theme.Value >= 0 && theme.Value < ShopZone.ByName.Count ? ShopZone.ByName[theme.Value] : null;
        public string DisplayName => Shop != null ? Shop.TrinketName : "capsule toy";
        protected override bool Present => inPlay.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            inPlay.OnValueChanged += OnInPlayChanged;
            theme.OnValueChanged += OnThemeChanged;
            Apply();
            Tint();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            inPlay.OnValueChanged -= OnInPlayChanged;
            theme.OnValueChanged -= OnThemeChanged;
        }

        void OnInPlayChanged(bool previous, bool current) => Apply();
        void OnThemeChanged(int previous, int current) => Tint();

        void Tint()
        {
            if (Shop == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_Color", Shop.TrinketColor);
                block.SetColor("_BaseColor", Shop.TrinketColor);
                r.SetPropertyBlock(block);
            }
        }

        internal void ServerDispense(int newTheme)
        {
            theme.Value = newTheme;
            inPlay.Value = true;
            Apply();
            Tint();
        }

        // Used up or round over: back inside the machine.
        public override void ServerReturnHome()
        {
            base.ServerReturnHome();
            inPlay.Value = false;
            Apply();
        }
    }
}
