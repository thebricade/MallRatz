using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class Rack : MonoBehaviour, IEmployeeTarget
    {
        [SerializeField] private float stackOffset = 0.01f;

        [Header("Stock Label")]
        [SerializeField] private GameObject label;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private TextMeshPro amountText;

        public Product Product { get; private set; }

        private StorageRack storageRack;
        public StorageRack StorageRack
        {
            get
            {
                if (storageRack == null) storageRack = GetComponentInParent<StorageRack>();
                return storageRack;
            }
        }

        public int BoxQuantity => boxes.Count;
        public int ProductQuantity => GetProductQuantities().Sum();
        public bool IsFull => BoxQuantity >= boxCapacity;

        public bool IsTargeted { get; set; }

        private Stack<Box> boxes = new Stack<Box>();

        private BoxCollider boxCollider;

        private float stackHeight;
        private int boxCapacity;

        private void Awake()
        {
            gameObject.layer = GameConfig.Instance.RackLayer.ToSingleLayer();

            boxCollider = GetComponent<BoxCollider>();

            UpdateLabel();
        }

        public void Initialize(Product product)
        {
            Product = product;

            stackHeight = product.Box.GetComponent<Box>().Size.y;
            boxCapacity = Mathf.FloorToInt(boxCollider.size.y / stackHeight);
        }

        private void UpdateLabel()
        {
            if (Product != null)
            {
                label.SetActive(true);
                iconRenderer.sprite = Product.Icon;
                amountText.text = GetProductQuantities().Sum().ToString();
            }
            else
            {
                label.SetActive(false);
            }
        }

        /// <summary>
        /// Enables or disables interaction with the rack collider.
        /// </summary>
        /// <param name="enabled">True to enable interaction, false to disable.</param>
        public void ToggleInteraction(bool enabled) => boxCollider.enabled = enabled;

        public List<int> GetProductQuantities()
        {
            List<int> quantities = new List<int>();

            foreach (var box in boxes)
            {
                quantities.Add(box.Quantity);
            }

            // Reverse the order because boxes is a Stack<T>, which follows LIFO (Last In, First Out)
            quantities.Reverse();

            return quantities;
        }

        public bool CanStoreBox(Box box, out Vector3 boxPosition, bool isPlayer)
        {
            boxPosition = Vector3.zero;

            if (BoxQuantity < boxCapacity)
            {
                Vector3 offset = Vector3.up * stackOffset;
                boxPosition = offset + (Vector3.up * BoxQuantity * stackHeight);
                boxes.Push(box);
                UpdateLabel();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RetrieveBox(PlayerController player)
        {
            if (boxes.Count == 0) return;

            var box = boxes.Pop();
            box.IsStored = false;
            player.SetInteractable(box);

            if (boxes.Count == 0)
            {
                Product = null;
            }

            UpdateLabel();
        }

        public Box RetrieveBox()
        {
            if (boxes.Count == 0) return null;

            var box = boxes.Pop();
            box.IsStored = false;

            if (boxes.Count == 0) Product = null;

            UpdateLabel();

            return box;
        }

        public void RestoreBoxes(Product product, List<int> quantities)
        {
            Initialize(product);

            foreach (var qty in quantities)
            {
                var boxObj = Instantiate(product.Box, transform);
                boxObj.name = product.Box.name;
                var box = boxObj.GetComponent<Box>();

                if (qty > 0) box.RestoreProducts(product, qty);

                if (CanStoreBox(box, out Vector3 boxPosition, false))
                {
                    box.transform.localPosition = boxPosition;
                    box.transform.localRotation = Quaternion.identity;

                    box.IsStored = true;
                    box.SetActivePhysics(false);
                }
            }
        }
    }
}
