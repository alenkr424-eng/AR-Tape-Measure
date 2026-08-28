using System.Collections;
using SmartARMeasure.Models;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// Professional AR surface visualization controller attached to ARPlane prefabs.
    /// Implements subtle translucent ash gray surface rendering with fine dot distribution,
    /// dynamic fade-in on detection/expansion, and smooth fade-down once the surface is established.
    /// Responds immediately to AppSettings.ShowPlanes without interrupting AR tracking.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PlaneVisualizer : MonoBehaviour
    {
        private enum VisualState
        {
            Hidden,
            FadingIn,
            Holding,
            FadingOut
        }

        // Timing constants for smooth scanning feedback
        private const float FADE_IN_DURATION = 0.35f;   // Smooth appearance when newly detected
        private const float HOLD_DURATION = 2.0f;       // Visible while detecting/refining
        private const float FADE_OUT_DURATION = 1.2f;   // Graceful fade down once established
        private const float MIN_UPDATE_INTERVAL = 0.15f; // Debounce rapid boundary updates

        private ARPlane arPlane;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int AlphaPropId = Shader.PropertyToID("_AlphaMultiplier");

        private VisualState currentState = VisualState.Hidden;
        private float currentAlpha = 0f;
        private float holdTimer = 0f;
        private float lastBoundaryUpdateTime = 0f;
        private bool isSubscribed = false;

        private void Awake()
        {
            arPlane = GetComponent<ARPlane>();
            meshRenderer = GetComponent<MeshRenderer>();
            propertyBlock = new MaterialPropertyBlock();

            EnsureMaterialAssigned();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            ApplyInitialVisibility();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            currentState = VisualState.Hidden;
            currentAlpha = 0f;
            ApplyAlphaToRenderer(0f);
        }

        private void Start()
        {
            ApplyInitialVisibility();
        }

        private void SubscribeEvents()
        {
            if (isSubscribed) return;

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnPlaneVisualizationChanged += OnShowPlanesChanged;
            }

            if (arPlane != null)
            {
                arPlane.boundaryChanged += OnBoundaryChanged;
            }

            isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!isSubscribed) return;

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance.OnPlaneVisualizationChanged -= OnShowPlanesChanged;
            }

            if (arPlane != null)
            {
                arPlane.boundaryChanged -= OnBoundaryChanged;
            }

            isSubscribed = false;
        }

        private void EnsureMaterialAssigned()
        {
            if (meshRenderer == null) return;

            if (meshRenderer.sharedMaterial == null || !meshRenderer.sharedMaterial.shader.name.Contains("ARSurfaceDots"))
            {
                Shader dotShader = Shader.Find("SmartARMeasure/ARSurfaceDots");
                if (dotShader == null)
                {
                    dotShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
                }

                if (dotShader != null)
                {
                    Material mat = new Material(dotShader)
                    {
                        name = "M_ARSurfaceDots_Runtime"
                    };
                    meshRenderer.sharedMaterial = mat;
                }
            }
        }

        private void ApplyInitialVisibility()
        {
            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;

            if (!showPlanes)
            {
                currentState = VisualState.Hidden;
                currentAlpha = 0f;
                ApplyAlphaToRenderer(0f);
                if (meshRenderer != null) meshRenderer.enabled = false;
            }
            else
            {
                // Newly detected plane - smoothly fade in
                TriggerDetection();
            }
        }

        private void OnShowPlanesChanged(bool showPlanes)
        {
            if (!showPlanes)
            {
                currentState = VisualState.Hidden;
                currentAlpha = 0f;
                ApplyAlphaToRenderer(0f);
                if (meshRenderer != null) meshRenderer.enabled = false;
            }
            else
            {
                TriggerDetection();
            }
        }

        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
        {
            // Debounce boundary updates to avoid unnecessary per-frame restarts
            if (Time.time - lastBoundaryUpdateTime < MIN_UPDATE_INTERVAL)
            {
                // Refresh hold timer without interrupting ongoing fade
                holdTimer = HOLD_DURATION;
                return;
            }

            lastBoundaryUpdateTime = Time.time;

            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;
            if (showPlanes)
            {
                TriggerDetection();
            }
        }

        /// <summary>
        /// Triggers or refreshes the surface detection visualization cycle.
        /// </summary>
        public void TriggerDetection()
        {
            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;
            if (!showPlanes) return;

            if (meshRenderer != null && !meshRenderer.enabled)
            {
                meshRenderer.enabled = true;
            }

            holdTimer = HOLD_DURATION;

            if (currentState == VisualState.Holding || currentState == VisualState.FadingIn)
            {
                // Already visible, continue holding
                return;
            }

            currentState = VisualState.FadingIn;
        }

        private void Update()
        {
            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;
            if (!showPlanes)
            {
                if (meshRenderer != null && meshRenderer.enabled)
                {
                    meshRenderer.enabled = false;
                }
                return;
            }

            float dt = Time.deltaTime;

            switch (currentState)
            {
                case VisualState.FadingIn:
                    currentAlpha += dt / FADE_IN_DURATION;
                    if (currentAlpha >= 1.0f)
                    {
                        currentAlpha = 1.0f;
                        currentState = VisualState.Holding;
                    }
                    ApplyAlphaToRenderer(currentAlpha);
                    break;

                case VisualState.Holding:
                    holdTimer -= dt;
                    if (holdTimer <= 0f)
                    {
                        currentState = VisualState.FadingOut;
                    }
                    break;

                case VisualState.FadingOut:
                    currentAlpha -= dt / FADE_OUT_DURATION;
                    if (currentAlpha <= 0f)
                    {
                        currentAlpha = 0f;
                        currentState = VisualState.Hidden;
                        if (meshRenderer != null)
                        {
                            meshRenderer.enabled = false;
                        }
                    }
                    ApplyAlphaToRenderer(currentAlpha);
                    break;

                case VisualState.Hidden:
                    // Dormant until a new boundary expansion or re-scan triggers TriggerDetection
                    break;
            }
        }

        private void ApplyAlphaToRenderer(float alpha)
        {
            if (meshRenderer == null) return;

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(AlphaPropId, Mathf.Clamp01(alpha));
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// External interface called by ARManager for explicit visibility controls.
        /// </summary>
        public void SetVisibility(bool isVisible)
        {
            OnShowPlanesChanged(isVisible);
        }
    }
}
