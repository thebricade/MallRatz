using UnityEngine;
using Cinemachine;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(TextMeshPro))]
    public class StoreTitle : Interactable, ITextReceiver
    {
        [SerializeField, Tooltip("Virtual camera to focus on the store name (on the roof) during renaming.")]
        private CinemachineVirtualCamera roofCamera;

        private TextMeshPro tmp;

        // StoreTitle is a fixed part of a shop's hierarchy (on the roof), so a cached hierarchy
        // lookup is sufficient here.
        private StoreManager storeManager;

        public string Text
        {
            get => tmp.text;
            set => tmp.text = value;
        }

        public Color Color
        {
            get => tmp.color;
            set => tmp.color = value;
        }

        protected override void Awake()
        {
            base.Awake();
            tmp = GetComponent<TextMeshPro>();
            storeManager = GetComponentInParent<StoreManager>();
        }

        private void Start()
        {
            if (storeManager == null) return;

            Text = storeManager.DataManager.Data.StoreName;

            if (ColorUtility.TryParseHtmlString(storeManager.DataManager.Data.NameColor, out var nameColor))
            {
                Color = nameColor;
            }

            storeManager.DataManager.OnSave += () =>
            {
                storeManager.DataManager.Data.StoreName = Text;

                var nameColor = "#" + ColorUtility.ToHtmlStringRGB(Color);
                storeManager.DataManager.Data.NameColor = nameColor;
            };
        }

        public override void Interact(IInteractor interactor)
        {
            StoreEvents.RaiseStoreNameChangeRequested(
                this,
                () => roofCamera.gameObject.SetActive(false)
            );

            if (roofCamera != null)
            {
                roofCamera.gameObject.SetActive(true);
            }

            UIEvents.RaiseInteractMessage("");
        }
    }
}
