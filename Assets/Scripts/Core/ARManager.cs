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
        // AWAKE
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
        // UPDATE
        // ============================================================

        private void Update()
        {
            HandleTouchInput();
        }

        // ============================================================
        // TOUCH INPUT
        // ============================================================

        private void HandleTouchInput()
        {
            var touches =
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

            if (touches.Count == 0)
                return;

            // --------------------------------------------------------
            // ONLY PROCESS A NEW TAP
            // --------------------------------------------------------

            foreach (var touch in touches)
            {
                if (touch.phase !=
                    UnityEngine.InputSystem.TouchPhase.Began)
                {
                    continue;
                }

                Vector2 screenPosition =
                    touch.screenPosition;

                Debug.Log(
                    "[ARManager] TOUCH BEGAN -> " +
                    screenPosition);

                HandleScreenTap(
                    screenPosition,
                    touch.finger.index);
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
                    Debug.LogError(
                        "[ARManager] ARRaycastManager is missing.");

                    return false;
                }
            }

            if (!raycastManager.enabled)
            {
                raycastManager.enabled = true;

                Debug.Log(
                    "[ARManager] RaycastManager re-enabled.");
            }

            s_Hits.Clear();

            // --------------------------------------------------------
            // FIRST: REAL TRACKED PLANES (EXACT WITHIN POLYGON)
            // --------------------------------------------------------

            bool hit =
                raycastManager.Raycast(
                    screenPosition,
                    s_Hits,
                    TrackableType.PlaneWithinPolygon);

            if (hit && s_Hits.Count > 0)
            {
                hitPose =
                    s_Hits[0].pose;

                hitPlane =
                    s_Hits[0].trackable as ARPlane;

                lastHitPose =
                    hitPose;

                hasValidHitPose =
                    true;

                OnReticleUpdated?.Invoke(
                    hitPose,
                    true);

                Debug.Log(
                    "[ARManager] EXACT SURFACE HIT -> " +
                    hitPose.position + (hitPlane != null ? $" (Plane: {hitPlane.trackableId})" : ""));

                return true;
            }

            // --------------------------------------------------------
            // SECOND: ESTIMATED PLANE FALLBACK
            // --------------------------------------------------------

            s_Hits.Clear();

            hit =
                raycastManager.Raycast(
                    screenPosition,
                    s_Hits,
                    TrackableType.PlaneEstimated | TrackableType.PlaneWithinBounds);

            if (hit && s_Hits.Count > 0)
            {
                hitPose =
                    s_Hits[0].pose;

                hitPlane =
                    s_Hits[0].trackable as ARPlane;

                lastHitPose =
                    hitPose;

                hasValidHitPose =
                    true;

                OnReticleUpdated?.Invoke(
                    hitPose,
                    true);

                Debug.Log(
                    "[ARManager] ESTIMATED SURFACE HIT -> " +
                    hitPose.position);

                return true;
            }

            // --------------------------------------------------------
            // THIRD: FEATURE POINT FALLBACK
            // --------------------------------------------------------

            s_Hits.Clear();

            hit =
                raycastManager.Raycast(
                    screenPosition,
                    s_Hits,
                    TrackableType.FeaturePoint);

            if (hit && s_Hits.Count > 0)
            {
                hitPose =
                    s_Hits[0].pose;

                hitPlane = null;

                lastHitPose =
                    hitPose;

                hasValidHitPose =
                    true;

                OnReticleUpdated?.Invoke(
                    hitPose,
                    true);

                Debug.Log(
                    "[ARManager] FEATURE POINT HIT -> " +
                    hitPose.position);

                return true;
            }

            // --------------------------------------------------------
            // NO HIT
            // --------------------------------------------------------

            hasValidHitPose = false;

            OnReticleUpdated?.Invoke(
                Pose.identity,
                false);

            Debug.Log(
                "[ARManager] Raycast returned NO HIT.");

            return false;
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

            if (planeManager.trackables.count > 0)
            {
                if (currentState ==
                    ARTrackingState.SearchingForPlanes)
                {
                    SetTrackingState(
                        ARTrackingState.TrackingActive);

                    Debug.Log(
                        "[ARManager] AR PLANE DETECTED.");
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