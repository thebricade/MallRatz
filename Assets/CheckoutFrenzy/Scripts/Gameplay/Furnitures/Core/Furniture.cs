using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Localization;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class Furniture : Interactable, IPurchasable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

        [Header("Messages")]
        [SerializeField, Tooltip("The localized text shown when the object cannot be placed at the selected position.")]
        private LocalizedString invalidPlacementMessage;

        [Header("Identification")]
        [SerializeField, Tooltip("Unique identifier for this furniture.")]
        private int furnitureId;

        [SerializeField, Tooltip("Name of the furniture.")]
        private new string name;

        [Header("Visuals")]
        [SerializeField, Tooltip("Icon representing the furniture.")]
        private Sprite icon;

        [SerializeField, Tooltip("Mesh Renderer of the furniture.")]
        private MeshRenderer mainRenderer;

        [SerializeField, Tooltip("Material used when the furniture is being moved.")]
        private Material movingMaterial;

        [Header("Economy")]
        [SerializeField, Tooltip("Price of the furniture in cents.")]
        private int priceInCents;

        [SerializeField, Tooltip("Time in seconds it takes to order this furniture.")]
        private int orderTime = 5;

        [Header("Configuration")]
        [SerializeField, Tooltip("The type of product section (e.g., General, Shelf, Fridge) that this furniture is configured to display.")]
        private DisplaySection section;

        public int FurnitureID => furnitureId;

        // IPurchasable Properties
        public string Name => name;
        public Sprite Icon => icon;
        public decimal Price => priceInCents / 100m;
        public int OrderTime => orderTime;
        public DisplaySection Section => section;

        public bool IsMoving { get; private set; }

        private enum Direction { North, East, South, West }
        private Direction currentDirection;

        protected IInteractor interactor;
        private BoxCollider col;
        private Material defaultMaterial;

        private List<Collider> others = new List<Collider>();

        protected override void Awake()
        {
            base.Awake();

            col = GetComponent<BoxCollider>();
            defaultMaterial = mainRenderer.material;
            currentDirection = GetFacingDirection();
        }

        protected virtual void Start()
        {
            // Subscribe to the OnSave event to save this furniture's data.
            DataManager.Instance.OnSave += HandleOnSave;
        }

        private void HandleOnSave()
        {
            // Create a new FurnitureData object from this furniture's properties.
            List<ShelfData> savedShelves = new();
            List<RackData> savedRacks = new();

            if (this is ShelvingUnit shelvingUnit)
            {
                foreach (var shelf in shelvingUnit.Shelves)
                {
                    var shelfData = new ShelfData(
                        shelf.Product != null ? shelf.Product.ProductID : 0,
                        shelf.AssignedProduct != null ? shelf.AssignedProduct.ProductID : 0,
                        shelf.Quantity
                    );

                    savedShelves.Add(shelfData);
                }
            }
            else if (this is StorageRack storageRack)
            {
                foreach (var rack in storageRack.Racks)
                {
                    savedRacks.Add(new RackData(
                        rack.Product != null ? rack.Product.ProductID : 0,
                        rack.GetProductQuantities()
                    ));
                }
            }

            var furnitureData = new FurnitureData()
            {
                FurnitureID = this.FurnitureID,
                Name = this.Name,
                Location = new Location(transform.position),
                Orientation = new Orientation(transform.rotation),
                LastMoved = interactor != null ? new Location(interactor.Position) : new Location(Vector3.zero),
                SavedShelves = savedShelves,
                SavedRacks = savedRacks
            };

            // Add the furniture data to the list of saved furniture.
            DataManager.Instance.Data.SavedFurnitures.Add(furnitureData);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        ///<summary>
        /// Adds a Rigidbody component to enable physics interactions for the furniture.
        /// 
        /// This allows the furniture to respond to gravity and collisions naturally.
        ///</summary>
        public void ActivatePhysics()
        {
            gameObject.AddComponent<Rigidbody>();
        }

        public override void Interact(IInteractor interactor)
        {
            this.interactor = interactor;

            StartCoroutine(Move());

            UIEvents.RaiseInteractMessage("");
        }

        private IEnumerator Move()
        {
            SetMovingState(true);
            interactor.StateManager.PushState(PlayerState.Moving);

            // Ensure furniture is oriented correctly on first move to prevent possible disorientation.
            if (currentDirection == Direction.North) transform.DORotate(Vector3.zero, 0.5f);

            while (IsMoving)
            {
                transform.position = interactor.GetFrontPosition();

                yield return null;
            }
        }

        protected virtual void SetMovingState(bool isMoving)
        {
            IsMoving = isMoving;

            if (isMoving)
            {
                // If starting to move:
                // Add a Rigidbody component if one doesn't already exist.
                Rigidbody body = GetComponent<Rigidbody>();
                if (body == null) body = gameObject.AddComponent<Rigidbody>();

                // Set the Rigidbody to kinematic so it's controlled by transform.
                body.isKinematic = true;

                // Set the collider to trigger mode to allow overlapping with other objects.
                col.isTrigger = true;

                // Switch to the moving material.
                mainRenderer.material = movingMaterial;

                // Enable the Place, Rotate, and Pack buttons in the UI.
                UIEvents.RaiseActionUI(ActionType.Place, true, Place);
                UIEvents.RaiseActionUI(ActionType.Rotate, true, Rotate);
                UIEvents.RaiseActionUI(ActionType.Pack, true, Pack);

                // If this furniture has doors, disable Open and Close buttons in the UI (e.g., Fridge, Freezer)
                UIEvents.RaiseActionUI(ActionType.Open, false, null);
                UIEvents.RaiseActionUI(ActionType.Close, false, null);
            }
            else
            {
                // If stopping movement:
                // Destroy the Rigidbody component if one exists.
                if (TryGetComponent<Rigidbody>(out Rigidbody body)) Destroy(body);

                // Set the collider back to non-trigger mode.
                col.isTrigger = false;

                // Switch back to the default material.
                mainRenderer.material = defaultMaterial;

                // Disable the Place, Rotate, and Pack buttons in the UI.
                UIEvents.RaiseActionUI(ActionType.Place, false, null);
                UIEvents.RaiseActionUI(ActionType.Rotate, false, null);
                UIEvents.RaiseActionUI(ActionType.Pack, false, null);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ShouldIgnoreTrigger(other)) return;

            others.Add(other); // Add "other" to the list of overlapping objects.
            ToggleColor();
        }

        private void OnTriggerExit(Collider other)
        {
            if (ShouldIgnoreTrigger(other)) return;

            others.Remove(other); // Remove "other" from the list of overlapping objects.
            ToggleColor();
        }

        // Checks if the current trigger event should be ignored.
        private bool ShouldIgnoreTrigger(Collider other)
        {
            // Ignore triggers if not moving or if the object is on the Ground Layer.
            return !IsMoving || GameConfig.Instance.GroundLayer.Contains(other);
        }

        private void ToggleColor()
        {
            var color = new Color(0f, 1f, 0f, 0.5f);
            if (others.Count > 0) color = new Color(1f, 0f, 0f, 0.5f);

            int propertyId = mainRenderer.material.HasProperty(BaseColorId) ? BaseColorId : LegacyColorId;
            mainRenderer.material.SetColor(propertyId, color);
        }

        protected virtual void Place()
        {
            // If there are any overlapping objects, the furniture cannot be placed.
            if (others.Count > 0)
            {
                UIEvents.RaiseMessage(invalidPlacementMessage.GetLocalizedString(), Color.red);
                return;
            }

            SetMovingState(false);
            interactor.StateManager.PopState();
            interactor = null;

            // Update the NavMesh surface so Customer AI can pathfind around the placed furniture correctly.
            StoreManager.Instance.UpdateNavMeshSurface();
        }

        private void Rotate()
        {
            // Calculate the next rotation direction.
            currentDirection = (Direction)(((int)currentDirection + 1) % 4); // Cycle through the Direction enum values.

            // Calculate the target rotation.
            Vector3 targetRotation = Vector3.up * (int)currentDirection * 90f; // 90-degree increments.

            // Rotate the furniture using a smooth animation.
            transform.DORotate(targetRotation, 0.5f);
        }

        private void Pack()
        {
            interactor.StateManager.PopState();

            Transform holdPoint = interactor.HoldPoint;

            var furnitureBox = Instantiate(
                StoreManager.Instance.FurnitureBoxPrefab,
                holdPoint.position,
                holdPoint.rotation
            );

            furnitureBox.furnitureId = furnitureId;
            furnitureBox.Interact(interactor);

            UIEvents.RaiseActionUI(ActionType.Place, false, null);
            UIEvents.RaiseActionUI(ActionType.Rotate, false, null);
            UIEvents.RaiseActionUI(ActionType.Pack, false, null);

            DOTween.Kill(transform);

            Destroy(gameObject);
        }

        private Direction GetFacingDirection()
        {
            Vector3 rotation = transform.eulerAngles;

            if (Mathf.Approximately(rotation.y, 0f) || rotation.y < 45f || rotation.y > 315f)
            {
                return Direction.North;
            }
            else if (Mathf.Approximately(rotation.y, 90f) || (rotation.y > 45f && rotation.y < 135f))
            {
                return Direction.East;
            }
            else if (Mathf.Approximately(rotation.y, 180f) || (rotation.y > 135f && rotation.y < 225f))
            {
                return Direction.South;
            }
            else if (Mathf.Approximately(rotation.y, 270f) || (rotation.y > 225f && rotation.y < 315f))
            {
                return Direction.West;
            }

            return Direction.North;
        }
    }
}
