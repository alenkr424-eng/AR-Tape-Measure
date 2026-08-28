using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// Smooth anti-aliased 3D line renderer connecting AR markers.
    /// Dynamically updates points, thickness, color, and attaches a floating text label at midpoint.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class MeasurementLine : MonoBehaviour
    {
        [Header("Line Setup")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private FloatingDistanceLabel labelPrefab;

        private FloatingDistanceLabel activeLabel;
        private Vector3 startPoint;
        private Vector3 endPoint;
        private float currentDistanceMeters;

        public Vector3 StartPoint => startPoint;
        public Vector3 EndPoint => endPoint;
        public float DistanceMeters => currentDistanceMeters;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            ConfigureLineRenderer();
        }

        private bool isAppSettingsSubscribed = false;

        private void OnEnable()
        {
            SubscribeAppSettings();
        }

        private void OnDisable()
        {
            UnsubscribeAppSettings();
        }

        private void Start()
        {
            SubscribeAppSettings();
        }

        private void SubscribeAppSettings()
        {
            if (isAppSettingsSubscribed) return;

            var settings = AppSettings.Instance ?? UnityEngine.Object.FindAnyObjectByType<AppSettings>();
            if (settings != null)
            {
                UpdateLineColor(settings.ThemeColor);
                UpdateLineThickness(settings.LineThickness);

                settings.OnThemeColorChanged += UpdateLineColor;
                settings.OnLineColorChanged += UpdateLineColor;
                settings.OnLineThicknessChanged += UpdateLineThickness;
                settings.OnUnitChanged += OnUnitSystemChanged;
                settings.OnMetricDisplayUnitChanged += OnMetricDisplayUnitChanged;
                settings.OnImperialDisplayUnitChanged += OnImperialDisplayUnitChanged;

                isAppSettingsSubscribed = true;
            }
        }

        private void UnsubscribeAppSettings()
        {
            if (!isAppSettingsSubscribed) return;

            var settings = AppSettings.Instance ?? UnityEngine.Object.FindAnyObjectByType<AppSettings>();
            if (settings != null)
            {
                settings.OnThemeColorChanged -= UpdateLineColor;
                settings.OnLineColorChanged -= UpdateLineColor;
                settings.OnLineThicknessChanged -= UpdateLineThickness;
                settings.OnUnitChanged -= OnUnitSystemChanged;
                settings.OnMetricDisplayUnitChanged -= OnMetricDisplayUnitChanged;
                settings.OnImperialDisplayUnitChanged -= OnImperialDisplayUnitChanged;
            }

            isAppSettingsSubscribed = false;
        }

        /// <summary>
        /// Initializes the line renderer with anti-aliasing materials and round caps.
        /// </summary>
        private void ConfigureLineRenderer()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 8;
            lineRenderer.numCornerVertices = 8;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Tile;

            // Apply default settings
            if (AppSettings.Instance != null)
            {
                UpdateLineColor(AppSettings.Instance.LineColor);
                UpdateLineThickness(AppSettings.Instance.LineThickness);
            }
            else
            {
                UpdateLineColor(new Color(0f, 0.75f, 1f, 0.95f));
                UpdateLineThickness(0.0032f);
            }

            if (lineRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(shader);
                lineRenderer.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// Updates start and end positions of the line in 3D world space.
        /// Re-calculates distance and updates the floating text label position.
        /// </summary>
        public void SetPoints(Vector3 start, Vector3 end, FloatingDistanceLabel labelInstance = null)
        {
            startPoint = start;
            endPoint = end;

            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

            currentDistanceMeters = Vector3.Distance(startPoint, endPoint);

            if (labelInstance != null && activeLabel == null)
            {
                activeLabel = labelInstance;
            }

            UpdateLabelPositionAndText();
        }

        /// <summary>
        /// Updates the end position dynamically during real-time placement targeting.
        /// </summary>
        public void UpdateTargetPoint(Vector3 end)
        {
            SetPoints(startPoint, end, activeLabel);
        }

        /// <summary>
        /// Attaches or binds a floating distance label.
        /// </summary>
        public void AttachLabel(FloatingDistanceLabel label)
        {
            activeLabel = label;
            UpdateLabelPositionAndText();
        }

        private void UpdateLabelPositionAndText()
        {
            if (activeLabel == null) return;

            Vector3 midPoint = (startPoint + endPoint) * 0.5f;
            // Float label slightly above line center
            Vector3 labelPosition = midPoint + Vector3.up * 0.04f;

            MeasurementUnit unit = AppSettings.Instance != null ? AppSettings.Instance.Unit : MeasurementUnit.Metric;
            activeLabel.SetDistance(currentDistanceMeters, labelPosition, unit);
        }

        public void UpdateLineColor(Color color)
        {
            if (lineRenderer == null) return;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            if (lineRenderer.sharedMaterial != null)
            {
                lineRenderer.sharedMaterial.color = color;
            }
        }

        public void UpdateLineThickness(float thickness)
        {
            if (lineRenderer == null) return;
            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
        }

        private void OnUnitSystemChanged(MeasurementUnit newUnit)
        {
            UpdateLabelPositionAndText();
        }

        private void OnMetricDisplayUnitChanged(MetricDisplayUnit newMetricUnit)
        {
            UpdateLabelPositionAndText();
        }

        private void OnImperialDisplayUnitChanged(ImperialDisplayUnit newImperialUnit)
        {
            UpdateLabelPositionAndText();
        }

        private void OnDestroy()
        {
            if (activeLabel != null)
            {
                Destroy(activeLabel.gameObject);
            }
        }
    }
}
