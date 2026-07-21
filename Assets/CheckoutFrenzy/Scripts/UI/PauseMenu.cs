using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class PauseMenu : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Tooltip("RectTransform of the main panel.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("The default position of the main panel.")]
        private Vector2 defaultPosition = new Vector2(0f, -30f);

        [SerializeField, Tooltip("A message indicating that the game has been saved.")]
        private TextMeshProUGUI saveMessage;

        [SerializeField, Tooltip("Reference to the settings window.")]
        private SettingsWindow settingsWindow;

        [SerializeField, Tooltip("The ScreenFader component to handle scene transitions.")]
        private ScreenFader screenFader;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button quitButton;

        private PlayerStateManager stateManager;

        private Sequence saveMessageSequence;

        private bool isLoadingMainMenu;

        private void Start()
        {
            mainPanel.anchoredPosition = defaultPosition;

            if (TryGetComponent<Image>(out Image image))
            {
                image.color = new Color(0f, 0f, 0f, 0.9f);
            }

            saveMessage.color = new Color(1f, 1f, 1f, 0f);

            // Initialize Buttons
            resumeButton.onClick.AddListener(Close);
            saveButton.onClick.AddListener(SaveGame);
            settingsButton.onClick.AddListener(() => settingsWindow.Toggle());
            menuButton.onClick.AddListener(LoadMainMenu);
            quitButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                stateManager = player.StateManager;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Handles pointer clicks on the pause menu.
        /// Closes the menu if clicked outside the main panel.
        /// </summary>
        /// <param name="eventData">The pointer event data.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // Check if the click originated from the main panel.
            if (RectTransformUtility.RectangleContainsScreenPoint(mainPanel, eventData.position))
            {
                // Clicked on the main panel, do nothing.
                return;
            }

            // Clicked outside the main panel, deactivate the game object.
            Close();
        }

        /// <summary>
        /// Opens the pause menu.
        /// </summary>
        public void Open()
        {
            gameObject.SetActive(true);
            stateManager?.PushState(PlayerState.Paused);
            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        /// <summary>
        /// Closes the pause menu.
        /// </summary>
        public void Close()
        {
            if (settingsWindow.gameObject.activeSelf || isLoadingMainMenu) return;

            gameObject.SetActive(false);
            stateManager?.PopState();
            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private void SaveGame()
        {
            DataManager.Instance.SaveGameData();
            AudioManager.Instance.PlaySFX(AudioID.Click);

            // If the sequence is running, don't start another one
            if (saveMessageSequence != null && saveMessageSequence.IsActive() && saveMessageSequence.IsPlaying())
                return;

            saveMessageSequence = DOTween.Sequence()
                .SetUpdate(UpdateType.Normal, true)
                .Append(saveMessage.DOFade(1f, 0.5f).SetUpdate(UpdateType.Normal, true))
                .AppendInterval(0.5f)
                .Append(saveMessage.DOFade(0f, 0.5f).SetUpdate(UpdateType.Normal, true))
                .OnComplete(() => saveMessageSequence = null);
        }

        private void LoadMainMenu()
        {
            isLoadingMainMenu = true;

            screenFader.FadeIn(onComplete: () =>
                StartCoroutine(LoadMainMenuAsync())
            );

            DataManager.Instance.SaveGameData();
            AudioManager.Instance.PlaySFX(AudioID.Click);
        }

        private IEnumerator LoadMainMenuAsync()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(0);
            asyncLoad.allowSceneActivation = false;

            // Wait until the asynchronous scene fully loads.
            while (!asyncLoad.isDone)
            {
                // Scene has loaded as much as possible, the last 10% can't be multi-threaded.
                if (asyncLoad.progress >= 0.9f)
                {
                    Time.timeScale = 1f;
                    DOTween.KillAll();

                    // Activate the scene when it's almost fully loaded.
                    asyncLoad.allowSceneActivation = true;
                }

                yield return new WaitForEndOfFrame();
            }

            isLoadingMainMenu = false;
        }
    }
}
