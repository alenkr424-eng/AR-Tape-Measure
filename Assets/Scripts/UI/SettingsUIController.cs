using SmartARMeasure.Core;
using SmartARMeasure.Models;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// Settings Panel UI Controller.
    /// Interactively configures units, marker/line styling, line thickness sliders, sound/haptic toggles, plane overlays, and reset actions.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetDefaultsButton;

        [Header("Unit System Selection")]
        [SerializeField] private Toggle metricToggle;
        [SerializeField] private Toggle imperialToggle;

        [Header("Color Options")]
        [SerializeField] private Button markerColorCyanBtn;
        [SerializeField] private Button markerColorMagentaBtn;
        [SerializeField] private Button markerColorAmberBtn;
        [SerializeField] private Button markerColorGreenBtn;

        [Header("Line Thickness")]
        [SerializeField] private Slider lineThicknessSlider;
        [SerializeField] private TextMeshProUGUI lineThicknessValueText;

        [Header("Toggles")]
        [SerializeField] private Toggle soundFxToggle;
        [SerializeField] private Toggle hapticToggle;
        [SerializeField] private Toggle planeVisualizationToggle;
        [SerializeField] private Toggle darkModeToggle;

        private void Awake()
        {
            SetupSliderLayout();
            SetupColorButtonsLayout();
            BindNavigation();
            BindControls();
        }

        private void OnEnable()
        {
            SetupSliderLayout();
            SetupColorButtonsLayout();
            LoadCurrentSettingsUI();
        }

        private void Start()
        {
            SetupSliderLayout();
            SetupColorButtonsLayout();
            LoadCurrentSettingsUI();
        }

        private void SetupSliderLayout()
        {
            if (lineThicknessSlider == null) return;

            RectTransform sliderRect = lineThicknessSlider.GetComponent<RectTransform>();
            if (sliderRect != null)
            {
                sliderRect.sizeDelta = new Vector2(800, 60);
                sliderRect.anchoredPosition = new Vector2(0, -35);
            }

            // Fix background track
            Image bgImg = lineThicknessSlider.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = new Color(0.18f, 0.22f, 0.3f, 1f);
            }

            // Fix Fill Area container & Fill image
            if (lineThicknessSlider.fillRect != null)
            {
                RectTransform fillRect = lineThicknessSlider.fillRect;
                RectTransform fillAreaRect = fillRect.parent as RectTransform;
                if (fillAreaRect != null)
                {
                    fillAreaRect.anchorMin = new Vector2(0, 0.5f);
                    fillAreaRect.anchorMax = new Vector2(1, 0.5f);
                    fillAreaRect.offsetMin = new Vector2(18f, -6f);
                    fillAreaRect.offsetMax = new Vector2(-18f, 6f);
                    fillAreaRect.sizeDelta = new Vector2(-36f, 12f);
                }

                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                Image fillImg = fillRect.GetComponent<Image>();
                if (fillImg != null && AppSettings.Instance != null)
                {
                    fillImg.color = AppSettings.Instance.ThemeColor;
                }
            }

            // Fix Handle Slide Area container & Handle image
            if (lineThicknessSlider.handleRect != null)
            {
                RectTransform handleRect = lineThicknessSlider.handleRect;
                RectTransform handleAreaRect = handleRect.parent as RectTransform;
                if (handleAreaRect != null)
                {
                    handleAreaRect.anchorMin = new Vector2(0, 0);
                    handleAreaRect.anchorMax = new Vector2(1, 1);
                    handleAreaRect.offsetMin = new Vector2(18f, 0);
                    handleAreaRect.offsetMax = new Vector2(-18f, 0);
                }

                handleRect.anchorMin = new Vector2(0, 0.5f);
                handleRect.anchorMax = new Vector2(0, 0.5f);
                handleRect.sizeDelta = new Vector2(36, 36);

                Image handleImg = handleRect.GetComponent<Image>();
                if (handleImg != null)
                {
                    handleImg.color = Color.white;
                    handleImg.raycastTarget = true;
                }
            }
        }

        private void BindNavigation()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.ReturnToHome();
                });
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.RemoveAllListeners();
                resetDefaultsButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayWarning();
                    HapticFeedback.TriggerWarning();
                    AppSettings.Instance?.ResetToDefaults();
                    LoadCurrentSettingsUI();
                    NotificationToastController.Instance?.ShowToast("Settings reset to defaults");
                });
            }
        }

        private void LoadCurrentSettingsUI()
        {
            if (AppSettings.Instance == null) return;

            // Unit Toggles
            if (metricToggle != null)
                metricToggle.SetIsOnWithoutNotify(AppSettings.Instance.Unit == MeasurementUnit.Metric);

            if (imperialToggle != null)
                imperialToggle.SetIsOnWithoutNotify(AppSettings.Instance.Unit == MeasurementUnit.Imperial);

            // Thickness Slider (1.0 mm to 8.0 mm)
            if (lineThicknessSlider != null)
            {
                lineThicknessSlider.minValue = 0.001f;
                lineThicknessSlider.maxValue = 0.008f;
                float currentThickness = AppSettings.Instance.LineThickness;

                // Force Unity Slider to recalculate UpdateVisuals() accurately for stored value
                lineThicknessSlider.SetValueWithoutNotify(-999f);
                lineThicknessSlider.SetValueWithoutNotify(currentThickness);
                UpdateThicknessText(currentThickness);
            }

            // Toggles
            if (soundFxToggle != null)
                soundFxToggle.SetIsOnWithoutNotify(AppSettings.Instance.SoundFX);

            if (hapticToggle != null)
                hapticToggle.SetIsOnWithoutNotify(AppSettings.Instance.HapticFeedback);

            if (planeVisualizationToggle != null)
                planeVisualizationToggle.SetIsOnWithoutNotify(AppSettings.Instance.ShowPlanes);

            if (darkModeToggle != null)
                darkModeToggle.SetIsOnWithoutNotify(AppSettings.Instance.DarkMode);

            // Update Active Color Visuals
            UpdateColorButtonsVisual(AppSettings.Instance.ThemeColor);
        }

        private void BindControls()
        {
            // Unit Toggles
            if (metricToggle != null)
            {
                metricToggle.onValueChanged.RemoveAllListeners();
                metricToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn && AppSettings.Instance != null)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerLight();
                        if (imperialToggle != null) imperialToggle.SetIsOnWithoutNotify(false);
                        AppSettings.Instance.Unit = MeasurementUnit.Metric;
                    }
                    else if (!isOn && imperialToggle != null && !imperialToggle.isOn)
                    {
                        metricToggle.SetIsOnWithoutNotify(true); // Don't allow unchecking both
                    }
                });
            }

            if (imperialToggle != null)
            {
                imperialToggle.onValueChanged.RemoveAllListeners();
                imperialToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn && AppSettings.Instance != null)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerLight();
                        if (metricToggle != null) metricToggle.SetIsOnWithoutNotify(false);
                        AppSettings.Instance.Unit = MeasurementUnit.Imperial;
                    }
                    else if (!isOn && metricToggle != null && !metricToggle.isOn)
                    {
                        imperialToggle.SetIsOnWithoutNotify(true); // Don't allow unchecking both
                    }
                });
            }

            // Marker & Line Colors
            if (markerColorCyanBtn != null)
            {
                markerColorCyanBtn.onClick.RemoveAllListeners();
                markerColorCyanBtn.onClick.AddListener(() => SetColor(AppSettings.THEME_CYAN));
            }

            if (markerColorMagentaBtn != null)
            {
                markerColorMagentaBtn.onClick.RemoveAllListeners();
                markerColorMagentaBtn.onClick.AddListener(() => SetColor(AppSettings.THEME_MAGENTA));
            }

            if (markerColorAmberBtn != null)
            {
                markerColorAmberBtn.onClick.RemoveAllListeners();
                markerColorAmberBtn.onClick.AddListener(() => SetColor(AppSettings.THEME_AMBER));
            }

            if (markerColorGreenBtn != null)
            {
                markerColorGreenBtn.onClick.RemoveAllListeners();
                markerColorGreenBtn.onClick.AddListener(() => SetColor(AppSettings.THEME_GREEN));
            }

            // Line Thickness Slider
            if (lineThicknessSlider != null)
            {
                lineThicknessSlider.onValueChanged.RemoveAllListeners();
                lineThicknessSlider.onValueChanged.AddListener((val) =>
                {
                    if (AppSettings.Instance != null)
                    {
                        AppSettings.Instance.LineThickness = val;
                        UpdateThicknessText(val);
                    }
                });
            }

            // Options Toggles
            if (soundFxToggle != null)
            {
                soundFxToggle.onValueChanged.RemoveAllListeners();
                soundFxToggle.onValueChanged.AddListener((val) =>
                {
                    if (AppSettings.Instance != null)
                    {
                        AppSettings.Instance.SoundFX = val;
                        if (val) AudioFeedback.PlayClick();
                    }
                });
            }

            if (hapticToggle != null)
            {
                hapticToggle.onValueChanged.RemoveAllListeners();
                hapticToggle.onValueChanged.AddListener((val) =>
                {
                    if (AppSettings.Instance != null)
                    {
                        AppSettings.Instance.HapticFeedback = val;
                        if (val) HapticFeedback.TriggerLight();
                    }
                });
            }

            if (planeVisualizationToggle != null)
            {
                planeVisualizationToggle.onValueChanged.RemoveAllListeners();
                planeVisualizationToggle.onValueChanged.AddListener((val) =>
                {
                    if (AppSettings.Instance != null)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerLight();
                        AppSettings.Instance.ShowPlanes = val;
                    }
                });
            }

            if (darkModeToggle != null)
            {
                darkModeToggle.onValueChanged.RemoveAllListeners();
                darkModeToggle.onValueChanged.AddListener((val) =>
                {
                    if (AppSettings.Instance != null)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerLight();
                        AppSettings.Instance.DarkMode = val;
                    }
                });
            }
        }

        private void SetColor(Color color)
        {
            AudioFeedback.PlayClick();
            HapticFeedback.TriggerLight();
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.SetThemeColor(color);
                UpdateColorButtonsVisual(color);
                NotificationToastController.Instance?.ShowToast("Marker & Line color updated");
            }
        }

        private void UpdateColorButtonsVisual(Color activeColor)
        {
            HighlightButton(markerColorCyanBtn, ColorsMatch(activeColor, AppSettings.THEME_CYAN));
            HighlightButton(markerColorMagentaBtn, ColorsMatch(activeColor, AppSettings.THEME_MAGENTA));
            HighlightButton(markerColorAmberBtn, ColorsMatch(activeColor, AppSettings.THEME_AMBER));
            HighlightButton(markerColorGreenBtn, ColorsMatch(activeColor, AppSettings.THEME_GREEN));

            // Sync Slider Fill Color with selected accent
            if (lineThicknessSlider != null && lineThicknessSlider.fillRect != null)
            {
                Image fillImg = lineThicknessSlider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = activeColor;
                }
            }
        }

        private bool ColorsMatch(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.1f && Mathf.Abs(a.g - b.g) < 0.1f && Mathf.Abs(a.b - b.b) < 0.1f;
        }

        private void HighlightButton(Button btn, bool isSelected)
        {
            if (btn == null) return;
            btn.transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;
        }

        private void UpdateThicknessText(float val)
        {
            if (lineThicknessValueText != null)
            {
                lineThicknessValueText.text = $"{val * 1000f:F1} mm";
            }
        }

        private void SetupColorButtonsLayout()
        {
            float buttonWidth = 190f;
            float buttonHeight = 100f;
            float gap = 20f;
            float step = buttonWidth + gap; // 210f
            float yPos = -30f;

            SetButtonPositionAndSize(markerColorCyanBtn,    -1.5f * step, yPos, buttonWidth, buttonHeight);
            SetButtonPositionAndSize(markerColorMagentaBtn, -0.5f * step, yPos, buttonWidth, buttonHeight);
            SetButtonPositionAndSize(markerColorAmberBtn,    0.5f * step, yPos, buttonWidth, buttonHeight);
            SetButtonPositionAndSize(markerColorGreenBtn,    1.5f * step, yPos, buttonWidth, buttonHeight);
        }

        private void SetButtonPositionAndSize(Button btn, float x, float y, float width, float height)
        {
            if (btn == null) return;
            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(width, height);
            }
        }
    }
}
