using UnityEngine;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class Freezer : ShelvingUnit
    {
        [Header("Freezer Settings")]
        [SerializeField, Tooltip("Transform reference for the left freezer door.")]
        private Transform doorLeft;

        [SerializeField, Tooltip("Transform reference for the right freezer door.")]
        private Transform doorRight;

        [SerializeField, Tooltip("The horizontal offset the door moves when opened.")]
        private float openOffset = 0.85f;

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
        /// Opens the freezer door with animation.
        /// </summary>
        /// <param name="forced">If true, the UI `Close` button is not shown after the door animation.
        /// This is used when the door is opened by an external interaction (not via UI `Open` button).
        /// For example, a customer opening the door.
        /// Another example, the player placing a product on a designated shelf that triggers the door to open.</param>
        /// <param name="playSFX">If true, plays the `sliding door` sound effect.</param>
        public override void Open(bool forced, bool playSFX)
        {
            bool isPlayer = playSFX;
            bool isAimingLeftDoor = false;

            if (isPlayer)
            {
                Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                Physics.Raycast(ray, out RaycastHit hit, 3.5f, 1 << gameObject.layer);
                isAimingLeftDoor = transform.InverseTransformPoint(hit.point).x > 0f;
            }

            if (isAimingLeftDoor)
            {
                doorLeft.DOLocalMoveX(-openOffset, 0.3f)
                    .OnComplete(() =>
                    {
                        IsOpen = true;
                        if (!forced) ShowCloseButton();
                    });
            }
            else
            {
                doorRight.DOLocalMoveX(openOffset, 0.3f)
                    .OnComplete(() =>
                    {
                        IsOpen = true;
                        if (!forced) ShowCloseButton();
                    });
            }

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.SlidingDoor);
        }

        /// <summary>
        /// Closes the freezer doors with animation.
        /// </summary>
        /// <param name="forced">If true, the UI `Open` button is not shown after the door animation.
        /// This is used when the door is closed by an external interaction (not via UI `Close` button).
        /// For example, a customer closing the door.</param>
        /// <param name="playSFX">If true, plays the `sliding door` sound effect.</param>
        public override void Close(bool forced, bool playSFX)
        {
            doorRight.DOLocalMoveX(0f, 0.3f)
                .OnComplete(() =>
                {
                    IsOpen = false;
                    if (!forced) ShowOpenButton();
                });

            doorLeft.DOLocalMoveX(0f, 0.3f);

            if (playSFX) AudioManager.Instance.PlaySFX(AudioID.SlidingDoor);
        }
    }
}
