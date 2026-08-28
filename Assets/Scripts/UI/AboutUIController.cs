using SmartARMeasure.Core;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// About Page UI Controller.
    /// Displays version metadata, developer credit details, and technology stack breakdown.
    /// </summary>
    public class AboutUIController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button backButton;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI appNameVersionText;
        [SerializeField] private TextMeshProUGUI techStackText;
        [SerializeField] private TextMeshProUGUI developerCreditsText;

        private void Start()
        {
            SetupInfoTexts();
            BindButtons();
        }

        private void SetupInfoTexts()
        {
            if (appNameVersionText != null)
            {
                appNameVersionText.text = $"Smart AR Measure\nVersion {Application.version} (Unity 6)";
            }

            if (techStackText != null)
            {
                techStackText.text = "<b>Technologies Used:</b>\n" +
                                     "• Unity 6 Engine\n" +
                                     "• AR Foundation 6.x\n" +
                                     "• Google ARCore SDK\n" +
                                     "• Material Design 3 Dark Theme\n" +
                                     "• Android Native Intents & Haptics";
            }

            if (developerCreditsText != null)
            {
                developerCreditsText.text = "Designed & Engineered for Android Portfolio\n" +
                                           "Precision AR Real-World Measurement Solutions.";
            }
        }

        private void BindButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.ReturnToHome();
                });
            }
        }
    }
}
