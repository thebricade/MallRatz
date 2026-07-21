using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance { get; set; }

        [SerializeField, Tooltip("The ScreenFader component to handle scene transitions.")]
        private ScreenFader screenFader;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            AudioManager.Instance.PlayBGMQueue();
        }

        /// <summary>
        /// Starts the game, fading out the main menu and loading the next scene asynchronously.
        /// </summary>
        public void StartGame()
        {
            screenFader.FadeIn(onComplete: () => // Fade in the screen.
                StartCoroutine(StartGameAsync()) // Start the asynchronous scene loading coroutine when the fade in is complete.
            );

            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private IEnumerator StartGameAsync()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1); // Load Game scene (index 1) asynchronously.
            asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation.

            // Wait until the asynchronous scene fully loads.
            while (!asyncLoad.isDone)
            {
                // Scene has loaded as much as possible,
                // the last 10% can't be multi-threaded.
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true; // Activate the scene when it's almost fully loaded.
                }

                yield return null; // Wait for the next frame.
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
