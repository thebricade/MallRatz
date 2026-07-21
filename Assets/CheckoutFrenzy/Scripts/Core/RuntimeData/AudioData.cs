using UnityEngine;

namespace CryingSnow.CheckoutFrenzy.Core
{
    [System.Serializable]
    public class AudioData
    {
        [Tooltip("Unique ID for each audio clip")]
        public AudioID id;

        [Tooltip("The audio clip associated with the audio ID")]
        public AudioClip clip;
    }
}
