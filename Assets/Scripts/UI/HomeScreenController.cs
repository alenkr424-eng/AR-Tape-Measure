using SmartARMeasure.Core;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// Landing page view controller for Smart AR Measure home screen.
    /// Handles action button listeners and navigation routing.
    /// </summary>
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI appTitleText;
        [SerializeField] private TextMeshProUGUI appSubtitleText;

        [Header("Action Buttons")]
        [SerializeField] private Button startMeasuringButton;
        [SerializeField] private Button historyButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button aboutButton;

        private void Start()
        {
            SetupText();
            BindButtons();
        }

        private void SetupText()
        {
            if (appTitleText != null) appTitleText.text = "Smart AR Measure";
            if (appSubtitleText != null) appSubtitleText.text = "Measure the real world with precision";
        }

        private void BindButtons()
        {
            if (startMeasuringButton != null)
            {
                startMeasuringButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.StartMeasuring();
                });
            }

            if (historyButton != null)
            {
                historyButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenHistory();
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenSettings();
                });
            }

            if (aboutButton != null)
            {
                aboutButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenAbout();
                });
            }
        }
    }
}
