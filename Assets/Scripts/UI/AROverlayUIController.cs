using SmartARMeasure.Core;
using SmartARMeasure.Models;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// AR Measurement View overlay UI controller.
    /// Manages Top Toolbar, Bottom Mode Toolbar, live measurement HUD cards, and tracking state status banners.
    /// </summary>
    public class AROverlayUIController : MonoBehaviour
    {
        public static AROverlayUIController Instance { get; private set; }

        [Header("Top Toolbar Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button screenshotButton;
        [SerializeField] private Button historyButton;
        [SerializeField] private Button settingsButton;

        [Header("Bottom Toolbar Mode Buttons")]
        [SerializeField] private Button distanceModeButton;
        [SerializeField] private Button areaModeButton;
        [SerializeField] private Button heightModeButton;
        [SerializeField] private Button bottomSettingsButton;

        [Header("HUD Display Cards")]
        [SerializeField] private TextMeshProUGUI liveDistanceHUDText;
        [SerializeField] private TextMeshProUGUI unitLabelText;
        [SerializeField] private Button unitLabelButton;
        [SerializeField] private TextMeshProUGUI modeTitleHUDText;
        [SerializeField] private GameObject trackingHintBanner;
        [SerializeField] private TextMeshProUGUI trackingHintText;

        [Header("UI Overlay Canvas")]
        [SerializeField] private Canvas uiCanvas;

        private static readonly Color NORMAL_MODE_COLOR = new Color(0.18f, 0.22f, 0.28f, 1f);

        private float lastDistanceInMeters = 0f;
        private MetricDisplayUnit fallbackMetricUnit = MetricDisplayUnit.Meters;
        private ImperialDisplayUnit fallbackImperialUnit = ImperialDisplayUnit.Feet;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupUnitLabelButton();
            BindTopToolbar();
            BindBottomToolbar();
        }

        private void Start()
        {
            SetupUnitLabelButton();
            BindTopToolbar();
            BindBottomToolbar();
            RegisterEvents();
            UpdateModeButtonsVisual(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentMode : MeasurementMode.Distance);
            UpdateHUDDistance(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentValue : 0f);
        }

        private void SetupUnitLabelButton()
        {
            if (unitLabelText == null)
            {
                var allTmp = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in allTmp)
                {
                    if (tmp.gameObject.name == "UnitLabel")
                    {
                        unitLabelText = tmp;
                        break;
                    }
                }
            }

            if (unitLabelText != null)
            {
                unitLabelText.raycastTarget = true;
                if (unitLabelButton == null)
                {
                    unitLabelButton = unitLabelText.GetComponent<Button>() ?? unitLabelText.gameObject.AddComponent<Button>();
                    unitLabelButton.transition = Selectable.Transition.None;
                }
                unitLabelButton.onClick.RemoveAllListeners();
                unitLabelButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    ToggleUnit();
                });
            }
        }

        private void ToggleUnit()
        {
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.ToggleDisplayUnit();
            }
            else
            {
                MeasurementUnit currentSys = AppSettings.Instance != null ? AppSettings.Instance.Unit : MeasurementUnit.Metric;
                if (currentSys == MeasurementUnit.Metric)
                {
                    fallbackMetricUnit = (fallbackMetricUnit == MetricDisplayUnit.Meters)
                        ? MetricDisplayUnit.Centimeters
                        : MetricDisplayUnit.Meters;
                }
                else
                {
                    fallbackImperialUnit = (fallbackImperialUnit == ImperialDisplayUnit.Feet)
                        ? ImperialDisplayUnit.Inches
                        : ImperialDisplayUnit.Feet;
                }
                UpdateHUDDistance(lastDistanceInMeters);
            }
        }

        private void OnEnable()
        {
            SetupUnitLabelButton();
            BindTopToolbar();
            BindBottomToolbar();
            RegisterEvents();
            UpdateModeButtonsVisual(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentMode : MeasurementMode.Distance);
            UpdateHUDDistance(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentValue : 0f);
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            UnregisterEvents(); // Prevent duplicate registration

            if (MeasurementManager.Instance != null)
            {
                MeasurementManager.Instance.OnDistanceUpdated += UpdateHUDDistance;
                MeasurementManager.Instance.OnModeChanged += UpdateHUDModeTitle;
            }

            if (ARManager.Instance != null)
            {
                ARManager.Instance.OnTrackingStateChanged += UpdateTrackingStatusHint;
            }

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnUnitChanged += OnUnitSystemChanged;
                AppSettings.Instance.OnMetricDisplayUnitChanged += OnMetricUnitChanged;
                AppSettings.Instance.OnImperialDisplayUnitChanged += OnImperialUnitChanged;
                AppSettings.Instance.OnThemeColorChanged += OnThemeColorChanged;
            }
        }

        private void UnregisterEvents()
        {
            if (MeasurementManager.Instance != null)
            {
                MeasurementManager.Instance.OnDistanceUpdated -= UpdateHUDDistance;
                MeasurementManager.Instance.OnModeChanged -= UpdateHUDModeTitle;
            }

            if (ARManager.Instance != null)
            {
                ARManager.Instance.OnTrackingStateChanged -= UpdateTrackingStatusHint;
            }

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnUnitChanged -= OnUnitSystemChanged;
                AppSettings.Instance.OnMetricDisplayUnitChanged -= OnMetricUnitChanged;
                AppSettings.Instance.OnImperialDisplayUnitChanged -= OnImperialUnitChanged;
                AppSettings.Instance.OnThemeColorChanged -= OnThemeColorChanged;
            }
        }

        private void BindTopToolbar()
        {
            if (homeButton != null)
            {
                homeButton.onClick.RemoveAllListeners();
                homeButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.ReturnToHome();
                });
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayWarning();
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.ResetActiveMeasurement();
                });
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.SaveCurrentMeasurement();
                });
            }

            if (undoButton != null)
            {
                undoButton.onClick.RemoveAllListeners();
                undoButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    Debug.Log("[AROverlayUI] Undo button tapped.");
                    MeasurementManager.Instance?.UndoLastPoint();
                });
            }

            if (screenshotButton != null)
            {
                screenshotButton.onClick.RemoveAllListeners();
                screenshotButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    ScreenshotUtility.Instance?.CaptureScreenshot(uiCanvas);
                });
            }

            if (historyButton != null)
            {
                historyButton.onClick.RemoveAllListeners();
                historyButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenHistory();
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenSettings();
                });
            }
        }

        private void BindBottomToolbar()
        {
            if (distanceModeButton != null)
            {
                distanceModeButton.onClick.RemoveAllListeners();
                distanceModeButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.SetMeasurementMode(MeasurementMode.Distance);
                    NotificationToastController.Instance?.ShowToast("Distance Mode Active");
                });
            }

            if (areaModeButton != null)
            {
                areaModeButton.onClick.RemoveAllListeners();
                areaModeButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.SetMeasurementMode(MeasurementMode.Area);
                    NotificationToastController.Instance?.ShowToast("Area Mode Active");
                });
            }

            if (heightModeButton != null)
            {
                heightModeButton.onClick.RemoveAllListeners();
                heightModeButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.SetMeasurementMode(MeasurementMode.Height);
                    NotificationToastController.Instance?.ShowToast("Height Mode Active");
                });
            }

            if (bottomSettingsButton != null)
            {
                bottomSettingsButton.onClick.RemoveAllListeners();
                bottomSettingsButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.OpenSettings();
                });
            }
        }

        private void UpdateHUDDistance(float distanceInMeters)
        {
            lastDistanceInMeters = distanceInMeters;
            if (liveDistanceHUDText == null) return;

            MeasurementUnit unit = AppSettings.Instance != null ? AppSettings.Instance.Unit : MeasurementUnit.Metric;
            MetricDisplayUnit metricUnit = AppSettings.Instance != null ? AppSettings.Instance.MetricUnit : fallbackMetricUnit;
            ImperialDisplayUnit imperialUnit = AppSettings.Instance != null ? AppSettings.Instance.ImperialUnit : fallbackImperialUnit;
            MeasurementMode mode = MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentMode : MeasurementMode.Distance;

            var (valStr, unitStr) = UnitConverter.FormatValueAndUnit(distanceInMeters, mode, unit, metricUnit, imperialUnit);

            liveDistanceHUDText.text = valStr;

            if (unitLabelText != null)
            {
                unitLabelText.text = unitStr;
            }
            else
            {
                liveDistanceHUDText.text = $"{valStr} {unitStr}";
            }
        }

        private void UpdateHUDModeTitle(MeasurementMode mode)
        {
            if (modeTitleHUDText != null)
                modeTitleHUDText.text = $"{mode} Mode";

            UpdateModeButtonsVisual(mode);
            UpdateHUDDistance(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentValue : 0f);
        }

        private void UpdateModeButtonsVisual(MeasurementMode activeMode)
        {
            Color selectedColor = AppSettings.Instance != null ? AppSettings.Instance.ThemeColor : new Color(0f, 0.6f, 0.8f, 1f);

            SetButtonBackgroundColor(distanceModeButton, activeMode == MeasurementMode.Distance ? selectedColor : NORMAL_MODE_COLOR);
            SetButtonBackgroundColor(areaModeButton, activeMode == MeasurementMode.Area ? selectedColor : NORMAL_MODE_COLOR);
            SetButtonBackgroundColor(heightModeButton, activeMode == MeasurementMode.Height ? selectedColor : NORMAL_MODE_COLOR);
        }

        private void SetButtonBackgroundColor(Button btn, Color color)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }

        private void UpdateTrackingStatusHint(ARTrackingState state)
        {
            if (trackingHintBanner == null || trackingHintText == null) return;

            switch (state)
            {
                case ARTrackingState.SearchingForPlanes:
                    trackingHintBanner.SetActive(true);
                    trackingHintText.text = "Point camera at surfaces and move slowly to scan area...";
                    break;

                case ARTrackingState.TrackingActive:
                    trackingHintBanner.SetActive(true);
                    trackingHintText.text = "Tap on surface to place points";
                    // Hide hint after 3 seconds of continuous active tracking
                    Invoke(nameof(HideTrackingHint), 3.5f);
                    break;

                case ARTrackingState.Limited:
                    trackingHintBanner.SetActive(true);
                    trackingHintText.text = "Low light or fast motion. Move device slowly.";
                    break;

                case ARTrackingState.Error:
                    trackingHintBanner.SetActive(true);
                    trackingHintText.text = "AR core unavailable or camera permission missing.";
                    break;
            }
        }

        private void HideTrackingHint()
        {
            if (trackingHintBanner != null && ARManager.Instance != null && ARManager.Instance.CurrentState == ARTrackingState.TrackingActive)
            {
                trackingHintBanner.SetActive(false);
            }
        }

        public void ShowCustomHint(string text, float duration = 3.5f)
        {
            if (trackingHintBanner == null || trackingHintText == null) return;
            
            CancelInvoke(nameof(HideCustomHint));
            CancelInvoke(nameof(HideTrackingHint));

            trackingHintBanner.SetActive(true);
            trackingHintText.text = text;
            
            if (duration > 0f)
            {
                Invoke(nameof(HideCustomHint), duration);
            }
        }

        private void HideCustomHint()
        {
            if (trackingHintBanner != null)
            {
                trackingHintBanner.SetActive(false);
            }
            // Restore AR tracking state if needed
            if (ARManager.Instance != null)
            {
                UpdateTrackingStatusHint(ARManager.Instance.CurrentState);
            }
        }

        private void OnUnitSystemChanged(MeasurementUnit newUnit)
        {
            UpdateHUDDistance(lastDistanceInMeters);
        }

        private void OnMetricUnitChanged(MetricDisplayUnit newMetricUnit)
        {
            UpdateHUDDistance(lastDistanceInMeters);
        }

        private void OnImperialUnitChanged(ImperialDisplayUnit newImperialUnit)
        {
            UpdateHUDDistance(lastDistanceInMeters);
        }

        private void OnThemeColorChanged(Color newColor)
        {
            UpdateModeButtonsVisual(MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentMode : MeasurementMode.Distance);
            if (unitLabelText != null)
            {
                unitLabelText.color = newColor;
            }
        }
    }
}
