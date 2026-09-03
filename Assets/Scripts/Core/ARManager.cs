using System;
using System.Collections.Generic;
using SmartARMeasure.Models;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SmartARMeasure.Core
{
    public enum ARTrackingState
    {
        Initializing,
        SearchingForPlanes,
        TrackingActive,
        Limited,
        Error
    }

    public class ARManager : MonoBehaviour
    {
        // ============================================================
        // SINGLETON
        // ============================================================

        public static ARManager Instance { get; private set; }

        // ============================================================
        // AR COMPONENTS
        // ============================================================

        [Header("AR Foundation 6.x Components")]

        [SerializeField]
        private ARSession arSession;

        [SerializeField]
        private XROrigin xrOrigin;

        [SerializeField]
        private ARPlaneManager planeManager;

        [SerializeField]
        private ARRaycastManager raycastManager;

        [SerializeField]
        private ARCameraManager cameraManager;

        [SerializeField]
        private ARCameraBackground cameraBackground;

        [SerializeField]
        private ARAnchorManager anchorManager;

        // ============================================================
        // OLD RETICLE
        // ============================================================

        [Header("Old Reticle")]

        [SerializeField]
        private GameObject reticleObject;

        // ============================================================
        // GESTURE & PINCH ZOOM STATE
        // ============================================================

        private enum GestureState
        {
            None,
            PossibleTap,
            PinchZoom,
            PinchLockedEnding,
            DragIgnored,
            MarkerDragging
        }

        [Header("Pinch Zoom Settings")]
        [SerializeField] private float minZoom = 1.0f;
        [SerializeField] private float maxZoom = 3.5f;
        [SerializeField] private float zoomSensitivity = 0.0035f;
        [SerializeField] private float zoomSmoothing = 15f;

        private GestureState currentGestureState = GestureState.None;
        private Vector2 tapStartPosition;
        private float tapStartTime;
        private int tapFingerId = -1;
        private float lastPinchDistance = 0f;
        private const float MAX_TAP_MOVEMENT = 30f; // Max pixel drift allowed for stationary tap
        private const float MAX_TAP_DURATION = 0.55f; // Max duration (seconds) for tap gesture
        
        private int draggedMarkerIndex = -1;

        private float targetZoom = 1.0f;
        private float currentZoom = 1.0f;
        private Matrix4x4 baseProjMatrix = Matrix4x4.identity;
        private float baseFOV = 60f;
        private bool hasBaseProjection = false;

        // ============================================================
        // RAYCAST
        // ============================================================

        private Pose lastHitPose;
        private bool hasValidHitPose;
        private bool hasReceivedFrame;

        private static readonly List<ARRaycastHit> s_Hits =
            new List<ARRaycastHit>();

        // ============================================================
        // TRACKING
        // ============================================================

        private ARTrackingState currentState =
            ARTrackingState.Initializing;

        // ============================================================
        // EVENTS
        // ============================================================

        public event Action<ARTrackingState>
            OnTrackingStateChanged;

        public event Action<Pose, bool>
            OnReticleUpdated;

        // ============================================================
        // PROPERTIES
        // ============================================================

        public ARSession Session => arSession;

        public XROrigin Origin => xrOrigin;

        public ARPlaneManager PlaneManager => planeManager;

        public ARRaycastManager RaycastManager => raycastManager;

        public ARCameraBackground CameraBackground =>
            cameraBackground;

        public ARAnchorManager AnchorManager =>
            anchorManager;

        public Camera ARCamera =>
            xrOrigin != null
                ? xrOrigin.Camera
                : Camera.main;

        public bool HasValidHitPose =>
            hasValidHitPose;

        public Pose LastHitPose =>
            lastHitPose;

        public ARTrackingState CurrentState =>
            currentState;

        // ============================================================
        // AWAKE & START
        // ============================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            ValidateComponents();

            SetReticleVisible(false);

            Debug.Log(
                "[ARManager] Awake - initialized.");
        }

        private void Start()
        {
            LogDiagnostics();
        }

        private void LogDiagnostics()
        {
            Debug.Log("==================== [AR FOUNDATION DIAGNOSTICS] ====================");
            Debug.Log($"* AR Session found: {(arSession != null ? "YES" : "NO")}");
            Debug.Log($"* AR Session running: {(arSession != null && arSession.enabled ? "YES" : "NO")}");
            Debug.Log($"* Tracking state: {ARSession.state}");
            Debug.Log($"* ARPlaneManager found: {(planeManager != null ? "YES" : "NO")}");
            if (planeManager != null)
            {
                Debug.Log($"* Enabled: {planeManager.enabled}");
                Debug.Log($"* Plane Detection Mode: {planeManager.requestedDetectionMode}");
                Debug.Log($"* Plane Prefab assigned: {(planeManager.planePrefab != null ? "YES" : "NO")}");
                Debug.Log($"* Tracked Plane Count: {planeManager.trackables.count}");
            }
            if (AppSettings.Instance != null)
            {
                Debug.Log($"* Show Surfaces setting: {(AppSettings.Instance.ShowPlanes ? "ON" : "OFF")}");
            }
            Debug.Log("=====================================================================");
        }

        // ============================================================
        // ENABLE
        // ============================================================

        private void OnEnable()
        {
            ARSession.stateChanged +=
                OnARSessionStateChanged;

            if (planeManager != null)
            {
                planeManager.planesChanged +=
                    OnPlanesChanged;
            }

            if (cameraManager != null)
            {
                cameraManager.frameReceived -=
                    OnCameraFrameReceived;

                cameraManager.frameReceived +=
                    OnCameraFrameReceived;
            }

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance
                    .OnPlaneVisualizationChanged +=
                    TogglePlaneVisualization;
            }

            // --------------------------------------------------------
            // ENABLE UNITY INPUT SYSTEM ENHANCED TOUCH
            // --------------------------------------------------------

            EnhancedTouchSupport.Enable();

            Debug.Log(
                "[ARManager] Enhanced Touch enabled.");
        }

        // ============================================================
        // DISABLE
        // ============================================================

        private void OnDisable()
        {
            ARSession.stateChanged -=
                OnARSessionStateChanged;

            if (planeManager != null)
            {
                planeManager.planesChanged -=
                    OnPlanesChanged;
            }

            if (cameraManager != null)
            {
                cameraManager.frameReceived -=
                    OnCameraFrameReceived;
            }

            if (AppSettings.Instance != null)
            {
                AppSettings.Instance
                    .OnPlaneVisualizationChanged -=
                    TogglePlaneVisualization;
            }

            EnhancedTouchSupport.Disable();
        }

        // ============================================================
        // UPDATE & LATE UPDATE
        // ============================================================

        private void Update()
        {
            UpdateCameraZoom();
            HandleTouchInput();
        }

        private void LateUpdate()
        {
            UpdateCameraZoom();
        }

        // ============================================================
        // PINCH ZOOM CAMERA & BACKGROUND VIDEO SMOOTHING
        // ============================================================

        private static readonly int UnityDisplayTransformPropId = Shader.PropertyToID("_UnityDisplayTransform");
        private static readonly int DisplayTransformPropId = Shader.PropertyToID("_DisplayTransform");
        private static readonly int TransformMatrixPropId = Shader.PropertyToID("_TransformMatrix");
        private static readonly int TextureTransformPropId = Shader.PropertyToID("_TextureTransform");

        private Matrix4x4 baseDisplayMatrix = Matrix4x4.identity;
        private bool hasBaseDisplayMatrix = false;
        private Matrix4x4 baseBgTransformMatrix = Matrix4x4.identity;
        private bool hasBaseBgTransform = false;

        private void UpdateCameraZoom()
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.unscaledDeltaTime * zoomSmoothing);

            Camera cam = ARCamera != null ? ARCamera : Camera.main;
            if (cam == null) return;

            ApplyCameraZoom(cam);
        }

        public void ApplyCameraZoom(Camera cam)
        {
            if (cam == null) return;

            if (!hasBaseProjection)
            {
                baseProjMatrix = cam.projectionMatrix;
                baseFOV = cam.fieldOfView > 0f ? cam.fieldOfView : 60f;
                hasBaseProjection = true;
            }

            // 1. ZOOM 3D VIRTUAL WORLD PROJECTION
            if (Mathf.Abs(currentZoom - 1.0f) > 0.001f)
            {
                Matrix4x4 zoomedProj = baseProjMatrix;
                zoomedProj[0, 0] *= currentZoom;
                zoomedProj[1, 1] *= currentZoom;
                // DO NOT multiply [0,2] and [1,2] (principal point). This causes marker drift!
                cam.projectionMatrix = zoomedProj;
                cam.fieldOfView = baseFOV / currentZoom;
            }
            else
            {
                if (hasBaseProjection)
                {
                    cam.projectionMatrix = baseProjMatrix;
                    cam.fieldOfView = baseFOV;
                }
            }

            // 2. ZOOM REAL CAMERA VIDEO FEED (ARCameraBackground)
            if (cameraBackground != null)
            {
                Material bgMat = cameraBackground.material;
                if (bgMat != null)
                {
                    float invZ = 1.0f / currentZoom;
                    float offset = 0.5f * (1.0f - invZ);

                    // Row-vector affine center-scaling matrix:
                    // In GLSL row-major: vec4(u, v, 1, 0) * (scaleMatrix * baseDisplayMatrix)
                    // = (vec4(u, v, 1, 0) * scaleMatrix) * baseDisplayMatrix
                    // where vec4(u, v, 1, 0) * scaleMatrix = [0.5 + (u-0.5)/z, 0.5 + (v-0.5)/z, 1, 0]
                    Matrix4x4 scaleMatrix = Matrix4x4.identity;
                    scaleMatrix.m00 = invZ;
                    scaleMatrix.m11 = invZ;
                    scaleMatrix.m20 = offset; // Row 2, Col 0
                    scaleMatrix.m21 = offset; // Row 2, Col 1

                    // Apply to _UnityDisplayTransform (Official AR Foundation background property)
                    bool appliedDisplayTransform = false;
                    if (hasBaseDisplayMatrix)
                    {
                        Matrix4x4 zoomedDisplayMatrix = scaleMatrix * baseDisplayMatrix;
                        bgMat.SetMatrix(UnityDisplayTransformPropId, zoomedDisplayMatrix);
                        if (bgMat.HasProperty(DisplayTransformPropId))
                            bgMat.SetMatrix(DisplayTransformPropId, zoomedDisplayMatrix);
                        appliedDisplayTransform = true;
                    }
                    else if (bgMat.HasProperty(UnityDisplayTransformPropId))
                    {
                        Matrix4x4 cur = bgMat.GetMatrix(UnityDisplayTransformPropId);
                        bgMat.SetMatrix(UnityDisplayTransformPropId, scaleMatrix * cur);
                        appliedDisplayTransform = true;
                    }

                    if (bgMat.HasProperty(TransformMatrixPropId))
                    {
                        if (!hasBaseBgTransform || Mathf.Abs(currentZoom - 1.0f) < 0.001f)
                        {
                            baseBgTransformMatrix = bgMat.GetMatrix(TransformMatrixPropId);
                            hasBaseBgTransform = true;
                        }

                        if (Mathf.Abs(currentZoom - 1.0f) > 0.001f)
                        {
                            bgMat.SetMatrix(TransformMatrixPropId, scaleMatrix * baseBgTransformMatrix);
                        }
                        else if (hasBaseBgTransform)
                        {
                            bgMat.SetMatrix(TransformMatrixPropId, baseBgTransformMatrix);
                        }
                        appliedDisplayTransform = true;
                    }

                    // Only fallback to _MainTex if shader does not use display transform properties
                    if (!appliedDisplayTransform && bgMat.HasProperty("_MainTex"))
                    {
                        if (Mathf.Abs(currentZoom - 1.0f) > 0.001f)
                        {
                            bgMat.SetTextureScale("_MainTex", new Vector2(invZ, invZ));
                            bgMat.SetTextureOffset("_MainTex", new Vector2(offset, offset));
                        }
                        else
                        {
                            bgMat.SetTextureScale("_MainTex", Vector2.one);
                            bgMat.SetTextureOffset("_MainTex", Vector2.zero);
                        }
                    }
                    else if (appliedDisplayTransform && bgMat.HasProperty("_MainTex"))
                    {
                        // Reset _MainTex scale to (1,1) so it doesn't double-scale with _UnityDisplayTransform
                        bgMat.SetTextureScale("_MainTex", Vector2.one);
                        bgMat.SetTextureOffset("_MainTex", Vector2.zero);
                    }
                }
            }
        }

        // ============================================================
        // COORDINATED GESTURE & TOUCH INPUT
        // ============================================================

        private void HandleTouchInput()
        {
            var touches =
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            int touchCount = touches.Count;

            if (touchCount == 0)
            {
                // When all fingers leave the screen, reset gesture state
                if (currentGestureState == GestureState.PinchLockedEnding ||
                    currentGestureState == GestureState.DragIgnored ||
                    currentGestureState == GestureState.PossibleTap)
                {
                    currentGestureState = GestureState.None;
                }
                else if (currentGestureState == GestureState.MarkerDragging)
                {
                    if (MeasurementManager.Instance != null)
                    {
                        MeasurementManager.Instance.EndDrag(draggedMarkerIndex);
                    }
                    currentGestureState = GestureState.None;
                    draggedMarkerIndex = -1;
                }
                lastPinchDistance = 0f;
                return;
            }

            // --------------------------------------------------------
            // 1. PINCH ZOOM DETECTION (2 or more active touches)
            // --------------------------------------------------------
            if (touchCount >= 2)
            {
                var touch0 = touches[0];
                var touch1 = touches[1];

                // If touch sequence originated over UI, ignore to protect UI elements
                if (currentGestureState == GestureState.None || currentGestureState == GestureState.PossibleTap || currentGestureState == GestureState.MarkerDragging)
                {
                    if (IsPointerOverUI(touch0.screenPosition, touch0.finger.index) ||
                        IsPointerOverUI(touch1.screenPosition, touch1.finger.index))
                    {
                        currentGestureState = GestureState.DragIgnored;
                        return;
                    }
                }

                // Immediately cancel any pending tap or long-press and enter PinchZoom
                currentGestureState = GestureState.PinchZoom;
                tapFingerId = -1;
                draggedMarkerIndex = -1;

                float currentPinchDist = Vector2.Distance(touch0.screenPosition, touch1.screenPosition);

                // If newly entering pinch, initialize distance
                if (touch0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    touch1.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                    lastPinchDistance <= 0.01f)
                {
                    lastPinchDistance = currentPinchDist;
                }
                else
                {
                    float delta = currentPinchDist - lastPinchDistance;
                    lastPinchDistance = currentPinchDist;

                    // Apply zoom delta
                    targetZoom = Mathf.Clamp(targetZoom + delta * zoomSensitivity, minZoom, maxZoom);
                }

                // Raycast/marker placement is 100% blocked during pinch
                return;
            }

            // --------------------------------------------------------
            // 2. PINCH LOCKED TRANSITION (Dropped from 2+ fingers to 1)
            // --------------------------------------------------------
            if (currentGestureState == GestureState.PinchZoom || currentGestureState == GestureState.PinchLockedEnding)
            {
                // One finger still on screen from previous pinch.
                // Lock it so it CANNOT be converted into an accidental tap when lifted.
                currentGestureState = GestureState.PinchLockedEnding;
                lastPinchDistance = 0f;
                return;
            }

            // --------------------------------------------------------
            // 3. SINGLE TOUCH PROCESSING (Exactly 1 active touch)
            // --------------------------------------------------------
            var singleTouch = touches[0];
            Vector2 currentPos = singleTouch.screenPosition;
            int fingerId = singleTouch.finger.index;

            switch (singleTouch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    // Check UI blocking first
                    if (IsPointerOverUI(currentPos, fingerId))
                    {
                        currentGestureState = GestureState.DragIgnored;
                        return;
                    }

                    // Check if touching an existing marker
                    if (MeasurementManager.Instance != null && MeasurementManager.Instance.GetMarkerAtScreenPoint(currentPos, out int mIndex))
                    {
                        currentGestureState = GestureState.MarkerDragging;
                        draggedMarkerIndex = mIndex;
                        tapStartPosition = currentPos;
                        tapStartTime = Time.unscaledTime;
                        tapFingerId = fingerId;
                        if (MeasurementManager.Instance != null)
                        {
                            MeasurementManager.Instance.BeginDrag(draggedMarkerIndex);
                        }
                        return;
                    }

                    // Start possible tap tracking
                    currentGestureState = GestureState.PossibleTap;
                    tapStartPosition = currentPos;
                    tapStartTime = Time.unscaledTime;
                    tapFingerId = fingerId;
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    float duration = Time.unscaledTime - tapStartTime;
                    float movedDist = Vector2.Distance(currentPos, tapStartPosition);

                    if (currentGestureState == GestureState.PossibleTap)
                    {
                        if (movedDist > MAX_TAP_MOVEMENT)
                        {
                            // Finger moved too far -> convert to DragIgnored (no tap)
                            currentGestureState = GestureState.DragIgnored;
                        }
                    }
                    else if (currentGestureState == GestureState.MarkerDragging)
                    {
                        // Freely dragging the marker
                        if (MeasurementManager.Instance != null)
                        {
                            MeasurementManager.Instance.UpdateDrag(draggedMarkerIndex, currentPos);
                        }
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (currentGestureState == GestureState.PossibleTap && singleTouch.phase == UnityEngine.InputSystem.TouchPhase.Ended && fingerId == tapFingerId)
                    {
                        float tapDur = Time.unscaledTime - tapStartTime;
                        float tapDist = Vector2.Distance(currentPos, tapStartPosition);

                        if (tapDur <= MAX_TAP_DURATION && tapDist <= MAX_TAP_MOVEMENT)
                        {
                            // Confirmed genuine single tap!
                            Debug.Log($"[ARManager] Confirmed Single Tap at {tapStartPosition} (Duration: {tapDur:F2}s, Movement: {tapDist:F1}px, Zoom: {currentZoom:F2}x)");
                            HandleScreenTap(tapStartPosition, fingerId);
                        }
                    }
                    else if (currentGestureState == GestureState.MarkerDragging)
                    {
                        if (MeasurementManager.Instance != null)
                        {
                            MeasurementManager.Instance.EndDrag(draggedMarkerIndex);
                        }
                    }

                    currentGestureState = GestureState.None;
                    tapFingerId = -1;
                    draggedMarkerIndex = -1;
                    break;
            }
        }

        // ============================================================
        // SCREEN TAP
        // ============================================================

        private void HandleScreenTap(
            Vector2 screenPosition,
            int fingerId)
        {
            // --------------------------------------------------------
            // UI CHECK (GraphicRaycast + EventSystem validation)
            // --------------------------------------------------------

            if (IsPointerOverUI(screenPosition, fingerId))
            {
                Debug.Log(
                    "[ARManager] TAP IGNORED -> UI element under touch at " +
                    screenPosition);

                return;
            }

            // --------------------------------------------------------
            // CAMERA
            // --------------------------------------------------------

            Camera cam = ARCamera != null
                ? ARCamera
                : Camera.main;

            if (cam == null)
            {
                Debug.LogError(
                    "[ARManager] NO CAMERA FOUND.");

                return;
            }

            // --------------------------------------------------------
            // BASIC TOUCH DIAGNOSTICS
            // --------------------------------------------------------

            Debug.Log(
                "[ARManager] =================================");

            Debug.Log(
                "[ARManager] RAW TOUCH = " +
                screenPosition);

            Debug.Log(
                "[ARManager] SCREEN SIZE = " +
                Screen.width + " x " + Screen.height);

            Debug.Log(
                "[ARManager] CAMERA PIXEL RECT = " +
                cam.pixelRect);

            // --------------------------------------------------------
            // VERIFY TOUCH IS INSIDE CAMERA
            // --------------------------------------------------------

            if (!cam.pixelRect.Contains(screenPosition))
            {
                Debug.LogWarning(
                    "[ARManager] TOUCH IS OUTSIDE CAMERA RECT -> " +
                    screenPosition);

                return;
            }

            // --------------------------------------------------------
            // SCREEN RAY DIAGNOSTIC
            // --------------------------------------------------------

            Ray screenRay =
                cam.ScreenPointToRay(screenPosition);

            Debug.Log(
                "[ARManager] SCREEN RAY ORIGIN = " +
                screenRay.origin);

            Debug.Log(
                "[ARManager] SCREEN RAY DIRECTION = " +
                screenRay.direction);

            // --------------------------------------------------------
            // AR RAYCAST
            // --------------------------------------------------------

            Pose hitPose;
            ARPlane hitPlane;

            bool hit =
                PerformRaycast(
                    screenPosition,
                    out hitPose,
                    out hitPlane);

            Debug.Log(
                "[ARManager] RAYCAST HIT = " +
                hit);

            if (!hit)
            {
                Debug.LogWarning(
                    "[ARManager] TAP DETECTED, " +
                    "BUT NO AR SURFACE WAS HIT.");

                return;
            }

            // --------------------------------------------------------
            // WORLD POSITION
            // --------------------------------------------------------

            Vector3 worldPoint =
                hitPose.position;

            // --------------------------------------------------------
            // PROJECT WORLD POINT BACK TO SCREEN
            // --------------------------------------------------------

            Vector3 projectedScreenPoint =
                cam.WorldToScreenPoint(worldPoint);

            Vector2 projected2D =
                new Vector2(
                    projectedScreenPoint.x,
                    projectedScreenPoint.y);

            float screenError =
                Vector2.Distance(
                    screenPosition,
                    projected2D);

            Debug.Log(
                "[ARManager] HIT WORLD POSITION = " +
                worldPoint);

            Debug.Log(
                "[ARManager] HIT PROJECTED SCREEN = " +
                projected2D);

            Debug.Log(
                "[ARManager] ORIGINAL TOUCH = " +
                screenPosition);

            Debug.Log(
                "[ARManager] SCREEN POSITION ERROR = " +
                screenError);

            Vector2 viewportPos = cam.ScreenToViewportPoint(screenPosition);
            Vector3 planeNormal = hitPlane != null ? hitPlane.normal : Vector3.up;
            string modeName = MeasurementManager.Instance != null ? MeasurementManager.Instance.CurrentMode.ToString() : "Unknown";

            Debug.Log(
                "--------------------------------\n" +
                "MARKER DEBUG\n" +
                "--------------------------------\n" +
                $"Mode: {modeName}\n" +
                $"Orientation: {Screen.orientation} ({Screen.width}x{Screen.height})\n" +
                $"Zoom: {currentZoom:F2}x\n" +
                $"Screen: ({screenPosition.x:F1}, {screenPosition.y:F1})\n" +
                $"Viewport: ({viewportPos.x:F3}, {viewportPos.y:F3})\n" +
                $"Ray Origin: {screenRay.origin}\n" +
                $"Ray Direction: {screenRay.direction}\n" +
                $"Raycast Hit: {worldPoint}\n" +
                $"Trackable: {(hitPlane != null ? hitPlane.trackableId.ToString() : "None/FeaturePoint")}\n" +
                $"Plane Normal: {planeNormal}\n" +
                $"Anchor Position: {worldPoint}\n" +
                $"Stored Measurement Position: {worldPoint}\n" +
                "--------------------------------");

            if (MeasurementManager.Instance != null && MeasurementManager.Instance.MeasurementPoints.Count >= 1)
            {
                Vector3 prevPoint = MeasurementManager.Instance.MeasurementPoints[MeasurementManager.Instance.MeasurementPoints.Count - 1];
                Vector3 delta = worldPoint - prevPoint;
                float distMeters = delta.magnitude;
                Debug.Log(
                    "--------------------------------\n" +
                    $"DX: {delta.x:F4}m ({delta.x * 100f:F1}cm)\n" +
                    $"DY: {delta.y:F4}m ({delta.y * 100f:F1}cm)\n" +
                    $"DZ: {delta.z:F4}m ({delta.z * 100f:F1}cm)\n" +
                    $"WORLD DISTANCE: {distMeters:F4}m ({distMeters * 100f:F2}cm)\n" +
                    "--------------------------------");
            }

            // --------------------------------------------------------
            // ADD MEASUREMENT POINT WITH NATIVE ARPLANE ANCHORING
            // --------------------------------------------------------

            if (MeasurementManager.Instance == null)
            {
                Debug.LogError(
                    "[ARManager] MeasurementManager.Instance " +
                    "IS NULL.");

                return;
            }

            MeasurementManager.Instance.AddMeasurementPoint(
                hitPose,
                hitPlane);

            Debug.Log(
                "[ARManager] MEASUREMENT POINT ADDED -> " +
                worldPoint + (hitPlane != null ? $" (Plane: {hitPlane.trackableId})" : ""));

            Debug.Log(
                "[ARManager] =================================");
        }

        private bool IsPointerOverUI(Vector2 screenPosition, int fingerId = -1)
        {
            if (EventSystem.current == null)
                return false;

            // 1. Direct GraphicRaycast against all active Canvases at the exact screenPosition
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                Debug.Log($"[ARManager] Touch at {screenPosition} is blocked by UI: {results[0].gameObject.name}");
                return true;
            }

            // 2. Standard EventSystem pointer checks (mouse / legacy / device pointer)
            if (EventSystem.current.IsPointerOverGameObject())
                return true;

            if (fingerId >= 0 && EventSystem.current.IsPointerOverGameObject(fingerId))
                return true;

            return false;
        }

        private void ValidateComponents()
        {
            if (xrOrigin == null)
                xrOrigin =
                    FindAnyObjectByType<XROrigin>();

            if (xrOrigin != null)
            {
                xrOrigin.CameraYOffset = 0f;
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            }

            if (arSession == null)
                arSession =
                    FindAnyObjectByType<ARSession>();

            if (arSession != null)
            {
                var inputMgr = arSession.GetComponent<ARInputManager>();
                if (inputMgr == null)
                {
                    inputMgr = arSession.gameObject.AddComponent<ARInputManager>();
                    Debug.Log("[ARManager] Successfully attached ARInputManager to AR Session!");
                }
            }

            if (planeManager == null)
                planeManager =
                    FindAnyObjectByType<ARPlaneManager>();

            if (raycastManager == null)
                raycastManager =
                    FindAnyObjectByType<ARRaycastManager>();

            if (cameraManager == null)
                cameraManager =
                    FindAnyObjectByType<ARCameraManager>();

            if (cameraBackground == null)
                cameraBackground =
                    FindAnyObjectByType<ARCameraBackground>();

            if (anchorManager == null && xrOrigin != null)
            {
                anchorManager = xrOrigin.GetComponent<ARAnchorManager>();
                if (anchorManager == null)
                {
                    anchorManager = xrOrigin.gameObject.AddComponent<ARAnchorManager>();
                    Debug.Log("[ARManager] Successfully attached ARAnchorManager to XROrigin!");
                }
            }

            if (anchorManager == null)
                anchorManager =
                    FindAnyObjectByType<ARAnchorManager>();

            if (anchorManager != null)
                anchorManager.enabled = true;

            // --------------------------------------------------------
            // PLANE DETECTION
            // --------------------------------------------------------

            if (planeManager != null)
            {
                planeManager.requestedDetectionMode =
                    PlaneDetectionMode.Horizontal |
                    PlaneDetectionMode.Vertical;

                planeManager.enabled = true;

                if (planeManager.planePrefab == null)
                {
                    GameObject template = new GameObject("ARPlaneTemplate", typeof(ARPlane), typeof(ARPlaneMeshVisualizer), typeof(MeshFilter), typeof(MeshRenderer), typeof(SmartARMeasure.Visuals.PlaneVisualizer));
                    
                    var mr = template.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Shader shader = Shader.Find("SmartARMeasure/ARSurfaceDots") 
                                     ?? Shader.Find("Universal Render Pipeline/Unlit") 
                                     ?? Shader.Find("Unlit/Transparent");

                        if (shader != null)
                        {
                            Material mat = new Material(shader)
                            {
                                name = "M_ARSurfaceDots_Template"
                            };

                            mr.sharedMaterial = mat;
                            mr.enabled = true;
                        }
                    }

                    template.transform.SetParent(transform);
                    template.SetActive(false);
                    planeManager.planePrefab = template;
                    Debug.Log("[ARManager] Successfully initialized ARPlaneManager planePrefab template with ARSurfaceDots material.");
                }
            }
            else
            {
                Debug.LogError(
                    "[ARManager] ARPlaneManager NOT FOUND.");
            }

            // --------------------------------------------------------
            // RAYCAST
            // --------------------------------------------------------

            if (raycastManager != null)
            {
                raycastManager.enabled = true;
            }
            else
            {
                Debug.LogError(
                    "[ARManager] ARRaycastManager NOT FOUND.");
            }

            // --------------------------------------------------------
            // TRACKED POSE DRIVER (CRUCIAL FOR 6DoF AR CAMERA MOVEMENT)
            // --------------------------------------------------------

            Camera cam = ARCamera != null ? ARCamera : (cameraManager != null ? cameraManager.GetComponent<Camera>() : Camera.main);
            if (cam != null)
            {
                var tpd = cam.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tpd == null)
                {
                    tpd = cam.gameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                    Debug.Log("[ARManager] Successfully attached TrackedPoseDriver to AR Camera!");
                }

                if (tpd.positionInput.action == null || tpd.positionInput.action.bindings.Count == 0)
                {
                    var posAction = new UnityEngine.InputSystem.InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
                    posAction.AddBinding("<HandheldARInputDevice>/devicePosition");
                    tpd.positionInput = new UnityEngine.InputSystem.InputActionProperty(posAction);
                }

                if (tpd.rotationInput.action == null || tpd.rotationInput.action.bindings.Count == 0)
                {
                    var rotAction = new UnityEngine.InputSystem.InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
                    rotAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
                    tpd.rotationInput = new UnityEngine.InputSystem.InputActionProperty(rotAction);
                }

                tpd.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
                tpd.updateType = UnityEngine.InputSystem.XR.TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                tpd.enabled = true;
            }

            Debug.Log(
                "[ARManager] Components validated. " +
                "Plane detection, camera tracking, and raycasting enabled.");
        }

        // ============================================================
        // ENSURE AR ACTIVE
        // ============================================================

        public void EnsureARCameraActive()
        {
            ValidateComponents();

            if (xrOrigin != null)
                xrOrigin.gameObject.SetActive(true);

            if (arSession != null)
            {
                arSession.gameObject.SetActive(true);
                arSession.enabled = true;

                if (ARSession.state ==
                        ARSessionState.CheckingAvailability ||
                    ARSession.state ==
                        ARSessionState.None ||
                    ARSession.state ==
                        ARSessionState.SessionInitializing)
                {
                    arSession.Reset();

                    Debug.Log(
                        "[ARManager] AR Session reset.");
                }
            }

            if (cameraManager != null)
            {
                cameraManager.gameObject.SetActive(true);
                cameraManager.enabled = true;
            }

            if (cameraBackground != null)
            {
                cameraBackground.gameObject.SetActive(true);
                cameraBackground.enabled = true;
            }

            if (ARCamera != null)
            {
                ARCamera.gameObject.SetActive(true);
                ARCamera.enabled = true;

                var tpd = ARCamera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                if (tpd != null) tpd.enabled = true;
            }

            if (planeManager != null)
                planeManager.enabled = true;

            if (raycastManager != null)
                raycastManager.enabled = true;

            if (anchorManager != null)
                anchorManager.enabled = true;

            SetReticleVisible(false);

            Debug.Log(
                "[ARManager] AR Camera, Session, Plane Manager " +
                "and Raycast Manager are active.");
        }

        // ============================================================
        // CAMERA FRAME
        // ============================================================

        private void OnCameraFrameReceived(
            ARCameraFrameEventArgs args)
        {
            hasReceivedFrame = true;
            if (args.displayMatrix.HasValue)
            {
                baseDisplayMatrix = args.displayMatrix.Value;
                hasBaseDisplayMatrix = true;
            }
            if (args.projectionMatrix.HasValue)
            {
                baseProjMatrix = args.projectionMatrix.Value;
                hasBaseProjection = true;
            }
        }

        // ============================================================
        // EXACT SCREEN RAYCAST
        // ============================================================

        public bool PerformRaycast(
            Vector2 screenPosition,
            out Pose hitPose,
            out ARPlane hitPlane)
        {
            hitPose = Pose.identity;
            hitPlane = null;

            if (raycastManager == null)
            {
                ValidateComponents();
                if (raycastManager == null)
                {
                    Debug.LogError("[ARManager] ARRaycastManager is missing.");
                    return false;
                }
            }

            if (!raycastManager.enabled)
            {
                raycastManager.enabled = true;
                Debug.Log("[ARManager] RaycastManager re-enabled.");
            }

            Camera cam = ARCamera != null ? ARCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogError("[ARManager] Camera is missing for raycast.");
                return false;
            }

            // 1. CALCULATE UNZOOMED SCREEN COORDINATE
            Vector2 screenCenter = cam.pixelRect.center;
            Vector2 unzoomedScreenPos = screenPosition;
            if (currentZoom > 1.001f)
            {
                unzoomedScreenPos = screenCenter + (screenPosition - screenCenter) / currentZoom;
            }

            Debug.Log($"[ARManager] RAYCAST PIPELINE -> Touch: {screenPosition}, Unzoomed: {unzoomedScreenPos}, Zoom: {currentZoom:F2}x");

            s_Hits.Clear();

            // --------------------------------------------------------
            // PASS 1: GATHER ALL NATIVE HITS
            // --------------------------------------------------------
            
            s_Hits.Clear();
            List<ARRaycastHit> polygonHits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(unzoomedScreenPos, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                polygonHits.AddRange(s_Hits);
            }

            s_Hits.Clear();
            List<ARRaycastHit> boundsHits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(unzoomedScreenPos, s_Hits, TrackableType.PlaneWithinBounds | TrackableType.PlaneEstimated))
            {
                boundsHits.AddRange(s_Hits);
            }

            s_Hits.Clear();
            List<ARRaycastHit> featureHits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(unzoomedScreenPos, s_Hits, TrackableType.FeaturePoint))
            {
                featureHits.AddRange(s_Hits);
            }

            // --------------------------------------------------------
            // PASS 2: DEPTH-AWARE PRIORITY EVALUATION
            // --------------------------------------------------------
            ARRaycastHit bestHit = default;
            bool foundHit = false;
            TrackableType selectedType = TrackableType.None;

            if (polygonHits.Count > 0)
            {
                bestHit = polygonHits[0];
                foundHit = true;
                selectedType = TrackableType.PlaneWithinPolygon;
                
                float polygonDist = bestHit.distance > 0f ? bestHit.distance : Vector3.Distance(cam.transform.position, bestHit.pose.position);

                // Check if a FeaturePoint or Bounds hit is significantly closer (Foreground Object Graze)
                float minForegroundDist = polygonDist;
                ARRaycastHit foregroundHit = default;
                bool hasForeground = false;
                TrackableType fgType = TrackableType.None;

                foreach (var fh in featureHits)
                {
                    float dist = fh.distance > 0f ? fh.distance : Vector3.Distance(cam.transform.position, fh.pose.position);
                    if (dist < minForegroundDist - 0.10f) // Must be > 10cm closer to camera
                    {
                        minForegroundDist = dist;
                        foregroundHit = fh;
                        hasForeground = true;
                        fgType = TrackableType.FeaturePoint;
                    }
                }

                foreach (var bh in boundsHits)
                {
                    float dist = bh.distance > 0f ? bh.distance : Vector3.Distance(cam.transform.position, bh.pose.position);
                    if (dist < minForegroundDist - 0.10f) // Must be > 10cm closer to camera
                    {
                        minForegroundDist = dist;
                        foregroundHit = bh;
                        hasForeground = true;
                        fgType = bh.hitType;
                    }
                }

                if (hasForeground)
                {
                    bestHit = foregroundHit;
                    selectedType = fgType;
                    Debug.Log($"[ARManager] FOREGROUND GRAZE DETECTED! Polygon plane was at {polygonDist:F2}m, but Foreground ({fgType}) hit was at {minForegroundDist:F2}m. Selecting foreground.");
                }
            }
            else if (boundsHits.Count > 0)
            {
                bestHit = boundsHits[0];
                foundHit = true;
                selectedType = bestHit.hitType;
            }
            else if (featureHits.Count > 0)
            {
                bestHit = featureHits[0];
                foundHit = true;
                selectedType = TrackableType.FeaturePoint;
            }

            // --------------------------------------------------------
            // PASS 3: FINALIZE
            // --------------------------------------------------------
            if (foundHit)
            {
                hitPose = bestHit.pose;
                hitPlane = bestHit.trackable as ARPlane;
                lastHitPose = hitPose;
                hasValidHitPose = true;

                OnReticleUpdated?.Invoke(hitPose, true);
                Debug.Log($"[ARManager] DEPTH-AWARE SURFACE HIT ({selectedType}) -> Pos: {hitPose.position}" + (hitPlane != null ? $" (Plane: {hitPlane.trackableId})" : ""));
                return true;
            }

            hasValidHitPose = false;
            OnReticleUpdated?.Invoke(Pose.identity, false);
            Debug.Log("[ARManager] Raycast returned NO HIT.");
            return false;
        }

        /// <summary>
        /// Geometrically refines a surface hit against the physical plane's boundary polygon vertices and edge segments.
        /// When the user taps near a table edge/corner (< 6 cm), snaps to the boundary edge or corner in 3D world space.
        /// Leaves hits on open surfaces untouched to maintain exact linear accuracy.
        /// </summary>
        public static Pose RefineHitPoseNearPlaneBoundary(Pose hitPose, ARPlane plane, float maxSnapDistance = 0.06f)
        {
            if (plane == null) return hitPose;

            var boundary = plane.boundary;
            if (!boundary.IsCreated || boundary.Length < 3) return hitPose;

            Vector3 worldHit = hitPose.position;
            Vector3 localHit = plane.transform.InverseTransformPoint(worldHit);
            Vector2 localHit2D = new Vector2(localHit.x, localHit.z);

            float closestEdgeDist = float.MaxValue;
            Vector2 closestEdgePoint2D = localHit2D;
            int closestVertexIndex = -1;
            float closestVertexDist = float.MaxValue;

            int n = boundary.Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 v0 = boundary[i];
                Vector2 v1 = boundary[(i + 1) % n];

                float vDist = Vector2.Distance(localHit2D, v0);
                if (vDist < closestVertexDist)
                {
                    closestVertexDist = vDist;
                    closestVertexIndex = i;
                }

                Vector2 segPoint = ClosestPointOnSegment2D(localHit2D, v0, v1);
                float segDist = Vector2.Distance(localHit2D, segPoint);
                if (segDist < closestEdgeDist)
                {
                    closestEdgeDist = segDist;
                    closestEdgePoint2D = segPoint;
                }
            }

            Vector3 planeScale = plane.transform.lossyScale;
            float scaleFactor = Mathf.Max(planeScale.x, planeScale.z);
            if (scaleFactor <= 1e-4f) scaleFactor = 1f;

            float worldEdgeDist = closestEdgeDist * scaleFactor;
            float worldVertexDist = closestVertexDist * scaleFactor;

            // Strict confidence threshold: snap only when user tap was within maxSnapDistance of detected boundary
            if (worldEdgeDist <= maxSnapDistance)
            {
                Vector2 targetLocal2D;
                if (worldVertexDist <= 0.04f && closestVertexIndex >= 0)
                {
                    targetLocal2D = boundary[closestVertexIndex];
                    Debug.Log($"[ARManager] CORNER REFINEMENT -> Vertex {closestVertexIndex}, Dist: {worldVertexDist * 100f:F1}cm");
                }
                else
                {
                    targetLocal2D = closestEdgePoint2D;
                    Debug.Log($"[ARManager] EDGE REFINEMENT -> Edge Dist: {worldEdgeDist * 100f:F1}cm");
                }

                Vector3 refinedLocal = new Vector3(targetLocal2D.x, localHit.y, targetLocal2D.y);
                Vector3 refinedWorld = plane.transform.TransformPoint(refinedLocal);

                return new Pose(refinedWorld, hitPose.rotation);
            }

            return hitPose;
        }

        private static Vector2 ClosestPointOnSegment2D(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen < 1e-6f) return a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqrLen);
            return a + t * ab;
        }

        private static ARRaycastHit GetClosestHit(List<ARRaycastHit> hits)
        {
            if (hits == null || hits.Count == 0) return default;
            ARRaycastHit closest = hits[0];
            float minDist = closest.distance;

            for (int i = 1; i < hits.Count; i++)
            {
                if (hits[i].distance > 0f && (minDist <= 0f || hits[i].distance < minDist))
                {
                    minDist = hits[i].distance;
                    closest = hits[i];
                }
            }
            return closest;
        }

        public bool PerformRaycast(
            Vector2 screenPosition,
            out Pose hitPose)
        {
            return PerformRaycast(screenPosition, out hitPose, out _);
        }

        // ============================================================
        // OLD RETICLE COMPATIBILITY
        // ============================================================

        public void SetReticleVisible(
            bool visible)
        {
            if (reticleObject != null)
                reticleObject.SetActive(false);

            if (visible)
            {
                Debug.Log(
                    "[ARManager] Old reticle request ignored. " +
                    "Measurement uses tap markers.");
            }
        }

        // ============================================================
        // PLANE VISUALIZATION
        // ============================================================

        public void TogglePlaneVisualization(
            bool showPlanes)
        {
            if (planeManager == null)
                return;

            foreach (var plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    var visualizer = plane.GetComponent<SmartARMeasure.Visuals.PlaneVisualizer>();
                    if (visualizer != null)
                    {
                        visualizer.SetVisibility(showPlanes);
                    }
                    else
                    {
                        var mr = plane.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = showPlanes;

                        var lr = plane.GetComponent<LineRenderer>();
                        if (lr != null) lr.enabled = showPlanes;
                    }
                }
            }
        }

        // ============================================================
        // SESSION STATE
        // ============================================================

        private void OnARSessionStateChanged(
            ARSessionStateChangedEventArgs args)
        {
            switch (args.state)
            {
                case ARSessionState.CheckingAvailability:
                case ARSessionState.Installing:
                case ARSessionState.Ready:

                    SetTrackingState(
                        ARTrackingState.Initializing);

                    break;

                case ARSessionState.SessionInitializing:

                    SetTrackingState(
                        ARTrackingState.SearchingForPlanes);

                    break;

                case ARSessionState.SessionTracking:

                    SetTrackingState(
                        ARTrackingState.TrackingActive);

                    break;

                case ARSessionState.Unsupported:
                case ARSessionState.NeedsInstall:

                    SetTrackingState(
                        ARTrackingState.Error);

                    break;
            }
        }

        // ============================================================
        // PLANES CHANGED
        // ============================================================

        private void OnPlanesChanged(
            ARPlanesChangedEventArgs args)
        {
            if (planeManager == null)
                return;

            foreach (var plane in args.added)
            {
                if (plane != null)
                {
                    // Ensure the instantiated plane GameObject is active
                    if (!plane.gameObject.activeSelf)
                    {
                        plane.gameObject.SetActive(true);
                    }

                    var visualizer = plane.GetComponent<SmartARMeasure.Visuals.PlaneVisualizer>();
                    if (visualizer == null)
                    {
                        visualizer = plane.gameObject.AddComponent<SmartARMeasure.Visuals.PlaneVisualizer>();
                    }
                    visualizer.EnsureMaterialAssigned();

                    var mr = plane.GetComponent<MeshRenderer>();
                    var mf = plane.GetComponent<MeshFilter>();
                    int vCount = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.vertexCount : 0;
                    int tCount = (mf != null && mf.sharedMesh != null && mf.sharedMesh.triangles != null) ? mf.sharedMesh.triangles.Length / 3 : 0;
                    string sName = (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.shader != null) ? mr.sharedMaterial.shader.name : "NONE";

                    Debug.Log($"[ARManager] >>> AR PLANE ADDED <<<\n" +
                              $"* ID: {plane.trackableId}\n" +
                              $"* Tracking State: {plane.trackingState}\n" +
                              $"* Alignment: {plane.alignment}\n" +
                              $"* Size: {plane.size}\n" +
                              $"* Mesh Vertices: {vCount}\n" +
                              $"* Mesh Triangles: {tCount}\n" +
                              $"* MeshRenderer Enabled: {(mr != null && mr.enabled)}\n" +
                              $"* Material Assigned: {(mr != null && mr.sharedMaterial != null)}\n" +
                              $"* Shader: {sName}\n" +
                              $"* GameObject Active: {plane.gameObject.activeInHierarchy}");
                }
            }

            foreach (var plane in args.updated)
            {
                if (plane != null)
                {
                    var mr = plane.GetComponent<MeshRenderer>();
                    var mf = plane.GetComponent<MeshFilter>();
                    int vCount = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.vertexCount : 0;
                    int tCount = (mf != null && mf.sharedMesh != null && mf.sharedMesh.triangles != null) ? mf.sharedMesh.triangles.Length / 3 : 0;

                    Debug.Log($"[ARManager] >>> AR PLANE UPDATED <<<\n" +
                              $"* ID: {plane.trackableId}\n" +
                              $"* Tracking State: {plane.trackingState}\n" +
                              $"* Mesh Vertices: {vCount}\n" +
                              $"* Mesh Triangles: {tCount}\n" +
                              $"* MeshRenderer Enabled: {(mr != null && mr.enabled)}");
                }
            }

            foreach (var plane in args.removed)
            {
                if (plane != null)
                {
                    Debug.Log($"[ARManager] >>> AR PLANE REMOVED <<< ID: {plane.trackableId}");
                }
            }

            if (planeManager.trackables.count > 0)
            {
                if (currentState ==
                    ARTrackingState.SearchingForPlanes)
                {
                    SetTrackingState(
                        ARTrackingState.TrackingActive);

                    Debug.Log(
                        "[ARManager] AR PLANE DETECTED. Active tracked planes: " + planeManager.trackables.count);
                }
            }
        }

        // ============================================================
        // TRACKING STATE
        // ============================================================

        private void SetTrackingState(
            ARTrackingState newState)
        {
            if (currentState == newState)
                return;

            currentState =
                newState;

            Debug.Log(
                "[ARManager] Tracking state -> " +
                currentState);

            OnTrackingStateChanged?.Invoke(
                currentState);
        }
    }
}