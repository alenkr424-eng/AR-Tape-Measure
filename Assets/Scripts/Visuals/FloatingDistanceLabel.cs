using SmartARMeasure.Models;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// World-space billboard label hovering above measurement lines.
    /// Displays distance formatted in metric/imperial units and faces the active AR Camera.
    /// </summary>
    public class FloatingDistanceLabel : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image cardBackground;

        [Header("Billboard Scaling")]
        [SerializeField] private float baseScale = 0.002f;
        [SerializeField] private float minDistanceScale = 0.5f;
        [SerializeField] private float maxDistanceScale = 3.0f;

        private Transform mainCameraTransform;

        private void Awake()
        {
            if (Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }

            // Ensure Canvas is set to World Space
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
            }

            // Material 3 Dark theme card styling
            if (cardBackground != null)
            {
                cardBackground.color = new Color(0.08f, 0.09f, 0.12f, 0.85f); // Glassmorphism Dark
            }
        }

        private void LateUpdate()
        {
            if (mainCameraTransform == null)
            {
                if (Camera.main != null)
                {
                    mainCameraTransform = Camera.main.transform;
                }
                return;
            }

            // Billboard effect: Rotate towards camera
            transform.rotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);

            // Scale based on camera distance so text remains legible
            float distanceToCamera = Vector3.Distance(transform.position, mainCameraTransform.position);
            float scaleFactor = Mathf.Clamp(distanceToCamera, minDistanceScale, maxDistanceScale);
            transform.localScale = Vector3.one * (baseScale * scaleFactor);
        }

        /// <summary>
        /// Updates the displayed distance string and world space placement.
        /// </summary>
        public void SetDistance(float distanceInMeters, Vector3 worldPosition, MeasurementUnit unitSystem)
        {
            transform.position = worldPosition;

            MetricDisplayUnit metricUnit = AppSettings.Instance != null ? AppSettings.Instance.MetricUnit : MetricDisplayUnit.Meters;
            ImperialDisplayUnit imperialUnit = AppSettings.Instance != null ? AppSettings.Instance.ImperialUnit : ImperialDisplayUnit.Feet;
            string formattedString = UnitConverter.FormatDistance(distanceInMeters, unitSystem, metricUnit, imperialUnit);
            if (distanceText != null)
            {
                distanceText.text = formattedString;
            }
        }

        /// <summary>
        /// Directly sets custom label text (e.g. Area or Height readout).
        /// </summary>
        public void SetCustomText(string text, Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (distanceText != null)
            {
                distanceText.text = text;
            }
        }
    }
}
