using DG.Tweening;
using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Fridge : ShelvingUnit
    {
        [Header("Fridge Settings")]
        [SerializeField, Tooltip("Transform reference for the left fridge door.")]
        private Transform doorLeft;

        [SerializeField, Tooltip("Transform reference for the right fridge door (optional).")]
        private Transform doorRight;

        [SerializeField, Tooltip("Angle in degrees to which the fridge doors open.")]
        private float openAngle = 90f;

        protected override void Awake()
        {
            base.Awake();

            IsOpen = false;
        }

        public override void OnFocused()
        {
            base.OnFocused();

            if (IsOpen) ShowCloseButton();
            else ShowOpenButton();
        }

        public override void OnDefocused()
        {
            base.OnDefocused();

            UIEvents.RaiseActionUI(ActionType.Close, false, null);
            UIEvents.RaiseActionUI(ActionType.Open, false, null);
        }

        private void ShowOpenButton()
        {
            UIEvents.RaiseActionUI(ActionType.Open, true, () =>
            {
                UIEvents.RaiseActionUI(ActionType.Open, false, null);
                Open(false, true);
            });
        }

        private void ShowCloseButton()
        {
            UIEvents.RaiseActionUI(ActionType.Close, true, () =>
            {
                UIEvents.RaiseActionUI(ActionType.Close, false, null);
                Close(false, true);
            });
        }

        /// <summary>
        /// Opens the fridge doors with animation.
        /// </summary>
        /// <param name="forced">If true, the UI `Close` button is not shown after the door animation.
        /// This is used when the door is opened by an external interaction (not via UI `Open` button).
        /// For example, a customer opening the door.
        /// Another example, the player placing a product on a designated shelf that triggers the door to open.</param>
        /// <param name="playSFX">If true, plays the `door open` sound effect.</param>
        public override void Open(bool forced, bool playSFX)
        {
            if (doorLeft != null)
            {
                doorLeft.DOLocalRotate(Vector3.up * openAngle, 0.3f).OnComplete(() =>
                {
                    IsOpen = true;
                    if (!forced) ShowCloseButton();
                });
            }

            if (doorRight != null)
            {
                doorRight.DOLocalRotate(Vector3.down * openAngle, 0.3f).OnComplete(() =>
                {
                    IsOpen = true;
                    if (!forced) ShowCloseButton();
                });
            }

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.DoorOpen);
        }

        /// <summary>
        /// Closes the fridge doors with animation.
        /// </summary>
        /// <param name="forced">If true, the UI `Open` button is not shown after the door animation.
        /// This is used when the door is closed by an external interaction (not via UI `Close` button).
        /// For example, a customer closing the door.</param>
        /// <param name="playSFX">If true, plays the `door close` sound effect.</param>
        public override void Close(bool forced, bool playSFX)
        {
            if (doorLeft != null)
            {
                doorLeft.DOLocalRotate(Vector3.zero, 0.3f).OnComplete(() =>
                {
                    IsOpen = false;
                    if (!forced) ShowOpenButton();
                });
            }

            if (doorRight != null)
            {
                doorRight.DOLocalRotate(Vector3.zero, 0.3f).OnComplete(() =>
                {
                    IsOpen = false;
                    if (!forced) ShowOpenButton();
                });
            }

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.DoorClose);
        }
    }
}
