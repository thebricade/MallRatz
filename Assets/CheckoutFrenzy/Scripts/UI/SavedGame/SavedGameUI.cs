using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using CryingSnow.CheckoutFrenzy.Core;
using CryingSnow.CheckoutFrenzy.Gameplay;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class SavedGameUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI slotNumber;
        [SerializeField] private TextMeshProUGUI description;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button deleteGameButton;

        [Header("Localization")]
        [SerializeField] private LocalizedString slotLabel;
        [SerializeField] private LocalizedString emptySlotDescription;
        [SerializeField] private LocalizedString saveDescription;

        [Header("Style")]
        [SerializeField] private Color emptySlotColor = new(0.38f, 0.38f, 0.38f);

        private int slot;
        private GameData cachedGameData;

        private Color defaultColor;
        private FontStyles defaultStyle;

        private void Awake()
        {
            defaultColor = description.color;
            defaultStyle = description.fontStyle;
        }

        public void Initialize(GameData gameData, System.Action<int> onDelete)
        {
            slot = transform.GetSiblingIndex() + 1;
            cachedGameData = gameData;

            slotLabel.Arguments = new object[] { slot };

            if (gameData == null)
            {
                description.color = emptySlotColor;
                description.fontStyle = FontStyles.Italic;
            }
            else
            {
                description.color = defaultColor;
                description.fontStyle = defaultStyle;

                saveDescription.Arguments = new object[]
                {
                    gameData.StoreName,
                    gameData.TotalPlaytime.TotalHours,
                    GameConfig.Instance.ActiveCurrency.currencySymbol,
                    gameData.PlayerMoney,
                    gameData.LastSaved
                };
            }

            newGameButton.onClick.RemoveAllListeners();
            loadGameButton.onClick.RemoveAllListeners();
            deleteGameButton.onClick.RemoveAllListeners();

            if (gameData == null)
            {
                newGameButton.gameObject.SetActive(true);
                loadGameButton.gameObject.SetActive(false);
                deleteGameButton.gameObject.SetActive(false);

                newGameButton.onClick.AddListener(StartGameAtSlot);
            }
            else
            {
                newGameButton.gameObject.SetActive(false);
                loadGameButton.gameObject.SetActive(true);
                deleteGameButton.gameObject.SetActive(true);

                loadGameButton.onClick.AddListener(StartGameAtSlot);
                deleteGameButton.onClick.AddListener(() => onDelete?.Invoke(slot));
            }

            slotLabel.StringChanged += OnSlotLabelChanged;
            emptySlotDescription.StringChanged += OnEmptyChanged;
            saveDescription.StringChanged += OnSaveChanged;
        }

        private void OnDestroy()
        {
            slotLabel.StringChanged -= OnSlotLabelChanged;
            emptySlotDescription.StringChanged -= OnEmptyChanged;
            saveDescription.StringChanged -= OnSaveChanged;
        }

        private void OnSlotLabelChanged(string value)
        {
            slotNumber.text = value;
        }

        private void OnEmptyChanged(string value)
        {
            if (cachedGameData == null)
                description.text = value;
        }

        private void OnSaveChanged(string value)
        {
            if (cachedGameData != null)
                description.text = value;
        }

        private void StartGameAtSlot()
        {
            PlayerPrefs.SetInt("Slot", slot);
            AudioManager.Instance.PlaySFX(AudioID.Click);
            MainMenu.Instance.StartGame();
        }
    }
}
