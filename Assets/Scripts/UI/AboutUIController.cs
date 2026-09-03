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
            OptimizeLayout();
        }

        private void OptimizeLayout()
        {
            if (appNameVersionText == null) return;

            // 1. Format the Text component
            appNameVersionText.enableWordWrapping = true;
            appNameVersionText.alignment = TextAlignmentOptions.Top;
            
            RectTransform textRect = appNameVersionText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0.5f, 0.5f);
                
                // Make the text dynamically size its height based on content
                ContentSizeFitter textCsf = appNameVersionText.gameObject.GetComponent<ContentSizeFitter>();
                if (textCsf == null) textCsf = appNameVersionText.gameObject.AddComponent<ContentSizeFitter>();
                textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // 2. Format the InfoCard (Dark Panel)
                RectTransform infoCardRect = textRect.parent.GetComponent<RectTransform>();
                if (infoCardRect != null)
                {
                    VerticalLayoutGroup cardVlg = infoCardRect.gameObject.GetComponent<VerticalLayoutGroup>();
                    if (cardVlg == null) cardVlg = infoCardRect.gameObject.AddComponent<VerticalLayoutGroup>();
                    
                    // Comfortable padding inside the dark panel
                    cardVlg.padding = new RectOffset(60, 60, 80, 80);
                    cardVlg.childAlignment = TextAnchor.UpperCenter;
                    cardVlg.childControlHeight = true;
                    cardVlg.childControlWidth = true;
                    cardVlg.childForceExpandHeight = false;
                    cardVlg.childForceExpandWidth = false;

                    ContentSizeFitter cardCsf = infoCardRect.gameObject.GetComponent<ContentSizeFitter>();
                    if (cardCsf == null) cardCsf = infoCardRect.gameObject.AddComponent<ContentSizeFitter>();
                    cardCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    
                    // Keep the width fixed to prevent text stretching too wide
                    infoCardRect.sizeDelta = new Vector2(900f, infoCardRect.sizeDelta.y);

                    // 3. Format the Root About Panel (Background)
                    RectTransform rootPanelRect = infoCardRect.parent.GetComponent<RectTransform>();
                    if (rootPanelRect != null)
                    {
                        VerticalLayoutGroup rootVlg = rootPanelRect.gameObject.GetComponent<VerticalLayoutGroup>();
                        if (rootVlg == null) rootVlg = rootPanelRect.gameObject.AddComponent<VerticalLayoutGroup>();
                        
                        rootVlg.padding = new RectOffset(0, 0, 120, 120); // Top/Bottom screen margins
                        rootVlg.spacing = 80; // Clear breathing room between Title, Panel, and Back button
                        rootVlg.childAlignment = TextAnchor.MiddleCenter;
                        rootVlg.childControlHeight = false;
                        rootVlg.childControlWidth = false;
                        rootVlg.childForceExpandHeight = false;
                        rootVlg.childForceExpandWidth = false;
                    }
                }
            }
        }

        private void SetupInfoTexts()
        {
            if (appNameVersionText != null)
            {
                string appName = "<size=48>Smart AR Measure</size>\n";
                string version = "<size=32><color=#BBBBBB>Version " + Application.version + " (Unity 6)</color></size>\n\n\n";
                
                string noticeHeading = "<color=#00E5FF><size=36>USAGE & ACCURACY NOTICE</size></color>\n\n";
                string noticeBody = "<size=34><color=#DDDDDD>This app works best in well-lit environments with clear, textured surfaces and stable surroundings. For better results, keep the camera steady and move slowly while allowing the AR system to detect and understand the environment.</color></size>\n\n\n";
                
                string cautionHeading = "<color=#FF5555><size=36>⚠ CAUTION</size></color>\n\n";
                string cautionBody = "<size=34><color=#DDDDDD>AR measurements are estimates and may not always be accurate. Accuracy can vary depending on lighting, surface texture, camera movement, distance, device hardware, and environmental conditions.\n\nDo not rely on these measurements for critical, professional, or safety-sensitive applications.</color></size>";

                appNameVersionText.text = appName + version + noticeHeading + noticeBody + cautionHeading + cautionBody;
                appNameVersionText.richText = true;
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
                                           "Precision AR Real-World Measurement Solutions.\n\n" +
                                           "Usage & Accuracy Notice\n" +
                                           "This app works best in well-lit environments with clear, textured surfaces and stable surroundings. For better results, keep the camera steady and move slowly while allowing the AR system to detect and understand the environment.\n\n" +
                                           "⚠ Caution: AR measurements are estimates and may not always be accurate. Measurement accuracy can vary depending on lighting, surface texture, camera movement, distance, device hardware, and environmental conditions. Do not rely on these measurements for critical, professional, or safety-sensitive applications.";
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
