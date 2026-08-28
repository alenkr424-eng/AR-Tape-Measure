using System;
using UnityEngine;

namespace SmartARMeasure.Models
{
    /// <summary>
    /// Manages persistent application settings using PlayerPrefs.
    /// Provides event notifications when settings change.
    /// </summary>
    public class AppSettings : MonoBehaviour
    {
        public static AppSettings Instance { get; private set; }

        // Settings Keys
        private const string KEY_UNIT = "Setting_MeasurementUnit";
        private const string KEY_METRIC_UNIT = "Setting_MetricDisplayUnit";
        private const string KEY_IMPERIAL_UNIT = "Setting_ImperialDisplayUnit";
        private const string KEY_THEME_COLOR = "Setting_ThemeColor";
        private const string KEY_MARKER_COLOR = "Setting_MarkerColor";
        private const string KEY_LINE_COLOR = "Setting_LineColor";
        private const string KEY_LINE_THICKNESS = "Setting_LineThickness";
        private const string KEY_SOUND_FX = "Setting_SoundFX";
        private const string KEY_HAPTIC = "Setting_HapticFeedback";
        private const string KEY_SHOW_PLANES = "Setting_ShowPlanes";
        private const string KEY_DARK_MODE = "Setting_DarkMode";
        private const string KEY_SMART_SNAP = "Setting_SmartSnap";

        // Curated Theme Colors (Material Design 3 High-Fidelity AR Accents)
        public static readonly Color THEME_CYAN = new Color(0f, 0.9f, 1f, 1f);     // Vibrant Cyan (#00E5FF)
        public static readonly Color THEME_MAGENTA = new Color(1f, 0.25f, 0.6f, 1f); // Vibrant Magenta (#FF4099)
        public static readonly Color THEME_AMBER = new Color(1f, 0.75f, 0f, 1f);     // Vibrant Amber (#FFC000)
        public static readonly Color THEME_GREEN = new Color(0f, 0.9f, 0.45f, 1f);   // Vibrant Emerald Green (#00E673)

        // Default Values
        public const MeasurementUnit DEFAULT_UNIT = MeasurementUnit.Metric;
        public const MetricDisplayUnit DEFAULT_METRIC_UNIT = MetricDisplayUnit.Meters;
        public const ImperialDisplayUnit DEFAULT_IMPERIAL_UNIT = ImperialDisplayUnit.Feet;
        public static readonly Color DEFAULT_THEME_COLOR = THEME_CYAN;
        public const float DEFAULT_LINE_THICKNESS = 0.0032f; // ~3.2mm thick lines in world space
        public const bool DEFAULT_SOUND = true;
        public const bool DEFAULT_HAPTIC = true;
        public const bool DEFAULT_SHOW_PLANES = true;
        public const bool DEFAULT_DARK_MODE = true;
        public const bool DEFAULT_SMART_SNAP = true;

        // Events
        public event Action<MeasurementUnit> OnUnitChanged;
        public event Action<MetricDisplayUnit> OnMetricDisplayUnitChanged;
        public event Action<ImperialDisplayUnit> OnImperialDisplayUnitChanged;
        public event Action<Color> OnThemeColorChanged;
        public event Action<Color> OnMarkerColorChanged;
        public event Action<Color> OnLineColorChanged;
        public event Action<float> OnLineThicknessChanged;
        public event Action<bool> OnPlaneVisualizationChanged;
        public event Action<bool> OnSmartSnapChanged;
        public event Action<bool> OnSoundFXChanged;
        public event Action<bool> OnHapticFeedbackChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Properties with PlayerPrefs persistence

        public MeasurementUnit Unit
        {
            get => (MeasurementUnit)PlayerPrefs.GetInt(KEY_UNIT, (int)DEFAULT_UNIT);
            set
            {
                PlayerPrefs.SetInt(KEY_UNIT, (int)value);
                PlayerPrefs.Save();
                OnUnitChanged?.Invoke(value);
            }
        }

        public MetricDisplayUnit MetricUnit
        {
            get => (MetricDisplayUnit)PlayerPrefs.GetInt(KEY_METRIC_UNIT, (int)DEFAULT_METRIC_UNIT);
            set
            {
                PlayerPrefs.SetInt(KEY_METRIC_UNIT, (int)value);
                PlayerPrefs.Save();
                OnMetricDisplayUnitChanged?.Invoke(value);
            }
        }

        public ImperialDisplayUnit ImperialUnit
        {
            get => (ImperialDisplayUnit)PlayerPrefs.GetInt(KEY_IMPERIAL_UNIT, (int)DEFAULT_IMPERIAL_UNIT);
            set
            {
                PlayerPrefs.SetInt(KEY_IMPERIAL_UNIT, (int)value);
                PlayerPrefs.Save();
                OnImperialDisplayUnitChanged?.Invoke(value);
            }
        }

        public void ToggleMetricUnit()
        {
            MetricUnit = (MetricUnit == MetricDisplayUnit.Meters)
                ? MetricDisplayUnit.Centimeters
                : MetricDisplayUnit.Meters;
        }

        public void ToggleImperialUnit()
        {
            ImperialUnit = (ImperialUnit == ImperialDisplayUnit.Feet)
                ? ImperialDisplayUnit.Inches
                : ImperialDisplayUnit.Feet;
        }

        public void ToggleDisplayUnit()
        {
            if (Unit == MeasurementUnit.Metric)
                ToggleMetricUnit();
            else
                ToggleImperialUnit();
        }

        public Color ThemeColor
        {
            get
            {
                string hex = PlayerPrefs.GetString(KEY_THEME_COLOR, ColorUtility.ToHtmlStringRGBA(DEFAULT_THEME_COLOR));
                if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                    return color;
                return DEFAULT_THEME_COLOR;
            }
            set
            {
                PlayerPrefs.SetString(KEY_THEME_COLOR, ColorUtility.ToHtmlStringRGBA(value));
                PlayerPrefs.SetString(KEY_MARKER_COLOR, ColorUtility.ToHtmlStringRGBA(value));
                PlayerPrefs.SetString(KEY_LINE_COLOR, ColorUtility.ToHtmlStringRGBA(value));
                PlayerPrefs.Save();

                OnThemeColorChanged?.Invoke(value);
                OnMarkerColorChanged?.Invoke(value);
                OnLineColorChanged?.Invoke(value);
            }
        }

        public Color MarkerColor
        {
            get => ThemeColor;
            set => ThemeColor = value;
        }

        public Color LineColor
        {
            get => ThemeColor;
            set => ThemeColor = value;
        }

        public void SetThemeColor(Color color)
        {
            ThemeColor = color;
        }

        public float LineThickness
        {
            get => PlayerPrefs.GetFloat(KEY_LINE_THICKNESS, DEFAULT_LINE_THICKNESS);
            set
            {
                float clamped = Mathf.Clamp(value, 0.001f, 0.015f);
                PlayerPrefs.SetFloat(KEY_LINE_THICKNESS, clamped);
                PlayerPrefs.Save();
                OnLineThicknessChanged?.Invoke(clamped);
            }
        }

        public bool SoundFX
        {
            get => PlayerPrefs.GetInt(KEY_SOUND_FX, DEFAULT_SOUND ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KEY_SOUND_FX, value ? 1 : 0);
                PlayerPrefs.Save();
                OnSoundFXChanged?.Invoke(value);
            }
        }

        public bool HapticFeedback
        {
            get => PlayerPrefs.GetInt(KEY_HAPTIC, DEFAULT_HAPTIC ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KEY_HAPTIC, value ? 1 : 0);
                PlayerPrefs.Save();
                OnHapticFeedbackChanged?.Invoke(value);
            }
        }

        public bool ShowPlanes
        {
            get => PlayerPrefs.GetInt(KEY_SHOW_PLANES, DEFAULT_SHOW_PLANES ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KEY_SHOW_PLANES, value ? 1 : 0);
                PlayerPrefs.Save();
                OnPlaneVisualizationChanged?.Invoke(value);
            }
        }

        public bool DarkMode
        {
            get => PlayerPrefs.GetInt(KEY_DARK_MODE, DEFAULT_DARK_MODE ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KEY_DARK_MODE, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool SmartSnap
        {
            get => PlayerPrefs.GetInt(KEY_SMART_SNAP, DEFAULT_SMART_SNAP ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KEY_SMART_SNAP, value ? 1 : 0);
                PlayerPrefs.Save();
                OnSmartSnapChanged?.Invoke(value);
            }
        }

        #endregion

        /// <summary>
        /// Resets all settings to factory default.
        /// </summary>
        public void ResetToDefaults()
        {
            Unit = DEFAULT_UNIT;
            MetricUnit = DEFAULT_METRIC_UNIT;
            ImperialUnit = DEFAULT_IMPERIAL_UNIT;
            SetThemeColor(DEFAULT_THEME_COLOR);
            LineThickness = DEFAULT_LINE_THICKNESS;
            SoundFX = DEFAULT_SOUND;
            HapticFeedback = DEFAULT_HAPTIC;
            ShowPlanes = DEFAULT_SHOW_PLANES;
            DarkMode = DEFAULT_DARK_MODE;
            SmartSnap = DEFAULT_SMART_SNAP;
        }
    }
}
