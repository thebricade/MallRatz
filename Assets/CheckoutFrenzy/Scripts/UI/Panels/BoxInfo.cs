using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class BoxInfo : UIPanel
    {
        [Header("Component References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private BoxInfoRow rowPrefab;

        private List<BoxInfoRow> spawnedRows = new List<BoxInfoRow>();
        private Sprite emptyIcon;

        protected override void Awake()
        {
            base.Awake();
            emptyIcon = iconImage.sprite; // Store the default (empty box) icon.
            HideUI();
        }

        private void OnEnable()
        {
            StoreEvents.OnBoxSelected += HandleBoxSelected;
        }

        private void OnDisable()
        {
            StoreEvents.OnBoxSelected -= HandleBoxSelected;
        }

        private void HandleBoxSelected(IBox box)
        {
            if (box != null)
            {
                ShowUI();
                UpdateInfo(box);
            }
            else
            {
                HideUI();
            }
        }

        /// <summary>
        /// Updates the displayed information based on the provided box.
        /// </summary>
        /// <param name="box">The Box object to display information for.</param>
        private void UpdateInfo(IBox box)
        {
            iconImage.sprite = box.GetIcon() ?? emptyIcon;

            // 1. Clear old rows
            foreach (var row in spawnedRows) Destroy(row.gameObject);
            spawnedRows.Clear();

            // 2. Spawn new rows
            foreach (var detail in box.GetDetails())
            {
                var row = Instantiate(rowPrefab, contentContainer);
                row.Setup(detail.LabelKey, detail.Value);
                spawnedRows.Add(row);
            }
        }
    }
}
