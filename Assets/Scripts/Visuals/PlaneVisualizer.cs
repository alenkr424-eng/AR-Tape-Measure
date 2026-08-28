using SmartARMeasure.Models;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// Custom plane visualization controller attached to ARPlane prefabs.
    /// Manages grid texture tinting according to horizontal vs vertical plane classification and toggle settings.
    /// </summary>

    [RequireComponent(typeof(ARPlane), typeof(MeshRenderer))]
    public class PlaneVisualizer : MonoBehaviour
    {
        [Header("Plane Aesthetics")]
        [SerializeField] private Color horizontalPlaneColor = new Color(0f, 0.9f, 1f, 0.25f); // Soft Cyan
        [SerializeField] private Color verticalPlaneColor = new Color(1f, 0.76f, 0.03f, 0.25f); // Soft Amber

        private ARPlane arPlane;
        private MeshRenderer meshRenderer;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            arPlane = GetComponent<ARPlane>();
            meshRenderer = GetComponent<MeshRenderer>();
            lineRenderer = GetComponent<LineRenderer>();

            ApplyMaterialSettings();
        }

        private void OnEnable()
        {
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnPlaneVisualizationChanged += SetVisibility;
                SetVisibility(AppSettings.Instance.ShowPlanes);
            }

            if (arPlane != null)
            {
                arPlane.boundaryChanged += OnBoundaryChanged;
            }
        }

        private void OnDisable()
        {
            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnPlaneVisualizationChanged -= SetVisibility;
            }

            if (arPlane != null)
            {
                arPlane.boundaryChanged -= OnBoundaryChanged;
            }
        }

        private void Start()
        {
            UpdatePlaneColor();
        }

        private void ApplyMaterialSettings()
        {
            if (meshRenderer != null && meshRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
                Material mat = new Material(shader);
                meshRenderer.sharedMaterial = mat;
            }
        }

        private void UpdatePlaneColor()
        {
            if (arPlane == null || meshRenderer == null) return;

            Color targetColor = horizontalPlaneColor;

            if (arPlane.alignment == PlaneAlignment.Vertical)
            {
                targetColor = verticalPlaneColor;
            }

            if (meshRenderer.material != null)
            {
                meshRenderer.material.color = targetColor;
            }
        }

        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
        {
            UpdatePlaneColor();
        }

        /// <summary>
        /// Toggles visibility of plane mesh and line boundary renderer.
        /// </summary>
        public void SetVisibility(bool isVisible)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isVisible;
            }
            if (lineRenderer != null)
            {
                lineRenderer.enabled = isVisible;
            }
        }
    }
}
