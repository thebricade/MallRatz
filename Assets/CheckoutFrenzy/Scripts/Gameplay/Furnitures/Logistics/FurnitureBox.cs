using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using Unity.AI.Navigation;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class FurnitureBox : Interactable, IBox, IPersistentEntity
    {
        [Header("Messages")]
        [SerializeField, Tooltip("Shown when the box cannot be thrown because something is blocking it.")]
        private LocalizedString throwBlockedMessage;

        [Header("Furniture Settings")]
        public int furnitureId;

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

        public bool IsDisposable { get; private set; }

        // IPersistentEntity Implementation
        public int EntityID => furnitureId;
        public Transform EntityTransform => transform;

        // IBox Implementation
        public string Name => gameObject.name;
        public Transform Transform => transform;
        public Product Product => null;
        public int Quantity => -1;
        public Vector3 Size => Vector3.zero;
        public bool IsOpen => false;

        private bool isCheckingCollision;

        private Rigidbody body;
        private BoxCollider boxCollider;
        private IInteractor interactor;
        private Sequence lidSequence;
        private Coroutine disablePhysicsRoutine;

        protected override void Awake()
        {
            base.Awake();

            body = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();

            SetActivePhysics(false);

            var navMeshMod = gameObject.AddComponent<NavMeshModifier>();
            navMeshMod.ignoreFromBuild = true;
        }

        private IEnumerator Start()
        {
            DataManager.Instance.OnSave += HandleOnSave;

            yield return new WaitUntil(() => DataManager.Instance.IsLoaded);

            SetActivePhysics(true);
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
            var furnitureBoxData = new EntityData(this);
            DataManager.Instance.Data.SavedFurnitureBoxes.Add(furnitureBoxData);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isCheckingCollision) return;

            if (collision.relativeVelocity.magnitude > 2)
            {
                AudioManager.Instance.PlaySFX(AudioID.Impact);
            }
        }

        public override void Interact(IInteractor interactor)
        {
            this.interactor = interactor;

            IsDisposable = false;

            if (disablePhysicsRoutine != null)
            {
                StopCoroutine(disablePhysicsRoutine);
            }

            disablePhysicsRoutine = StartCoroutine(DisablePhysicsDelayed());

            foreach (Transform child in transform)
            {
                child.gameObject.layer = GameConfig.Instance.HeldObjectLayer.ToSingleLayer();
            }

            UIEvents.RaiseActionUI(ActionType.Throw, true, Throw);
            UIEvents.RaiseActionUI(ActionType.Open, true, Open);

            StoreEvents.RaiseBoxSelected(null);

            transform.SetParent(interactor.HoldPoint);
            transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);
            transform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuint);

            AudioManager.Instance.PlaySFX(AudioID.Pick);

            interactor.StateManager.PushState(PlayerState.Moving);

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

        public void SetActivePhysics(bool value)
        {
            body.isKinematic = !value;
            boxCollider.enabled = value;
        }

        private IEnumerator DisablePhysicsDelayed()
        {
            yield return new WaitForSeconds(0.2f);

            SetActivePhysics(false);
        }

        private void Throw()
        {
            var center = transform.position;
            var extents = boxCollider.size / 2f;
            var orientation = transform.rotation;
            var layerMask = ~GameConfig.Instance.PlayerLayer;

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

            transform.SetParent(null);

            SetActivePhysics(true);
            body.AddForce(transform.forward * 3.5f, ForceMode.Impulse);

            StartCoroutine(StartCollisionCheck());

            AudioManager.Instance.PlaySFX(AudioID.Throw);

            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Default");
            }

            UIEvents.RaiseActionUI(ActionType.Throw, false, null);
            UIEvents.RaiseActionUI(ActionType.Open, false, null);
            UIEvents.RaiseActionUI(ActionType.Close, false, null);
            UIEvents.RaiseActionUI(ActionType.Place, false, null);

            interactor.StateManager.PopState();

            interactor = null;

            IsDisposable = true;
        }

        private IEnumerator StartCollisionCheck()
        {
            float timer = collisionCheckDuration;
            isCheckingCollision = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            isCheckingCollision = false;
        }

        private void Open()
        {
            if (lidSequence.IsActive()) return;

            UIEvents.RaiseActionUI(ActionType.Open, false, null);
            UIEvents.RaiseActionUI(ActionType.Throw, false, null);

            lidSequence = DOTween.Sequence();

            lidSequence.Append(lidFront.DOLocalRotate(Vector3.right * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidBack.DOLocalRotate(Vector3.left * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .Append(lidLeft.DOLocalRotate(Vector3.back * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .Join(lidRight.DOLocalRotate(Vector3.forward * 250f, 0.3f, RotateMode.LocalAxisAdd))
                .InsertCallback(0.3f, () => AudioManager.Instance.PlaySFX(AudioID.Flip))
                .OnComplete(() => SpawnFurniture());
        }

        private void SpawnFurniture()
        {
            var furniturePrefab = DataManager.Instance.GetFurnitureById(furnitureId);

            if (furniturePrefab == null)
            {
                Debug.LogWarning("The Furniture ID is invalid!");
                return;
            }

            interactor.StateManager.PopState();

            var furniture = Instantiate(furniturePrefab, interactor.GetFrontPosition(), Quaternion.identity);
            furniture.Interact(interactor);

            Destroy(gameObject);
        }

        #region Box Info
        public Sprite GetIcon()
        {
            var content = DataManager.Instance.GetFurnitureById(furnitureId);
            return content?.Icon;
        }

        public IEnumerable<BoxDetail> GetDetails()
        {
            var content = DataManager.Instance.GetFurnitureById(furnitureId);
            if (content == null) return new List<BoxDetail>();

            return new List<BoxDetail>
            {
                new BoxDetail { LabelKey = "BoxInfo_Label_Name", Value = content.Name },
                new BoxDetail { LabelKey = "BoxInfo_Label_Section", Value = content.Section.ToString() },
                new BoxDetail { LabelKey = "BoxInfo_Label_Price", Value = $"${content.Price:N2}" }
            };
        }
        #endregion
    }
}
