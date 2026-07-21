using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class LabelCustomizer : UIPanel
    {
        [SerializeField, Tooltip("RectTransform of the main label customizer panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Button to remove the label on the current shelf.")]
        private Button removeButton;

        [SerializeField, Tooltip("The container where product UI elements will be instantiated.")]
        private RectTransform contentRect;

        [SerializeField, Tooltip("GameObject shown when there are no products available in the warehouse for this section.")]
        private GameObject emptyNotif;

        [SerializeField, Tooltip("The prefab used to represent an individual product in the customizer list.")]
        private StorageProductUI productUIPrefab;

        private List<StorageProductUI> productUIs = new List<StorageProductUI>();

        protected override void Awake()
        {
            base.Awake();
            mainPanel.anchoredPosition = Vector2.zero;
            HideUI();
        }

        private void OnEnable() => StoreEvents.OnLabelCustomizerRequested += HandleLabelCustomizerRequested;
        private void OnDisable() => StoreEvents.OnLabelCustomizerRequested -= HandleLabelCustomizerRequested;

        private void HandleLabelCustomizerRequested(ILabelable shelf)
        {
            ShowUI();

            // Toggle the return button and set its action to close the label customizer.
            UIEvents.RaiseActionUI(ActionType.Return, true, () =>
            {
                UIEvents.RaiseActionUI(ActionType.Return, false, null);
                HideUI();
            });

            UIEvents.RaiseInteractionAvailable(false);

            productUIs.ForEach(ui => Destroy(ui.gameObject));
            productUIs.Clear();

            var products = WarehouseManager.Instance.GetProducts(shelf.DisplaySection);

            if (products.Count == 0)
            {
                emptyNotif.SetActive(true);
                return;
            }

            emptyNotif.SetActive(false);

            foreach (var kvp in products)
            {
                var productUI = Instantiate(productUIPrefab, contentRect, false);

                productUI.Initialize(shelf, kvp.Key, kvp.Value, onClick: (clickedUI) =>
                {
                    productUIs.ForEach(ui => ui.SetInteractable(ui != clickedUI));
                });

                productUI.SetInteractable(shelf.AssignedProduct != kvp.Key);

                productUIs.Add(productUI);
            }

            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() =>
            {
                shelf.SetLabel(null);
                productUIs.ForEach(ui => ui.SetInteractable(true));
            });
        }
    }
}
