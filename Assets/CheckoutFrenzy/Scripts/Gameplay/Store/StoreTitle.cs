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
        }

        private void Start()
        {
            Text = DataManager.Instance.Data.StoreName;

            if (ColorUtility.TryParseHtmlString(DataManager.Instance.Data.NameColor, out var nameColor))
            {
                Color = nameColor;
            }

            DataManager.Instance.OnSave += () =>
            {
                DataManager.Instance.Data.StoreName = Text;

                var nameColor = "#" + ColorUtility.ToHtmlStringRGB(Color);
                DataManager.Instance.Data.NameColor = nameColor;
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
