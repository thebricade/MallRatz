using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    [RequireComponent(typeof(LightEmitter))]
    public class Car : MonoBehaviour
    {
        [SerializeField, Tooltip("The speed at which the car moves (unit per second).")]
        private float speed = 10f;

        [SerializeField, Tooltip("An array of transforms representing the car's wheels.")]
        private Transform[] wheels;

        [Header("Audio")]
        [SerializeField, Tooltip("The sound clip to play for the car's engine.")]
        private AudioClip engineSound;

        [SerializeField, Tooltip("The audio mixer group to route the engine sound to.")]
        private AudioMixerGroup sfxMixer;

        private void Start()
        {
            RotateWheels();
            CreateEngineAudio();
        }

        public void Move(float distance)
        {
            transform.DOLocalMoveZ(distance, speed)
                .SetSpeedBased()
                .SetEase(Ease.Linear)
                .OnComplete(() => DestroyCar());
        }

        private void DestroyCar()
        {
            foreach (Transform wheel in wheels)
            {
                DOTween.Kill(wheel);
            }

            Destroy(gameObject);
        }

        private void RotateWheels()
        {
            foreach (Transform wheel in wheels)
            {
                float diameter = wheel.GetComponent<MeshRenderer>().bounds.size.z;
                float circumference = Mathf.PI * diameter;
                float rotationSpeed = (speed / circumference) * 360f;

                wheel.DOLocalRotate(Vector3.right * 360f, rotationSpeed, RotateMode.FastBeyond360)
                    .SetSpeedBased()
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            }
        }

        private void CreateEngineAudio()
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = engineSound;
            audioSource.outputAudioMixerGroup = sfxMixer;
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.maxDistance = 15f;
            audioSource.Play();
        }
    }
}
