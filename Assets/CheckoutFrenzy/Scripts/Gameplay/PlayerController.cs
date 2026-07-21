using UnityEngine;
using Cinemachine;
using SimpleInputNamespace;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerController : MonoBehaviour, IInteractor
    {
        [SerializeField, Tooltip("Movement speed of the player.")]
        private float movingSpeed = 7.5f;

        [SerializeField, Tooltip("Gravity applied to the player.")]
        private float gravity = -9.81f;

        [SerializeField, Tooltip("Rotation speed of the player's view.")]
        private float lookSpeed = 2f;

        [SerializeField, Tooltip("Multiplier applied to Look input when using mobile touch controls.")]
        private float mobileLookMultiplier = 2f;

        [SerializeField, Tooltip("Maximum angle the player can look up or down.")]
        private float lookXLimit = 45.0f;

        [SerializeField, Tooltip("Maximum distance for interaction.")]
        private float interactDistance = 3.5f;

        [SerializeField, Tooltip("Time in seconds the interact button must be held to trigger an interaction.")]
        private float interactHoldThreshold = 1.0f;

        [SerializeField, Tooltip("Transform representing the player's holding point (hands).")]
        private Transform holdPoint;

        [SerializeField, Tooltip("Transform representing the player's camera.")]
        private Transform playerCamera;

        [Header("Sway Settings")]
        [SerializeField, Tooltip("Amount of sway applied to the camera.")]
        private float swayAmount = 0.05f;

        [SerializeField, Tooltip("Speed of the camera sway.")]
        private float swaySpeed = 5.0f;

        [SerializeField, Tooltip("Maximum amount of sway applied to the camera.")]
        private float maxSwayAmount = 0.2f;

        [Header("Bobbing Settings")]
        [SerializeField, Tooltip("Frequency of the camera bobbing effect.")]
        private float bobFrequency = 10.0f;

        [SerializeField, Tooltip("Horizontal amplitude of the camera bobbing effect.")]
        private float bobHorizontalAmplitude = 0.04f;

        [SerializeField, Tooltip("Vertical amplitude of the camera bobbing effect.")]
        private float bobVerticalAmplitude = 0.04f;

        [SerializeField, Tooltip("Smoothing applied to the camera bobbing effect.")]
        private float bobSmoothing = 8f;

        [Header("Sound Effects")]
        [SerializeField, Tooltip("Array of audio clips used for footstep sounds.")]
        private AudioClip[] footstepClips;

        [SerializeField, Tooltip("Distance traveled before playing a footstep sound.")]
        private float stepDistance = 2.0f;

        public Transform HoldPoint => holdPoint;
        public Vector3 Position => transform.position;
        public PlayerStateManager StateManager { get; private set; }

        private Camera mainCamera;

        private CharacterController controller;
        private Vector3 movement;
        private Vector3 playerVelocity = Vector3.zero;
        private float rotationX = 0;
        private string xLookAxis;
        private string yLookAxis;
        private bool isMobileControl;

        private AudioSource audioSource;
        private CinemachineVirtualCamera playerVirtualCam;

        private Interactable lastInteractable;
        private Shelf lastShelf;
        private Rack lastRack;

        private Vector3 holdPointOrigin;

        private float bobTimer = 0.0f;
        private float interactHoldDuration = 0f;

        private float distanceTraveled;

        private void Awake()
        {
            StateManager = new PlayerStateManager();

            controller = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();
            playerVirtualCam = GetComponentInChildren<CinemachineVirtualCamera>();

            holdPointOrigin = holdPoint.localPosition;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            isMobileControl = GameConfig.Instance.ControlMode == ControlMode.Mobile;
            xLookAxis = isMobileControl ? "Look X" : "Mouse X";
            yLookAxis = isMobileControl ? "Look Y" : "Mouse Y";

            // Mobile platforms use higher sensitivity for Look controls
            if (Application.isMobilePlatform)
            {
                var touchpad = FindFirstObjectByType<Touchpad>();
                if (touchpad != null) touchpad.sensitivity = mobileLookMultiplier;
            }
        }

        private void OnEnable()
        {
            UIEvents.OnUIPanelOpened += HandleUIOpened;
            UIEvents.OnUIPanelClosed += HandleUIClosed;
        }

        private void OnDisable()
        {
            UIEvents.OnUIPanelOpened -= HandleUIOpened;
            UIEvents.OnUIPanelClosed -= HandleUIClosed;
        }

        private void HandleUIOpened()
        {
            StateManager.PushState(PlayerState.Busy);
        }

        private void HandleUIClosed()
        {
            StateManager.PopState();
        }

        private void Update()
        {
            HandleMovement();
            HandleSway();
            HandleBobbing();
            HandleFootsteps();

            switch (StateManager.CurrentState)
            {
                case PlayerState.Free:
                    DetectInteractable();
                    DetectShelfToCustomize();
                    DetectRack();
                    break;

                case PlayerState.Holding:
                    DetectShelfToRestock();
                    DetectRack();
                    break;

                case PlayerState.Working:
                    Work();
                    break;

                case PlayerState.Moving:
                case PlayerState.Busy:
                case PlayerState.Paused:
                default:
                    break;
            }
        }

        private void LateUpdate()
        {
            HandleLook();
        }

        private void HandleMovement()
        {
            // Gravity handling
            if (controller.isGrounded && playerVelocity.y < 0f)
            {
                playerVelocity.y = -2f;
            }

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            float curSpeedX = !IsMovementBlocked() ? movingSpeed * SimpleInput.GetAxis("Vertical") : 0f;
            float curSpeedY = !IsMovementBlocked() ? movingSpeed * SimpleInput.GetAxis("Horizontal") : 0f;

            movement = (forward * curSpeedX) + (right * curSpeedY);
            controller.Move(movement * Time.deltaTime);

            // Gravity
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (!IsMovementBlocked())
            {
                float lookY = SimpleInput.GetAxis(yLookAxis) * lookSpeed;
                float lookX = SimpleInput.GetAxis(xLookAxis) * lookSpeed;

                rotationX -= lookY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
                transform.rotation *= Quaternion.Euler(0f, lookX, 0f);
            }
        }

        private bool IsMovementBlocked()
        {
            return StateManager.CurrentState is PlayerState.Working
                or PlayerState.Busy
                or PlayerState.Paused;
        }

        private void HandleSway()
        {
            // Get the look input values (horizontal and vertical).
            float lookX = SimpleInput.GetAxis(xLookAxis);
            float lookY = SimpleInput.GetAxis(yLookAxis);

            // Calculate the target position for the hold point based on look input.
            Vector3 targetPosition = new Vector3(-lookX, -lookY, 0) * swayAmount;

            // Clamp the sway amount to prevent excessive movement.
            targetPosition.x = Mathf.Clamp(targetPosition.x, -maxSwayAmount, maxSwayAmount);
            targetPosition.y = Mathf.Clamp(targetPosition.y, -maxSwayAmount, maxSwayAmount);

            // Smoothly interpolate the hold point's position towards the target position.
            holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + targetPosition, Time.deltaTime * swaySpeed);
        }

        private void HandleBobbing()
        {
            // Check if the player is moving.
            if (movement.magnitude > 0.1f)
            {
                // Increment the bob timer based on movement speed and frequency.
                bobTimer += Time.deltaTime * bobFrequency;

                // Calculate the horizontal and vertical offsets for the bobbing effect.
                float horizontalOffset = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
                float verticalOffset = Mathf.Cos(bobTimer * 2) * bobVerticalAmplitude;

                // Combine the offsets into a bobbing position vector.
                Vector3 bobPosition = new Vector3(horizontalOffset, verticalOffset, 0);

                // Smoothly interpolate the hold point's position with the bobbing effect.
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin + bobPosition, Time.deltaTime * bobSmoothing);
            }
            else
            {
                // Smoothly return the hold point to its origin when the player is not moving.
                holdPoint.localPosition = Vector3.Lerp(holdPoint.localPosition, holdPointOrigin, Time.deltaTime * bobSmoothing);
            }
        }

        private void HandleFootsteps()
        {
            // If the player is not moving, don't play footsteps.
            if (movement.magnitude < 0.1f) return;

            // Increase the distance traveled based on movement magnitude and time.
            distanceTraveled += movement.magnitude * Time.deltaTime;

            // Check if the traveled distance exceeds the step distance threshold.
            if (distanceTraveled >= stepDistance)
            {
                PlayFootstepSound();
                distanceTraveled = 0f;
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepClips.Length == 0) return;

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }

        public void SetInteractable(Interactable interactable)
        {
            lastInteractable = interactable;
            InteractWithCurrent();
        }

        private void DetectInteractable()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Perform a raycast to detect an interactable within the interact distance.
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.InteractableLayer))
            {
                var interactable = hit.transform.GetComponent<Interactable>();
                if (interactable != lastInteractable)
                {
                    // Defocus the previous interactable (if any)
                    lastInteractable?.OnDefocused();

                    // Focus on the new interactable
                    UIEvents.RaiseInteractionAvailable(true);
                    lastInteractable = interactable;
                    lastInteractable.OnFocused();

                    // Reset hold duration and UI
                    interactHoldDuration = 0f;
                    UIEvents.RaiseHoldProgressChanged(0f);
                }
            }
            else if (lastInteractable != null)
            {
                // Defocus the last interactable when no interactable is hit
                UIEvents.RaiseInteractionAvailable(false);
                lastInteractable.OnDefocused();
                lastInteractable = null;

                // Reset hold duration and UI
                interactHoldDuration = 0f;
                UIEvents.RaiseHoldProgressChanged(0f);
            }

            if (lastInteractable != null)
            {
                if (lastInteractable is Furniture)
                {
                    // Hold interaction for Furniture
                    if (isMobileControl ? SimpleInput.GetButton("Interact") : Input.GetMouseButton(0))
                    {
                        interactHoldDuration += Time.deltaTime;

                        // Update the radial fill UI based on hold progress
                        UIEvents.RaiseHoldProgressChanged(interactHoldDuration / interactHoldThreshold);

                        if (interactHoldDuration >= interactHoldThreshold)
                        {
                            InteractWithCurrent();
                            interactHoldDuration = 0f;
                            UIEvents.RaiseHoldProgressChanged(0f);
                        }
                    }
                    else
                    {
                        interactHoldDuration = 0f;
                        UIEvents.RaiseHoldProgressChanged(0f);
                    }
                }
                else if (isMobileControl ? SimpleInput.GetButtonDown("Interact") : Input.GetMouseButtonDown(0))
                {
                    // Immediate interaction for other interactables
                    InteractWithCurrent();
                }
            }
        }

        private void InteractWithCurrent()
        {
            lastInteractable?.Interact(this);
            UIEvents.RaiseInteractionAvailable(false);
        }

        private void DetectShelfToCustomize()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Perform a raycast to detect a shelf within the interact distance.
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();

                // Check if the detected shelf is different from the last detected shelf.
                if (detectedShelf != lastShelf)
                {
                    UIEvents.RaiseActionUI(ActionType.Price, false, null);
                    UIEvents.RaiseActionUI(ActionType.Label, false, null);

                    // Check if the detected shelf has a product.
                    if (detectedShelf?.Product != null)
                    {
                        UIEvents.RaiseActionUI(ActionType.Price, true, () =>
                        {
                            UIEvents.RaiseActionUI(ActionType.Price, false, null);

                            StoreEvents.RaisePriceCustomizer(detectedShelf.Product);

                            if (!detectedShelf.ShelvingUnit.IsOpen)
                            {
                                detectedShelf.ShelvingUnit.Open(true, true);
                            }

                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }
                    else
                    {
                        UIEvents.RaiseActionUI(ActionType.Label, true, () =>
                        {
                            UIEvents.RaiseActionUI(ActionType.Label, false, null);

                            StoreEvents.RaiseLabelCustomizerRequested(detectedShelf as ILabelable);

                            detectedShelf.ShelvingUnit.OnDefocused();
                        });
                    }

                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                // Disable the set price and set label action UIs if no shelf is detected.
                UIEvents.RaiseActionUI(ActionType.Price, false, null);
                UIEvents.RaiseActionUI(ActionType.Label, false, null);

                // Reset the last detected shelf.
                lastShelf = null;
            }
        }

        private void DetectShelfToRestock()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Get the currently held box.
            Box box = lastInteractable as Box;

            // Check if the box is open and perform a raycast to detect a shelf.
            if (box.IsOpen && Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.ShelfLayer))
            {
                Shelf detectedShelf = hit.transform.GetComponent<Shelf>();

                // Check if the detected shelf is different from the last detected shelf.
                if (detectedShelf != lastShelf)
                {
                    // Check if the box has items to place.
                    if (box.Quantity > 0)
                        // Enable the place button and set its click action.
                        UIEvents.RaiseActionUI(ActionType.Place, true, () =>
                        {
                            // Attempt to place the box's contents on the shelf.
                            bool placed = box.Place(detectedShelf);

                            // Open the shelving unit if placement was successful and it's not already open.
                            if (placed && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });

                    // Check if the shelf has items to take.
                    if (detectedShelf.Quantity > 0)
                        // Enable the take button and set its click action.
                        UIEvents.RaiseActionUI(ActionType.Take, true, () =>
                        {
                            // Attempt to take items from the shelf and put them in the box.
                            bool taken = box.Take(detectedShelf);

                            // Open the shelving unit if taking was successful and it's not already open.
                            if (taken && !detectedShelf.ShelvingUnit.IsOpen)
                                detectedShelf.ShelvingUnit.Open(true, true);
                        });
                    else
                        // Disable the take button if the shelf is empty.
                        UIEvents.RaiseActionUI(ActionType.Take, false, null);

                    // Update the last detected shelf.
                    lastShelf = detectedShelf;
                }
            }
            else if (lastShelf != null)
            {
                // Disable the place and take buttons if no shelf is detected.
                UIEvents.RaiseActionUI(ActionType.Place, false, null);
                UIEvents.RaiseActionUI(ActionType.Take, false, null);

                // Reset the last detected shelf.
                lastShelf = null;
            }
        }

        private void DetectRack()
        {
            // Create a ray from the center of the viewport.
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.RackLayer))
            {
                Rack detectedRack = hit.transform.GetComponent<Rack>();

                // Check if the detected rack is different from the last detected rack.
                if (detectedRack != lastRack)
                {
                    // Get the currently held box.
                    Box box = lastInteractable as Box;

                    // Check if player held a box and it is not empty.
                    if (box != null && box.Quantity > 0)
                    {
                        // Enable the place button and set its click action.
                        UIEvents.RaiseActionUI(ActionType.Place, true, () =>
                        {
                            // Attempt to store the box on the rack.
                            box.Store(detectedRack, true);
                        });
                    }
                    // Check if the player is NOT holding a box OR is holding an empty box.
                    else if (box == null && detectedRack.BoxQuantity > 0)
                    {
                        // Enable the take button and set its click action.
                        UIEvents.RaiseActionUI(ActionType.Take, true, () =>
                        {
                            // Attempt to retrieve boxes from the rack.
                            detectedRack.RetrieveBox(this);
                            UIEvents.RaiseActionUI(ActionType.Take, false, null);
                        });
                    }
                    else
                    {
                        // Disable the take button if the shelf is empty or the player is holding an empty box.
                        UIEvents.RaiseActionUI(ActionType.Take, false, null);
                    }

                    // Update the last detected shelf.
                    lastRack = detectedRack;
                }
            }
            else if (lastRack != null)
            {
                // Disable the place and take buttons if no rack is detected.
                if (StateManager.CurrentState != PlayerState.Moving)
                    UIEvents.RaiseActionUI(ActionType.Place, false, null);

                UIEvents.RaiseActionUI(ActionType.Take, false, null);

                // Reset the last detected rack.
                lastRack = null;
            }
        }

        private void Work()
        {
            // Check for mouse click and if the last interactable is a CheckoutCounter.
            if (Input.GetMouseButtonDown(0) && lastInteractable is CheckoutCounter counter)
            {
                // If the counter is in the placing state (customer is still placing items), don't scan items.
                if (counter.CurrentState == CounterState.Placing) return;

                // Create a ray from the mouse position.
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

                // Perform a raycast to detect a checkout item within the interact distance.
                if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, GameConfig.Instance.CheckoutItemLayer))
                {
                    if (hit.transform.TryGetComponent<CheckoutItem>(out CheckoutItem item))
                    {
                        item.Scan();
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a position in front of the player, taking into account the camera's pitch.
        /// </summary>
        /// <returns>A Vector3 representing the calculated front position.</returns>
        public Vector3 GetFrontPosition()
        {
            // Get the camera's pitch angle.
            float pitch = mainCamera.transform.localEulerAngles.x;

            // Adjust the pitch angle if it's greater than 180 degrees.
            if (pitch > 180) pitch -= 360;

            // Normalize the pitch angle to a 0-1 range.
            float normalizedPitch = Mathf.InverseLerp(lookXLimit, 0f, pitch);

            // Define the minimum and maximum distances for the front position.
            float minDistance = 1.5f;
            float maxDistance = 3f;

            // Interpolate between the minimum and maximum distances based on the normalized pitch.
            float offset = Mathf.Lerp(minDistance, maxDistance, normalizedPitch);

            // Calculate the front position based on the player's transform and the calculated offset.
            Vector3 front = transform.TransformPoint(Vector3.forward * offset);

            // Return the front position, zeroing out the Y-component and flooring the X and Z components to the nearest tenth.
            return new Vector3(front.x, 0f, front.z).FloorToTenth();
        }

        /// <summary>
        /// Smoothly sets the FOV of player's Cinemachine virtual camera.
        /// </summary>
        /// <param name="targetFOV">Target field of view value.</param>
        /// <param name="duration">How long the tween should take.</param>
        public void SetFOVSmooth(float targetFOV, float duration = 0.5f)
        {
            DOTween.To(
                () => playerVirtualCam.m_Lens.FieldOfView,
                fov => playerVirtualCam.m_Lens.FieldOfView = fov,
                targetFOV,
                duration
            ).SetEase(Ease.InOutSine);
        }

        public T GetInteractorComponent<T>() where T : class
        {
            return (this as MonoBehaviour).GetComponentInChildren<T>();
        }
    }
}
