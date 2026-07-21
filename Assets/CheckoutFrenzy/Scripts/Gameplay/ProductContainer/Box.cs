using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Unity.AI.Navigation;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class Box : ProductContainer, IBox
    {
        [Header("Messages")]
        [SerializeField, Tooltip("Shown when the box cannot be thrown because something is blocking it.")]
        private LocalizedString throwBlockedMessage;

        [SerializeField, Tooltip("Shown when placing onto a shelf assigned to another product.")]
        private LocalizedString placeAssignedDifferentProductMessage;

        [SerializeField, Tooltip("Shown when product doesn't belong to the shelf section.")]
        private LocalizedString placeWrongSectionMessage;

        [SerializeField, Tooltip("Shown when shelf contains a different product.")]
        private LocalizedString placeDifferentProductMessage;

        [SerializeField, Tooltip("Shown when trying to place product but shelf is full.")]
        private LocalizedString placeShelfFullMessage;

        [SerializeField, Tooltip("Shown when trying to take product but box is full.")]
        private LocalizedString takeBoxFullMessage;

        [SerializeField, Tooltip("Shown when box already contains a different product.")]
        private LocalizedString takeDifferentProductMessage;

        [SerializeField, Tooltip("Shown when box size is incompatible with product.")]
        private LocalizedString takeIncompatibleSizeMessage;

        [SerializeField, Tooltip("Shown when storing to rack with different product.")]
        private LocalizedString storeDifferentProductMessage;

        [SerializeField, Tooltip("Shown when trying to store product but rack is full.")]
        private LocalizedString storeRackFullMessage;

        [Header("Box Lids")]
        [SerializeField, Tooltip("Reference to the bone transform of the front lid of the box.")]
        private Transform lidFront;

        [SerializeField, Tooltip("Reference to the bone transform of the back lid of the box.")]
        private Transform lidBack;

        [SerializeField, Tooltip("Reference to the bone transform of the left lid of the box.")]
        private Transform lidLeft;

        [SerializeField, Tooltip("Reference to the bone transform of the right lid of the box.")]
        private Transform lidRight;

        [Header("Sound Settings")]
        [SerializeField, Tooltip("Duration (in seconds) to check for collisions after throwing the box.")]
        private float collisionCheckDuration = 3f;

        /// <summary>
        /// Gets the size of the box, with each dimension floored to the nearest tenth.
        /// The base size is derived from the box collider. This flooring is crucial for accurate inner dimension
        /// calculations, preventing issues that could arise from slight inaccuracies in the collider's reported size.
        /// </summary>
        public override Vector3 Size => base.Size.FloorToTenth();

        public bool IsStored { get; set; }
        public bool IsOpen { get; private set; }
        public bool IsDisposable { get; private set; }
        public bool IsCheckingCollision { get; private set; }

        public string Name => gameObject.name;
        public Transform Transform => transform;

        private Rigidbody body;
        private IInteractor interactor;

        private Sequence lidSequence;

        private Coroutine disablePhysicsRoutine;

        protected override void Awake()
        {
            base.Awake();

            body = GetComponent<Rigidbody>();
            SetActivePhysics(false);

            // Prevents the box from affecting the navigation mesh
            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;
        }

        private IEnumerator Start()
        {
            DataManager.Instance.OnSave += HandleOnSave;

            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);

            if (!IsStored) SetActivePhysics(true);
        }

        private void OnDestroy()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnSave -= HandleOnSave;
            }
        }

        private void HandleOnSave()
        {
            if (IsStored) return;

            var boxData = new BoxData(this);
            DataManager.Instance.Data.SavedBoxes.Add(boxData);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Ignore collision events if the box is not currently moving.
            if (!IsCheckingCollision) return;

            // Check if the collision impact is significant.
            if (collision.relativeVelocity.magnitude > 2)
            {
                // Play an impact sound effect.
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        /// <summary>
        /// Handles the interaction with the box when the player taps interact button. 
        /// This includes picking up the box, attaching it to the player's hand, 
        /// updating the player's state, and enabling relevant UI elements.
        /// </summary>
        /// <param name="interactor">The interactor who is interacting with the box.</param>
        public override void Interact(IInteractor interactor)
        {
            // Store a reference to the interacting player.
            this.interactor = interactor;

            // Prevent the box from being disposed of (e.g., thrown to trash can) while it's being held by the player.
            IsDisposable = false;

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
            }

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            // Change the layer of all child objects to the "HeldObject" layer.
            // Making them rendered on top of everything else (except UI).
            foreach (Transform child in transform)
            {
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();
            }

            UIEvents.RaiseActionUI(ActionType.Throw, true, Throw);

            // Enable the appropriate button for opening/closing the box.
            if (IsOpen) UIEvents.RaiseActionUI(ActionType.Close, true, Close);
            else UIEvents.RaiseActionUI(ActionType.Open, true, Open);

            StoreEvents.RaiseBoxSelected(null);

            // Move the box to the player's hand.
            transform.SetParent(interactor.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);

            interactor.StateManager.PushState(PlayerState.Holding);

            UIEvents.RaiseInteractMessage("");
        }

        public override void OnFocused()
        {
            base.OnFocused();

            StoreEvents.RaiseBoxSelected(this);
        }

        public override void OnDefocused()
        {
            base.OnDefocused();

            StoreEvents.RaiseBoxSelected(null);
        }

        private void Throw()
        {
            // Check for collisions within the box's bounds. 
            // If there's an overlap, prevent the throw.
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var layerMask = ~GameConfig.Instance.PlayerLayer; // Create a layer mask that excludes the "Player" layer. 

            if (Physics.OverlapBox(center, extents, orientation, layerMask).Length > 0)
            {
                UIEvents.RaiseMessage(throwBlockedMessage.GetLocalizedString(), Color.red);
                return;
            }

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
                disablePhysicsRoutine = null;
            }

            DOTween.Kill(transform);

            // Detach the box from the player's hand.
            transform.SetParent(null);

            // Enable physics for the box and apply an impulse force.
            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);

            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            // Change the layer of all child objects back to the default layer.
            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            // Disable UI elements related to holding and interacting with the box.
            UIEvents.RaiseActionUI(ActionType.Throw, false, null);
            UIEvents.RaiseActionUI(ActionType.Open, false, null);
            UIEvents.RaiseActionUI(ActionType.Close, false, null);
            UIEvents.RaiseActionUI(ActionType.Place, false, null);
            UIEvents.RaiseActionUI(ActionType.Take, false, null);

            interactor.StateManager.PopState();

            interactor = null;

            IsDisposable = true;
        }

        /// <summary>
        /// Disables physics for the box with a slight delay. 
        /// This prevents issues where the box's position is not 
        /// fully updated by the physics engine after being moved 
        /// using `Transform` (e.g., when picking up stacked boxes).
        /// </summary>
        private IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);

            SetActivePhysics(false);
        }

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        /// <summary>
        /// Starts a timer to check for collisions after the box is thrown.
        /// Collisions are only checked within the specified `collisionCheckDuration`.
        /// </summary>
        private IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            IsCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            IsCheckingCollision = false;
        }

        /// <summary>
        /// Opens the box lids with a smooth animation.
        /// Sets the IsOpen flag to true and enables the "Close" button.
        /// </summary>
        private void Open()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = true;
            UIEvents.RaiseActionUI(ActionType.Close, true, Close);
            UIEvents.RaiseActionUI(ActionType.Open, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Closes the box lids with a smooth animation.
        /// Sets the IsOpen flag to false and enables the "Open" button.
        /// </summary>
        private void Close()
        {
            if (lidSequence.IsActive()) return;

            IsOpen = false;
            UIEvents.RaiseActionUI(ActionType.Open, true, Open);
            UIEvents.RaiseActionUI(ActionType.Close, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip));
        }

        /// <summary>
        /// Opens the box lids immediately without animation. 
        /// Primarily used for initialization purposes.
        /// </summary>
        public void SetLidsOpen()
        {
            lidFront.localRotation = Quaternion.Euler(Vector3.right * 160f);
            lidBack.localRotation = Quaternion.Euler(Vector3.left * 160f);
            lidLeft.localRotation = Quaternion.Euler(Vector3.back * 160f);
            lidRight.localRotation = Quaternion.Euler(Vector3.forward * 160f);

            IsOpen = true;
        }

        public IEnumerator OpenLidsSmooth()
        {
            float duration = 0.3f;

            lidFront.DOLocalRotate(Vector3.right * 250f, duration, RotateMode.LocalAxisAdd);
            lidBack.DOLocalRotate(Vector3.left * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            lidLeft.DOLocalRotate(Vector3.back * 250f, duration, RotateMode.LocalAxisAdd);
            lidRight.DOLocalRotate(Vector3.forward * 250f, duration, RotateMode.LocalAxisAdd);

            yield return new WaitForSeconds(duration);

            IsOpen = true;
        }

        public void CloseIfOpened()
        {
            if (!IsOpen) return;

            var lidSequence = DOTween.Sequence();

            lidSequence.Append(lidLeft.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Append(lidFront.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd));

            IsOpen = false;
        }

        /// <summary>
        /// Places the last product from the box onto the specified shelf.
        /// Performs necessary checks for compatibility (product type, shelf space) 
        /// and updates the UI accordingly.
        /// </summary>
        /// <param name="shelf">The target shelf to place the product on.</param>
        /// <returns>True if the product was placed successfully, false otherwise.</returns>        
        public bool Place(Shelf shelf)
        {
            if (shelf.AssignedProduct != null && Product != shelf.AssignedProduct)
            {
                UIEvents.RaiseMessage(placeAssignedDifferentProductMessage.GetLocalizedString(), Color.red);
                return false;
            }
            else if (shelf.ShelvingUnit.Section != Product.Section)
            {
                UIEvents.RaiseMessage(placeWrongSectionMessage.GetLocalizedString(), Color.red);
                return false;
            }
            else if (shelf.Product == null)
            {
                shelf.Initialize(Product);
            }
            else if (shelf.Product != Product)
            {
                UIEvents.RaiseMessage(placeDifferentProductMessage.GetLocalizedString(), Color.red);
                return false;
            }

            var productModel = productModels.LastOrDefault();
            int prevShelfQty = shelf.Quantity;

            if (shelf.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.transform.SetParent(shelf.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                AudioManager.Instance.PlaySFX(AudioID.Draw);

                productModel.layer = LayerMask.NameToLayer("Default");

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                    UIEvents.RaiseActionUI(ActionType.Place, false, null);
                }

                if (prevShelfQty == 0)
                    UIEvents.RaiseActionUI(ActionType.Take, true, () => Take(shelf));

                return true;
            }
            else
            {
                UIEvents.RaiseMessage(placeShelfFullMessage.GetLocalizedString(), Color.red);
            }

            return false;
        }

        /// <summary>
        /// Takes a product from the specified shelf and adds it to the box.
        /// Performs necessary checks for compatibility (box capacity, product type, box size).
        /// Handles UI updates to reflect changes in the box and shelf states.
        /// </summary>
        /// <param name="shelf">The shelf to take a product from.</param>
        /// <returns>True if a product was successfully taken from the shelf, false otherwise.</returns>
        public bool Take(Shelf shelf)
        {
            // Check if the box is full
            if (Product != null && Quantity >= Capacity)
            {
                UIEvents.RaiseMessage(takeBoxFullMessage.GetLocalizedString(), Color.red);
                return false;
            }

            // If the box is not empty, check if the product types match
            if (Quantity > 0 && shelf.Product != Product)
            {
                UIEvents.RaiseMessage(takeDifferentProductMessage.GetLocalizedString(), Color.red);
                return false;
            }

            // If the box is empty, check if the product's box size is compatible
            if (Quantity == 0 && Size != shelf.Product.Box.GetComponent<Box>().Size)
            {
                UIEvents.RaiseMessage(takeIncompatibleSizeMessage.GetLocalizedString(), Color.red);
                return false;
            }

            // Initialize the box if empty and compatible
            if (Quantity == 0)
            {
                Initialize(shelf.Product);
            }

            // Take the product from the shelf and add it to the box
            int prevQuantity = Quantity;
            var position = productPositions[prevQuantity];

            var productModel = shelf.TakeProductModel();
            productModels.Add(productModel);

            productModel.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();

            productModel.transform.SetParent(transform);
            DOTween.Kill(productModel.transform);
            productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
            productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

            AudioManager.Instance.PlaySFX(AudioID.Draw);

            // If this was the first product added, enable the Place button
            if (prevQuantity == 0)
            {
                UIEvents.RaiseActionUI(ActionType.Place, true, () => Place(shelf));
            }

            if (shelf.Quantity == 0)
            {
                UIEvents.RaiseActionUI(ActionType.Take, false, null);
            }

            return true;
        }

        public bool Store(Rack rack, bool isPlayer)
        {
            if (rack.Product == null)
            {
                rack.Initialize(Product);
            }
            else if (rack.Product != Product)
            {
                if (isPlayer) UIEvents.RaiseMessage(storeDifferentProductMessage.GetLocalizedString(), Color.red);
                return false;
            }

            if (rack.CanStoreBox(this, out Vector3 position, isPlayer))
            {
                IsStored = true;
                IsDisposable = false;

                transform.SetParent(rack.transform);
                DOTween.Kill(transform);
                transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                transform.DOLocalRotate(Vector3.zero, 0.5f);

                // Change the layer of all products in the box back to the default layer.
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }

                if (isPlayer)
                {
                    if (IsOpen) Close();

                    // Disable UI elements related to holding and interacting with the box.
                    UIEvents.RaiseActionUI(ActionType.Throw, false, null);
                    UIEvents.RaiseActionUI(ActionType.Open, false, null);
                    UIEvents.RaiseActionUI(ActionType.Close, false, null);
                    UIEvents.RaiseActionUI(ActionType.Place, false, null);

                    AudioManager.Instance.PlaySFX(AudioID.Throw);

                    interactor.StateManager.PopState();

                    interactor = null;
                }

                return true;
            }
            else if (isPlayer)
            {
                UIEvents.RaiseMessage(storeRackFullMessage.GetLocalizedString(), Color.red);
            }

            return false;
        }

        public void Stock(Shelf shelfToStock)
        {
            if (shelfToStock.Product == null)
            {
                shelfToStock.Initialize(shelfToStock.AssignedProduct);
            }

            var productModel = productModels.LastOrDefault();

            if (shelfToStock.PlaceProductModel(productModel, out Vector3 position))
            {
                productModel.layer = LayerMask.NameToLayer("Default");

                productModel.transform.SetParent(shelfToStock.transform);
                DOTween.Kill(productModel.transform);
                productModel.transform.DOLocalJump(position, 0.5f, 1, 0.5f);
                productModel.transform.DOLocalRotate(Vector3.zero, 0.5f);

                productModels.Remove(productModel);

                if (Quantity == 0)
                {
                    Product = null;
                }
            }
        }

        #region Box Info
        public Sprite GetIcon() => Product?.Icon;

        public IEnumerable<BoxDetail> GetDetails()
        {
            return new List<BoxDetail>
            {
                new BoxDetail { LabelKey = "BoxInfo_Label_Name", Value = Product != null ? Product.Name : "—" },
                new BoxDetail { LabelKey = "BoxInfo_Label_Qty", Value = $"{Quantity}/{Capacity}" },
                new BoxDetail { LabelKey = "BoxInfo_Label_Size", Value = $"{Size.x*100}x{Size.z*100}x{Size.y*100}" }
            };
        }
        #endregion
    }
}
