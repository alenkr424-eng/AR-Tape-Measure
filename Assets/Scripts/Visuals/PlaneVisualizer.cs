using System.Collections;
using SmartARMeasure.Models;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SmartARMeasure.Visuals
{
    /// <summary>
    /// Professional AR surface visualization controller attached to ARPlane prefabs.
    /// Implements reference-style subtle ash-gray translucent surface with tiny white dots,
    /// dynamic fade-in on detection/expansion, hold during scanning/refinement, and smooth fade-out.
    /// Re-appears naturally when the user moves into newly scanned areas or expands plane boundaries.
    /// Operates purely visually; AR tracking and raycasting measurements remain uninterrupted.
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

        // Timing constants for scanning feedback
        private const float FADE_IN_DURATION = 0.35f;    // Smooth appearance when newly detected/expanded
        private const float HOLD_DURATION = 2.0f;        // Visible during detection/refinement
        private const float FADE_OUT_DURATION = 1.0f;    // Graceful fade down once surface is established
        private const float MIN_SIZE_CHANGE_RETRIGGER = 0.12f; // Retrigger if plane expands by > 12cm
        private const float RETRIGGER_COOLDOWN = 1.2f;   // Cooldown between movement/expansion retriggers

        private ARPlane arPlane;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int AlphaPropId = Shader.PropertyToID("_AlphaMultiplier");

        private VisualState currentState = VisualState.Hidden;
        private float currentAlpha = 0f;
        private float holdTimer = 0f;
        private float lastTriggerTime = 0f;
        private Vector2 lastTriggerSize = Vector2.zero;
        private bool isSubscribed = false;

        private void Awake()
        {
            arPlane = GetComponent<ARPlane>();
            meshFilter = GetComponent<MeshFilter>();
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
            EnsureMaterialAssigned();
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

        public void EnsureMaterialAssigned()
        {
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (meshRenderer == null) return;

            // Ensure custom ARSurfaceDots shader is used
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
                // Trigger smooth scanning fade-in for newly detected plane
                TriggerScanFeedback(true);
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
                TriggerScanFeedback(true);
            }
        }

        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
        {
            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;
            if (!showPlanes || arPlane == null) return;

            Vector2 currentSize = arPlane.size;
            float sizeDelta = Vector2.Distance(currentSize, lastTriggerSize);
            float timeSinceLast = Time.time - lastTriggerTime;

            // If plane is currently holding or fading in, refresh hold timer during continuous scanning
            if (currentState == VisualState.Holding || currentState == VisualState.FadingIn)
            {
                holdTimer = HOLD_DURATION;
                lastTriggerSize = currentSize;
                return;
            }

            // If plane has finished fading or is hidden, retrigger if the user moved and expanded the plane
            if (sizeDelta >= MIN_SIZE_CHANGE_RETRIGGER || timeSinceLast >= RETRIGGER_COOLDOWN)
            {
                TriggerScanFeedback(false);
            }
        }

        /// <summary>
        /// Triggers or refreshes the temporary scanning surface visualization cycle.
        /// </summary>
        public void TriggerScanFeedback(bool forceImmediate)
        {
            bool showPlanes = AppSettings.Instance != null ? AppSettings.Instance.ShowPlanes : AppSettings.DEFAULT_SHOW_PLANES;
            if (!showPlanes) return;

            if (meshRenderer != null && !meshRenderer.enabled)
            {
                meshRenderer.enabled = true;
            }

            holdTimer = HOLD_DURATION;
            lastTriggerTime = Time.time;
            if (arPlane != null)
            {
                lastTriggerSize = arPlane.size;
            }

            if (currentState == VisualState.Holding || currentState == VisualState.FadingIn)
            {
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
                    if (meshRenderer != null && !meshRenderer.enabled)
                    {
                        meshRenderer.enabled = true;
                    }

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
                    // Dormant until a new boundary expansion or re-scan triggers TriggerScanFeedback
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
