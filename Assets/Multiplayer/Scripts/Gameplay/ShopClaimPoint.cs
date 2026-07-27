using FishNet.Object;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace MallRatz.Multiplayer.Gameplay
{
    /// <summary>
    /// Placed at a shop plot's entrance. Lets a player walk up and interact to claim the
    /// plot for themselves if it is not already owned by someone else.
    /// </summary>
    public class ShopClaimPoint : Interactable
    {
        private const string ClaimedByOtherMessage = "This shop already belongs to another player.";
        private const string ClaimedByLocalMessage = "This is your shop.";
        private const string ClaimPromptMessage = "Interact to claim this shop.";

        [SerializeField, Tooltip("The plot's ownership tracker. Defaults to the ShopIdentity found on a parent object.")]
        private ShopIdentity shopIdentity;

        protected override void Awake()
        {
            base.Awake();

            if (shopIdentity == null) shopIdentity = GetComponentInParent<ShopIdentity>();
        }

        public override void OnFocused()
        {
            if (shopIdentity != null && shopIdentity.IsClaimed)
            {
                var message = shopIdentity.IsOwnedByLocalClient ? ClaimedByLocalMessage : ClaimedByOtherMessage;
                UIEvents.RaiseInteractMessage(message);
                return;
            }

            UIEvents.RaiseInteractMessage(ClaimPromptMessage);
        }

        public override void Interact(IInteractor interactor)
        {
            if (shopIdentity == null || shopIdentity.IsClaimed) return;

            var networkObject = interactor.GetInteractorComponent<NetworkObject>();
            if (networkObject == null) return;

            shopIdentity.RequestClaim();
        }
    }
}
