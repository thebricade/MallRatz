using UnityEngine;
using CryingSnow.CheckoutFrenzy.Core;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class CleaningTool : MonoBehaviour
    {
        [SerializeField, Tooltip("Type of cleaning tool this object represents. Used to determine which tool is required for a Cleanable.")]
        private CleaningToolType toolType;

        [SerializeField, Tooltip("Distance the cleaner should stop from the target when using this tool.")]
        private float stoppingDistance;

        [SerializeField, Tooltip("Name of the Animator parameter used to trigger the cleaning animation for this tool.")]
        private string animationTrigger;

        public CleaningToolType ToolType => toolType;
        public float StoppingDistance => stoppingDistance;
        public string AnimationTrigger => animationTrigger;
    }
}
