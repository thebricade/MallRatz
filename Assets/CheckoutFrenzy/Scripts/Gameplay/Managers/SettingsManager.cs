using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
using SimpleInputNamespace;

namespace CryingSnow.CheckoutFrenzy.Gameplay
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [SerializeField] private AudioMixer audioMixer;

        private const string BGM_KEY = "BGM Volume";
        private const string SFX_KEY = "SFX Volume";
        private const string LANG_KEY = "Language";
        private const string INTERACT_KEY = "Display Interact Message";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 120;
            }

            SetBGM(GetBGM());
            SetSFX(GetSFX());
            SetLanguage(GetLanguage());
        }

        #region Audio

        public float GetBGM()
        {
            return PlayerPrefs.GetFloat(BGM_KEY, 0.8f);
        }

        public void SetBGM(float value)
        {
            PlayerPrefs.SetFloat(BGM_KEY, value);

            float db = value > 0 ? Mathf.Log10(value) * 20 : -80f;
            audioMixer.SetFloat("BGM Volume", db);
        }

        public float GetSFX()
        {
            return PlayerPrefs.GetFloat(SFX_KEY, 0.8f);
        }

        public void SetSFX(float value)
        {
            PlayerPrefs.SetFloat(SFX_KEY, value);

            float db = value > 0 ? Mathf.Log10(value) * 20 : -80f;
            audioMixer.SetFloat("SFX Volume", db);
        }

        #endregion

        #region Language

        public int GetLanguage()
        {
            return PlayerPrefs.GetInt(LANG_KEY, 0);
        }

        public void SetLanguage(int index)
        {
            PlayerPrefs.SetInt(LANG_KEY, index);

            var locales = LocalizationSettings.AvailableLocales.Locales;
            LocalizationSettings.SelectedLocale = locales[index];
        }

        #endregion

        #region Gameplay

        public bool GetInteractMessage()
        {
            return PlayerPrefs.GetInt(INTERACT_KEY, 1) == 1;
        }

        public void SetInteractMessage(bool value)
        {
            PlayerPrefs.SetInt(INTERACT_KEY, value ? 1 : 0);
        }

        #endregion
    }
}
