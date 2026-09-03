using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SmartARMeasure.Models;

namespace SmartARMeasure.Core
{
    public class MeasurementManager : MonoBehaviour
    {
        // =========================================================
        // SINGLETON
        // =========================================================

        public static MeasurementManager Instance { get; private set; }

        // =========================================================
        // EVENTS
        // =========================================================

        public event Action<MeasurementMode> OnModeChanged;
        public event Action<float> OnDistanceUpdated;
        public event Action OnMeasurementCleared;
        public event Action<MeasurementRecord> OnMeasurementSaved;

        // =========================================================
        // CURRENT MODE
        // =========================================================

        [SerializeField]
        private MeasurementMode currentMode =
            MeasurementMode.Distance;

        public MeasurementMode CurrentMode
        {
            get { return currentMode; }
        }

        // =========================================================
        // UNIT
        // =========================================================

        [SerializeField]
        private MeasurementUnit currentUnit =
            MeasurementUnit.Metric;

        public MeasurementUnit CurrentUnit
        {
            get { return currentUnit; }
        }

        // =========================================================
        // AR REFERENCES
        // =========================================================

        [Header("AR References")]

        [SerializeField]
        private ARRaycastManager arRaycastManager;

        [SerializeField]
        private Camera arCamera;

        // =========================================================
        // AR RAYCAST
        // =========================================================

        private static readonly List<ARRaycastHit> raycastHits =
            new List<ARRaycastHit>();

        // =========================================================
        // CURRENT MEASUREMENT
        // =========================================================

        private readonly List<Vector3> measurementPoints =
            new List<Vector3>();

        public IReadOnlyList<Vector3> MeasurementPoints
        {
            get { return measurementPoints; }
        }

        private float currentValue;

        public float CurrentValue
        {
            get { return currentValue; }
        }

        // =========================================================
        // HISTORY
        // =========================================================

        private List<MeasurementRecord> historyRecords =
            new List<MeasurementRecord>();

        public List<MeasurementRecord> HistoryRecords
        {
            get { return historyRecords; }
        }

        // =========================================================
        // VISUAL SETTINGS
        // =========================================================

        [Header("Visual Settings")]

        // Bright cyan / blue
        [SerializeField]
        private Color markerColor =
            new Color(0.0f, 1.0f, 1.0f, 1.0f);

        // ~1.5 cm diameter sphere (calibrated centralized marker scale)
        [SerializeField]
        private float markerSize = 0.015f;

        [SerializeField]
        private Color lineColor =
            new Color(0.0f, 1.0f, 1.0f, 1.0f);

        // ~3.2 mm line width (reduced to ~40% thickness)
        [SerializeField]
        private float lineWidth = 0.0032f;

        // =========================================================
        // RUNTIME VISUAL OBJECTS
        // =========================================================

        private readonly List<GameObject> markers =
            new List<GameObject>();

        private GameObject measurementLine;

        private LineRenderer lineRenderer;

        // =========================================================
        // TOUCH SETTINGS
        // =========================================================

        [Header("Touch Settings")]

        // Disabled in MeasurementManager because ARManager handles screen taps and raycasting.
        [SerializeField]
        private bool enableTouchMeasurement = false;

        // Lift measurement points slightly above the AR plane.
        [SerializeField]
        private float surfaceOffset = 0.02f;

        // Prevent accidental duplicate taps.
        private float lastTapTime = -10f;

        [SerializeField]
        private float tapCooldown = 0.15f;

        // =========================================================
        // UNITY - AWAKE
        // =========================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            ResolveARReferences();

            LoadHistory();

            Debug.Log(
                "[MeasurementManager] Awake - initialized."
            );
        }

        // =========================================================
        // UNITY - ON ENABLE / ON DISABLE
        // =========================================================

        private bool isAppSettingsSubscribed = false;

        private void OnEnable()
        {
            SubscribeAppSettings();
        }

        private void OnDisable()
        {
            UnsubscribeAppSettings();
        }

        private void SubscribeAppSettings()
        {
            if (isAppSettingsSubscribed) return;

            var settings = AppSettings.Instance;
            if (settings == null)
            {
                settings = UnityEngine.Object.FindAnyObjectByType<AppSettings>();
            }

            if (settings != null)
            {
                markerColor = settings.ThemeColor;
                lineColor = settings.ThemeColor;
                lineWidth = settings.LineThickness;
                currentUnit = settings.Unit;

                settings.OnThemeColorChanged += HandleThemeColorChanged;
                settings.OnMarkerColorChanged += HandleMarkerColorChanged;
                settings.OnLineColorChanged += HandleLineColorChanged;
                settings.OnLineThicknessChanged += HandleLineThicknessChanged;
                settings.OnUnitChanged += HandleUnitChanged;
                settings.OnMetricDisplayUnitChanged += HandleMetricUnitChanged;
                settings.OnImperialDisplayUnitChanged += HandleImperialUnitChanged;

                isAppSettingsSubscribed = true;
            }
        }

        private void UnsubscribeAppSettings()
        {
            if (!isAppSettingsSubscribed) return;

            var settings = AppSettings.Instance;
            if (settings == null)
            {
                settings = UnityEngine.Object.FindAnyObjectByType<AppSettings>();
            }

            if (settings != null)
            {
                settings.OnThemeColorChanged -= HandleThemeColorChanged;
                settings.OnMarkerColorChanged -= HandleMarkerColorChanged;
                settings.OnLineColorChanged -= HandleLineColorChanged;
                settings.OnLineThicknessChanged -= HandleLineThicknessChanged;
                settings.OnUnitChanged -= HandleUnitChanged;
                settings.OnMetricDisplayUnitChanged -= HandleMetricUnitChanged;
                settings.OnImperialDisplayUnitChanged -= HandleImperialUnitChanged;
            }

            isAppSettingsSubscribed = false;
        }

        private void HandleMarkerColorChanged(Color color)
        {
            markerColor = color;
            foreach (var marker in markers)
            {
                if (marker != null)
                {
                    Renderer r = marker.GetComponent<Renderer>();
                    if (r != null)
                    {
                        if (r.material != null)
                        {
                            r.material.color = color;
                            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
                            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", color);
                        }
                        else
                        {
                            r.material = GetMeasurementMaterial(color);
                        }
                    }
                }
            }
        }

        private void HandleLineColorChanged(Color color)
        {
            lineColor = color;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                if (lineRenderer.material != null)
                {
                    lineRenderer.material.color = lineColor;
                    if (lineRenderer.material.HasProperty("_BaseColor")) lineRenderer.material.SetColor("_BaseColor", lineColor);
                    if (lineRenderer.material.HasProperty("_Color")) lineRenderer.material.SetColor("_Color", lineColor);
                }
                else
                {
                    lineRenderer.material = GetMeasurementMaterial(lineColor);
                }
            }
        }

        private void HandleThemeColorChanged(Color color)
        {
            HandleMarkerColorChanged(color);
            HandleLineColorChanged(color);
        }

        private void HandleLineThicknessChanged(float thickness)
        {
            lineWidth = Mathf.Max(thickness, 0.0005f);
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
            }
        }

        private void HandleUnitChanged(MeasurementUnit unit)
        {
            currentUnit = unit;
            NotifyDistanceChanged();
        }

        private void HandleMetricUnitChanged(MetricDisplayUnit unit)
        {
            NotifyDistanceChanged();
        }

        private void HandleImperialUnitChanged(ImperialDisplayUnit unit)
        {
            NotifyDistanceChanged();
        }

        // =========================================================
        // UNITY - START
        // =========================================================

        private void Start()
        {
            SubscribeAppSettings();
            ResolveARReferences();

            Debug.Log(
                "[MeasurementManager] Start."
            );
        }

        // =========================================================
        // UNITY - UPDATE
        // =========================================================

        private void Update()
        {
            // MeasurementManager no longer polls Input directly.
            // All input is routed from ARManager's gesture state machine.
        }

        private void LateUpdate()
        {
            SyncPointsWithAnchors();
        }

        private void SyncPointsWithAnchors()
        {
            if (markers.Count == 0 || measurementPoints.Count == 0) return;

            bool updated = false;
            int count = Mathf.Min(markers.Count, measurementPoints.Count);
            for (int i = 0; i < count; i++)
            {
                if (markers[i] != null)
                {
                    Vector3 currentAnchorPos = markers[i].transform.position;
                    if (Vector3.SqrMagnitude(currentAnchorPos - measurementPoints[i]) > 1e-6f)
                    {
                        measurementPoints[i] = currentAnchorPos;
                        updated = true;
                    }
                }
            }

            if (updated && measurementPoints.Count >= 2)
            {
                UpdateMeasurementVisuals();
                CalculateMeasurement();
            }
        }

        // =========================================================
        // FIND AR REFERENCES
        // =========================================================

        private void ResolveARReferences()
        {
            if (arRaycastManager == null)
            {
                arRaycastManager =
                    GetComponent<ARRaycastManager>();
            }

            if (arRaycastManager == null)
            {
                arRaycastManager =
                    FindObjectOfType<ARRaycastManager>();
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (arCamera == null)
            {
                Camera cameraFound =
                    FindObjectOfType<Camera>();

                if (cameraFound != null)
                    arCamera = cameraFound;
            }

            if (arRaycastManager == null)
            {
                Debug.LogError(
                    "[MeasurementManager] " +
                    "ARRaycastManager NOT FOUND."
                );
            }

            if (arCamera == null)
            {
                Debug.LogError(
                    "[MeasurementManager] " +
                    "AR Camera NOT FOUND."
                );
            }
        }

        // =========================================================
        // DRAG API
        // =========================================================

        public bool GetMarkerAtScreenPoint(Vector2 screenPoint, out int markerIndex)
        {
            markerIndex = -1;
            if (ARManager.Instance == null || ARManager.Instance.ARCamera == null) return false;
            Camera cam = ARManager.Instance.ARCamera;
            
            float minSqrDist = float.MaxValue;
            float maxRadius = 150f; // Reasonable hit radius
            float maxSqrRadius = maxRadius * maxRadius;

            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] == null || !markers[i].activeInHierarchy) continue;
                Vector3 screenPos = cam.WorldToScreenPoint(markers[i].transform.position);
                if (screenPos.z < 0) continue; // Behind camera

                Vector2 screenPos2D = new Vector2(screenPos.x, screenPos.y);
                float sqrDist = (screenPos2D - screenPoint).sqrMagnitude;

                if (sqrDist < maxSqrRadius && sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    markerIndex = i;
                }
            }
            return markerIndex != -1;
        }

        public void BeginDrag(int markerIndex)
        {
            if (markerIndex < 0 || markerIndex >= markers.Count || markers[markerIndex] == null) return;
            
            // Un-anchor the marker during drag
            ARAnchor anchor = markers[markerIndex].GetComponentInParent<ARAnchor>();
            if (anchor != null)
            {
                if (anchor.gameObject != markers[markerIndex])
                {
                    if (ARManager.Instance != null && ARManager.Instance.Origin != null)
                        markers[markerIndex].transform.SetParent(ARManager.Instance.Origin.TrackablesParent, true);
                    else
                        markers[markerIndex].transform.SetParent(null, true);
                    
                    Destroy(anchor.gameObject);
                }
                else
                {
                    Destroy(anchor);
                }
            }

            Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f); // ORANGE
            foreach (GameObject m in markers)
            {
                if (m == null) continue;
                MeshRenderer renderer = m.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = orange;
                }
            }

            if (lineRenderer != null)
            {
                lineRenderer.startColor = orange;
                lineRenderer.endColor = orange;
                if (lineRenderer.material != null)
                {
                    lineRenderer.material.color = orange;
                }
            }
            
            Debug.Log($"[MeasurementManager] Begin Drag Marker {markerIndex}. Entire measurement colored Orange.");
        }

        public void UpdateDrag(int markerIndex, Vector2 screenPos)
        {
            if (markerIndex < 0 || markerIndex >= markers.Count || markers[markerIndex] == null) return;

            if (ARManager.Instance != null && ARManager.Instance.PerformRaycast(screenPos, out Pose hitPose, out ARPlane hitPlane))
            {
                measurementPoints[markerIndex] = hitPose.position;
                markers[markerIndex].transform.position = hitPose.position;
                
                UpdateMeasurementVisuals();
                CalculateMeasurement();
            }
        }

        public void EndDrag(int markerIndex)
        {
            if (markerIndex < 0 || markerIndex >= markers.Count || markers[markerIndex] == null) return;
            
            Color effectiveColor = AppSettings.Instance != null ? AppSettings.Instance.ThemeColor : markerColor;
            foreach (GameObject m in markers)
            {
                if (m == null) continue;
                MeshRenderer renderer = m.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = effectiveColor;
                }
            }

            if (lineRenderer != null)
            {
                lineRenderer.startColor = effectiveColor;
                lineRenderer.endColor = effectiveColor;
                if (lineRenderer.material != null)
                {
                    lineRenderer.material.color = effectiveColor;
                }
            }

            // Re-anchor at the new position
            if (ARManager.Instance != null && ARManager.Instance.PerformRaycast(ARManager.Instance.ARCamera.WorldToScreenPoint(markers[markerIndex].transform.position), out Pose hitPose, out ARPlane hitPlane))
            {
                if (ARManager.Instance.AnchorManager != null && ARManager.Instance.AnchorManager.enabled)
                {
                    ARAnchor anchor = null;
                    if (hitPlane != null)
                    {
                        anchor = ARManager.Instance.AnchorManager.AttachAnchor(hitPlane, hitPose);
                    }

                    if (anchor == null)
                    {
                        GameObject anchorObj = new GameObject("Anchor_" + markerIndex);
                        if (ARManager.Instance.Origin != null && ARManager.Instance.Origin.TrackablesParent != null)
                        {
                            anchorObj.transform.SetParent(ARManager.Instance.Origin.TrackablesParent, false);
                        }
                        anchorObj.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                        anchor = anchorObj.AddComponent<ARAnchor>();
                    }

                    if (anchor != null)
                    {
                        markers[markerIndex].transform.SetParent(anchor.transform, false);
                        markers[markerIndex].transform.localPosition = Vector3.zero;
                        markers[markerIndex].transform.localRotation = Quaternion.identity;
                        
                        Vector3 parentScale = anchor.transform.lossyScale;
                        markers[markerIndex].transform.localScale = new Vector3(
                            parentScale.x > 1e-4f ? markerSize / parentScale.x : markerSize,
                            parentScale.y > 1e-4f ? markerSize / parentScale.y : markerSize,
                            parentScale.z > 1e-4f ? markerSize / parentScale.z : markerSize
                        );
                    }
                }
            }

            Debug.Log($"[MeasurementManager] End Drag Marker {markerIndex}. Anchored at new location.");
        }

        // =========================================================
        // SCREEN -> AR SURFACE
        // =========================================================

        public void TryMeasureAtScreenPoint(
            Vector2 screenPoint)
        {
            if (EventSystem.current != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = screenPoint
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count > 0)
                {
                    Debug.Log($"[MeasurementManager] Tap at {screenPoint} blocked by UI: {results[0].gameObject.name}");
                    return;
                }
            }

            if (ARManager.Instance != null)
            {
                if (ARManager.Instance.PerformRaycast(screenPoint, out Pose hitPose, out ARPlane hitPlane))
                {
                    AddMeasurementPoint(hitPose, hitPlane);
                }
                return;
            }

            ResolveARReferences();

            if (arRaycastManager == null)
            {
                Debug.LogError(
                    "[MeasurementManager] " +
                    "Cannot raycast: " +
                    "ARRaycastManager is missing."
                );

                return;
            }

            if (arCamera == null)
            {
                Debug.LogError(
                    "[MeasurementManager] " +
                    "Cannot raycast: " +
                    "AR Camera is missing."
                );

                return;
            }

            raycastHits.Clear();

            bool hitSurface =
                arRaycastManager.Raycast(
                    screenPoint,
                    raycastHits,
                    TrackableType.PlaneWithinPolygon
                );

            if (!hitSurface ||
                raycastHits.Count == 0)
            {
                Debug.Log(
                    "[MeasurementManager] " +
                    "No AR plane hit at " +
                    screenPoint
                );

                return;
            }

            Pose fallbackPose =
                raycastHits[0].pose;

            AddMeasurementPoint(
                fallbackPose,
                raycastHits[0].trackable as ARPlane
            );
        }

        // =========================================================
        // MODE
        // =========================================================

        public void SetMeasurementMode(
            MeasurementMode mode)
        {
            if (currentMode == mode)
                return;

            currentMode =
                mode;

            ResetActiveMeasurement();

            if (OnModeChanged != null)
                OnModeChanged.Invoke(
                    currentMode
                );
        }

        // =========================================================
        // UNIT
        // =========================================================

        public void SetMeasurementUnit(
            MeasurementUnit unit)
        {
            currentUnit =
                unit;

            NotifyDistanceChanged();
        }

        // =========================================================
        // ADD MEASUREMENT POINT
        // =========================================================

        public void AddMeasurementPoint(
            Pose hitPose,
            ARPlane hitPlane = null)
        {
            // -----------------------------------------------------
            // DISTANCE MODE
            //
            // IMPORTANT:
            // Do NOT reset after two points.
            //
            // CLEAR button is responsible for starting a new
            // measurement.
            // -----------------------------------------------------

            Pose effectivePose = hitPose;

            if (currentMode ==
                MeasurementMode.Height)
            {
                if (measurementPoints.Count >= 2 || markers.Count >= 2)
                {
                    Debug.Log(
                        "[MeasurementManager] " +
                        "Height mode already has 2 points. " +
                        "Ignoring additional tap."
                    );

                    return;
                }
            }

            // -----------------------------------------------------
            // ADD WORLD POINT
            // -----------------------------------------------------

            measurementPoints.Add(
                effectivePose.position
            );

            // -----------------------------------------------------
            // CREATE BLUE POINT (NATIVE PLANE-ATTACHED ANCHOR)
            // -----------------------------------------------------

            CreateMarker(
                effectivePose,
                hitPlane
            );

            // -----------------------------------------------------
            // UPDATE BLUE LINE
            // -----------------------------------------------------

            UpdateMeasurementVisuals();

            // -----------------------------------------------------
            // CALCULATE DISTANCE / AREA
            // -----------------------------------------------------

            CalculateMeasurement();

            if (currentMode == MeasurementMode.Area && measurementPoints.Count >= 4)
            {
                if (CheckSelfIntersection(measurementPoints.ToArray()))
                {
                    SmartARMeasure.UI.NotificationToastController.Instance?.ShowToast(
                        "Please mark points around the boundary in sequence."
                    );
                }
            }

            if (measurementPoints.Count >= 2)
            {
                if (SmartARMeasure.UI.AROverlayUIController.Instance != null)
                {
                    SmartARMeasure.UI.AROverlayUIController.Instance.ShowCustomHint("Tap a marker to move it", 10.0f);
                }
            }

            Debug.Log(
                "[MeasurementManager] " +
                "Point added. Count = " +
                measurementPoints.Count
            );
        }

        public void AddMeasurementPoint(
            Vector3 worldPoint)
        {
            AddMeasurementPoint(
                new Pose(worldPoint, Quaternion.identity),
                null);
        }

        // =========================================================
        // RESET CURRENT MEASUREMENT
        // =========================================================

        public void ResetActiveMeasurement()
        {
            measurementPoints.Clear();

            currentValue = 0f;

            DestroyVisuals();

            NotifyDistanceChanged();

            if (OnMeasurementCleared != null)
                OnMeasurementCleared.Invoke();

            Debug.Log(
                "[MeasurementManager] " +
                "Measurement cleared."
            );
        }

        // =========================================================
        // CLEAR ALL HISTORY
        // =========================================================

        public void ClearAllHistory()
        {
            historyRecords.Clear();

            SaveHistory();
        }

        // =========================================================
        // SAVE CURRENT MEASUREMENT
        // =========================================================

        public void SaveCurrentMeasurement()
        {
            if (measurementPoints.Count < 2)
                return;

            MeasurementRecord record =
                new MeasurementRecord();

            record.title =
                GetModeTitle();

            record.totalDistanceMeters =
                CalculateTotalDistance();

            record.unitUsed =
                currentUnit;

            record.modeUsed =
                currentMode;

            for (
                int i = 0;
                i < measurementPoints.Count - 1;
                i++
            )
            {
                MeasurementSegment segment =
                    new MeasurementSegment(
                        measurementPoints[i],
                        measurementPoints[i + 1]
                    );

                record.segments.Add(
                    segment
                );
            }

            historyRecords.Add(
                record
            );

            SaveHistory();

            if (OnMeasurementSaved != null)
                OnMeasurementSaved.Invoke(
                    record
                );

            Debug.Log(
                "[MeasurementManager] " +
                "Measurement saved."
            );
        }

        // =========================================================
        // DELETE HISTORY RECORD
        // =========================================================

        public void DeleteHistoryRecord(
            string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            for (
                int i = historyRecords.Count - 1;
                i >= 0;
                i--
            )
            {
                if (
                    historyRecords[i] != null &&
                    historyRecords[i].id == id
                )
                {
                    historyRecords.RemoveAt(i);
                    break;
                }
            }

            SaveHistory();
        }

        // =========================================================
        // UNDO LAST POINT
        // =========================================================

        public void UndoLastPoint()
        {
            Debug.Log($"[MeasurementManager] Undo triggered. Current points: {measurementPoints.Count}, Markers: {markers.Count}, Mode: {currentMode}");

            if (measurementPoints.Count == 0 && markers.Count == 0)
            {
                Debug.Log("[MeasurementManager] Undo ignored: No active points/markers to undo.");
                return;
            }

            // Remove last measurement point.
            if (measurementPoints.Count > 0)
            {
                measurementPoints.RemoveAt(
                    measurementPoints.Count - 1
                );
            }

            // Remove matching blue marker and its anchor.
            if (markers.Count > 0)
            {
                GameObject marker =
                    markers[markers.Count - 1];

                if (marker != null)
                {
                    if (marker.transform.parent != null && marker.transform.parent.GetComponent<ARAnchor>() != null)
                    {
                        Destroy(marker.transform.parent.gameObject);
                    }
                    else
                    {
                        Destroy(marker);
                    }
                }

                markers.RemoveAt(
                    markers.Count - 1
                );
            }

            // Rebuild line state.
            UpdateMeasurementVisuals();

            // Recalculate.
            CalculateMeasurement();

            Debug.Log(
                $"[MeasurementManager] Point undone successfully. Remaining points: {measurementPoints.Count}, Markers: {markers.Count}"
            );
        }

        // =========================================================
        // CALCULATE MEASUREMENT
        // =========================================================

        private void CalculateMeasurement()
        {
            currentValue = 0f;

            if (measurementPoints.Count < 2)
            {
                NotifyDistanceChanged();
                return;
            }

            switch (currentMode)
            {
                case MeasurementMode.Distance:
                case MeasurementMode.Continuous:

                    currentValue =
                        CalculateTotalDistance();

                    break;

                case MeasurementMode.Height:

                    Vector3 worldUp = (ARManager.Instance != null && ARManager.Instance.Origin != null)
                        ? ARManager.Instance.Origin.transform.up
                        : Vector3.up;
                    Vector3 delta = measurementPoints[1] - measurementPoints[0];
                    currentValue = Mathf.Abs(Vector3.Dot(delta, worldUp));
                    Debug.Log($"[MeasurementManager] HEIGHT CALCULATION: Point A={measurementPoints[0]}, Point B={measurementPoints[1]}, Delta={delta}, WorldUp={worldUp}, Height={currentValue:F4}m");

                    break;

                case MeasurementMode.Area:

                    currentValue =
                        CalculateArea();

                    break;
            }

            NotifyDistanceChanged();
        }

        // =========================================================
        // TOTAL POLYLINE DISTANCE
        // =========================================================

        private float CalculateTotalDistance()
        {
            if (measurementPoints.Count < 2)
                return 0f;

            float total = 0f;

            for (
                int i = 0;
                i < measurementPoints.Count - 1;
                i++
            )
            {
                total +=
                    Vector3.Distance(
                        measurementPoints[i],
                        measurementPoints[i + 1]
                    );
            }

            return total;
        }

        // =========================================================
        // AREA (NEWELL'S 3D STOKES METHOD - DIRECTION INDEPENDENT)
        // =========================================================

        private float CalculateArea()
        {
            int n = measurementPoints.Count;
            if (n < 3)
                return 0f;

            Vector3 normal = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 current = measurementPoints[i];
                Vector3 next = measurementPoints[(i + 1) % n];

                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }

            return normal.magnitude * 0.5f;
        }

        // =========================================================
        // SELF-INTERSECTION DETECTION
        // =========================================================

        private bool CheckSelfIntersection(Vector3[] pts)
        {
            int n = pts.Length;
            if (n < 4) return false;

            Vector3 normal = Vector3.zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 c = pts[i];
                Vector3 nxt = pts[(i + 1) % n];
                normal.x += (c.y - nxt.y) * (c.z + nxt.z);
                normal.y += (c.z - nxt.z) * (c.x + nxt.x);
                normal.z += (c.x - nxt.x) * (c.y + nxt.y);
            }

            if (normal.sqrMagnitude < 1e-6f) return false;

            Vector3 nrm = normal.normalized;
            Vector3 uAxis = Vector3.Cross(nrm, Mathf.Abs(nrm.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 vAxis = Vector3.Cross(nrm, uAxis);

            Vector2[] p2d = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                p2d[i] = new Vector2(Vector3.Dot(pts[i], uAxis), Vector3.Dot(pts[i], vAxis));
            }

            for (int i = 0; i < n; i++)
            {
                Vector2 a1 = p2d[i];
                Vector2 a2 = p2d[(i + 1) % n];

                for (int j = i + 2; j < n; j++)
                {
                    if (i == 0 && j == n - 1) continue;

                    Vector2 b1 = p2d[j];
                    Vector2 b2 = p2d[(j + 1) % n];

                    if (SegmentsIntersect2D(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool SegmentsIntersect2D(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
            if (Mathf.Abs(d) < 1e-6f) return false;

            float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            return (u > 0.001f && u < 0.999f && v > 0.001f && v < 0.999f);
        }

        // =========================================================
        // DISTANCE EVENT
        // =========================================================

        private void NotifyDistanceChanged()
        {
            if (OnDistanceUpdated != null)
                OnDistanceUpdated.Invoke(
                    currentValue
                );
        }

        // =========================================================
        // =========================================================
        // MATERIAL HELPER
        // =========================================================

        private Material GetMeasurementMaterial(Color color)
        {
            Material material = null;
            string resolvedShaderName = "none";

            // 1. Try pre-compiled Resources asset
            Material resMat = Resources.Load<Material>("MeasurementMarkerMat");
            if (resMat != null)
            {
                material = new Material(resMat);
                resolvedShaderName = "Resources/MeasurementMarkerMat (" + (resMat.shader != null ? resMat.shader.name : "null") + ")";
            }

            // 2. Candidate shader fallback chain
            if (material == null)
            {
                string[] candidateShaderNames = new string[]
                {
                    "SmartARMeasure/MeasurementOverlay",
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Color",
                    "Sprites/Default",
                    "UI/Default",
                    "Mobile/Unlit (Supports Lightmap)",
                    "Legacy Shaders/Diffuse",
                    "Standard"
                };

                foreach (string shaderName in candidateShaderNames)
                {
                    Shader s = Shader.Find(shaderName);
                    if (s != null)
                    {
                        material = new Material(s);
                        resolvedShaderName = shaderName;
                        break;
                    }
                }
            }

            if (material != null)
            {
                material.name = "MeasurementMat_" + resolvedShaderName.Replace('/', '_').Replace(' ', '_');

                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);

                try { material.color = color; }
                catch { /* ignore */ }

                // Render in Geometry+600 (2600) so it renders immediately after AR background (BeforeForwardOpaque)
                material.renderQueue = 2600;

                if (material.HasProperty("_ZTest"))
                {
                    material.SetInt(
                        "_ZTest",
                        (int)UnityEngine.Rendering.CompareFunction.Always
                    );
                }
            }
            else
            {
                Debug.LogError(
                    "[MeasurementVisual] CRITICAL: ALL shaders returned null. Creating fallback unlit material!"
                );
                material = new Material(Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Diffuse"));
                material.renderQueue = 2600;
            }

            Debug.Log(
                $"[MeasurementVisual] Shader resolved: {resolvedShaderName} | material null: {material == null}"
            );

            return material;
        }

        // =========================================================
        // PROCEDURAL SPHERE HELPER (SAFETY FALLBACK)
        // =========================================================

        private static Mesh cachedSphereMesh = null;

        private Mesh GetOrCreateSphereMesh()
        {
            if (cachedSphereMesh != null)
                return cachedSphereMesh;

            Mesh mesh = new Mesh();
            mesh.name = "MeasurementSphereMesh";

            int lon = 20;
            int lat = 20;
            float radius = 0.5f;

            Vector3[] vertices = new Vector3[(lon + 1) * (lat + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[lon * lat * 6];

            float pi = Mathf.PI;
            float _2pi = pi * 2f;

            for (int i = 0; i <= lat; i++)
            {
                float v = (float)i / lat;
                float latitude = (v - 0.5f) * pi;

                for (int j = 0; j <= lon; j++)
                {
                    float u = (float)j / lon;
                    float longitude = u * _2pi;

                    int index = i * (lon + 1) + j;

                    float x = Mathf.Cos(latitude) * Mathf.Cos(longitude);
                    float y = Mathf.Sin(latitude);
                    float z = Mathf.Cos(latitude) * Mathf.Sin(longitude);

                    vertices[index] = new Vector3(x, y, z) * radius;
                    uvs[index] = new Vector2(u, v);
                    colors[index] = Color.white;
                }
            }

            int triIndex = 0;
            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int current = i * (lon + 1) + j;
                    int next = current + lon + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;

                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            cachedSphereMesh = mesh;
            return cachedSphereMesh;
        }

        private void CreateMarker(
            Pose hitPose,
            ARPlane hitPlane = null)
        {
            SubscribeAppSettings();

            GameObject marker =
                new GameObject("MeasurementMarker_" + markers.Count);

            marker.layer = 0; // Default layer
            marker.transform.localScale = Vector3.one * markerSize;

            // -----------------------------------------------------
            // ATTACH TO REAL PERSISTENT AR TRACKING ANCHOR
            // -----------------------------------------------------
            ARAnchor anchor = null;
            if (ARManager.Instance != null && ARManager.Instance.AnchorManager != null && ARManager.Instance.AnchorManager.enabled)
            {
                if (hitPlane != null)
                {
                    anchor = ARManager.Instance.AnchorManager.AttachAnchor(hitPlane, hitPose);
                }

                if (anchor == null)
                {
                    GameObject anchorObj = new GameObject("Anchor_" + markers.Count);
                    if (ARManager.Instance.Origin != null && ARManager.Instance.Origin.TrackablesParent != null)
                    {
                        anchorObj.transform.SetParent(ARManager.Instance.Origin.TrackablesParent, false);
                    }
                    anchorObj.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                    anchor = anchorObj.AddComponent<ARAnchor>();
                }
            }

            if (anchor != null)
            {
                marker.transform.SetParent(anchor.transform, false);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;

                // Compensate for parent lossy scale so marker physical size is strictly markerSize in world space
                Vector3 parentScale = anchor.transform.lossyScale;
                marker.transform.localScale = new Vector3(
                    parentScale.x > 1e-4f ? markerSize / parentScale.x : markerSize,
                    parentScale.y > 1e-4f ? markerSize / parentScale.y : markerSize,
                    parentScale.z > 1e-4f ? markerSize / parentScale.z : markerSize
                );

                Debug.Log($"[MeasurementManager] Marker attached to persistent ARAnchor: {anchor.name} (Plane: {(hitPlane != null ? hitPlane.trackableId.ToString() : "free-anchor")})");
            }
            else
            {
                marker.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
                if (ARManager.Instance != null && ARManager.Instance.Origin != null && ARManager.Instance.Origin.TrackablesParent != null)
                {
                    marker.transform.SetParent(ARManager.Instance.Origin.TrackablesParent, true);
                }
                else
                {
                    marker.transform.SetParent(null, true);
                }

                if (marker.transform.parent != null)
                {
                    Vector3 parentScale = marker.transform.parent.lossyScale;
                    marker.transform.localScale = new Vector3(
                        parentScale.x > 1e-4f ? markerSize / parentScale.x : markerSize,
                        parentScale.y > 1e-4f ? markerSize / parentScale.y : markerSize,
                        parentScale.z > 1e-4f ? markerSize / parentScale.z : markerSize
                    );
                }
                else
                {
                    marker.transform.localScale = Vector3.one * markerSize;
                }
            }

            MeshFilter filter = marker.AddComponent<MeshFilter>();
            filter.sharedMesh = GetOrCreateSphereMesh();

            MeshRenderer renderer =
                marker.AddComponent<MeshRenderer>();

            Color effectiveColor = AppSettings.Instance != null ? AppSettings.Instance.ThemeColor : markerColor;
            Material mat = GetMeasurementMaterial(effectiveColor);

            if (renderer != null)
            {
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortingOrder = 1000;
                renderer.enabled = true;
            }

            marker.SetActive(true);

            markers.Add(
                marker
            );

            if (markers.Count == 1)
            {
                Debug.Log($"[MeasurementVisual] POINT CREATED at {hitPose.position}");
            }
            else if (markers.Count == 2)
            {
                Debug.Log($"[MeasurementVisual] SECOND POINT CREATED at {hitPose.position}");
            }
            else
            {
                Debug.Log($"[MeasurementVisual] POINT {markers.Count} CREATED at {hitPose.position}");
            }

            Debug.Log($"[MeasurementVisual] POINT ACTIVE = {marker.activeInHierarchy}");
            Debug.Log($"[MeasurementVisual] VISUAL POINT POSITION = {marker.transform.position}");
            Debug.Log($"[MeasurementVisual] POINT SCALE = {marker.transform.localScale}");
            Debug.Log($"[MeasurementVisual] POINT MATERIAL = {(mat != null ? mat.name : "null")}");
            Debug.Log($"[MeasurementVisual] POINT SHADER = {(mat != null && mat.shader != null ? mat.shader.name : "null")}");
            Debug.Log($"[MeasurementVisual] POINT RENDERER ENABLED = {(renderer != null ? renderer.enabled.ToString() : "false")}");
            Debug.Log($"[MeasurementVisual] POINT LAYER = {marker.layer}");
        }

        private void CreateMarker(
            Vector3 position)
        {
            CreateMarker(new Pose(position, Quaternion.identity), null);
        }

        // =========================================================
        // UPDATE MEASUREMENT VISUALS
        // =========================================================

        private void UpdateMeasurementVisuals()
        {
            // -----------------------------------------------------
            // Less than two points = no line.
            // -----------------------------------------------------

            if (measurementPoints.Count < 2)
            {
                DestroyLine();
                return;
            }

            // -----------------------------------------------------
            // Create line if necessary.
            // -----------------------------------------------------

            if (lineRenderer == null)
            {
                CreateLine();
            }

            // -----------------------------------------------------
            // DISTANCE / CONTINUOUS
            // -----------------------------------------------------

            if (currentMode ==
                MeasurementMode.Distance ||
                currentMode ==
                MeasurementMode.Continuous)
            {
                int count = measurementPoints.Count;
                lineRenderer.positionCount =
                    count;

                for (
                    int i = 0;
                    i < count;
                    i++
                )
                {
                    lineRenderer.SetPosition(
                        i,
                        measurementPoints[i]
                    );
                }

                Debug.Log("[MeasurementVisual] LINE UPDATED");
                LogLineDiagnostics();

                return;
            }

            // -----------------------------------------------------
            // HEIGHT
            // -----------------------------------------------------

            if (currentMode ==
                MeasurementMode.Height)
            {
                lineRenderer.positionCount = 2;

                lineRenderer.SetPosition(
                    0,
                    measurementPoints[0]
                );

                lineRenderer.SetPosition(
                    1,
                    measurementPoints[1]
                );

                Debug.Log("[MeasurementVisual] LINE UPDATED");
                LogLineDiagnostics();

                return;
            }

            // -----------------------------------------------------
            // AREA
            // -----------------------------------------------------

            if (currentMode ==
                MeasurementMode.Area)
            {
                int count = measurementPoints.Count;
                lineRenderer.positionCount = count + 1;

                for (int i = 0; i < count; i++)
                {
                    lineRenderer.SetPosition(i, measurementPoints[i]);
                }

                lineRenderer.SetPosition(count, measurementPoints[0]);

                Debug.Log("[MeasurementVisual] LINE UPDATED");
                LogLineDiagnostics();
            }
        }

        private void LogLineDiagnostics()
        {
            if (lineRenderer == null) return;
            Debug.Log($"[MeasurementVisual] LINE ENABLED = {lineRenderer.enabled}");
            Debug.Log($"[MeasurementVisual] LINE WORLD SPACE = {lineRenderer.useWorldSpace}");
            Debug.Log($"[MeasurementVisual] LINE WIDTH = {lineRenderer.startWidth}");
            Debug.Log($"[MeasurementVisual] LINE MATERIAL = {(lineRenderer.material != null ? lineRenderer.material.name : "null")}");
            Debug.Log($"[MeasurementVisual] LINE SHADER = {(lineRenderer.material != null && lineRenderer.material.shader != null ? lineRenderer.material.shader.name : "null")}");
            if (lineRenderer.positionCount >= 2)
            {
                Debug.Log($"[MeasurementVisual] LINE POSITION 0 = {lineRenderer.GetPosition(0)}");
                Debug.Log($"[MeasurementVisual] LINE POSITION 1 = {lineRenderer.GetPosition(1)}");
            }
        }

        // =========================================================
        // CREATE LINE
        // =========================================================

        private void CreateLine()
        {
            SubscribeAppSettings();

            measurementLine =
                new GameObject(
                    "MeasurementLine"
                );

            measurementLine.layer = 0; // Default layer

            lineRenderer =
                measurementLine.AddComponent<
                    LineRenderer
                >();

            lineRenderer.useWorldSpace =
                true;

            float effectiveWidth = AppSettings.Instance != null ? AppSettings.Instance.LineThickness : lineWidth;
            effectiveWidth = Mathf.Max(effectiveWidth, 0.0005f);
            lineRenderer.startWidth =
                effectiveWidth;

            lineRenderer.endWidth =
                effectiveWidth;

            Color effectiveColor = AppSettings.Instance != null ? AppSettings.Instance.ThemeColor : lineColor;
            lineRenderer.startColor =
                effectiveColor;

            lineRenderer.endColor =
                effectiveColor;

            lineRenderer.alignment =
                LineAlignment.View;

            lineRenderer.numCapVertices =
                8;

            lineRenderer.numCornerVertices =
                8;

            lineRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            lineRenderer.receiveShadows =
                false;

            Material lineMat = GetMeasurementMaterial(effectiveColor);
            lineRenderer.material = lineMat;
            lineRenderer.sortingOrder = 1000;
            lineRenderer.enabled = true;

            measurementLine.SetActive(true);

            Debug.Log("[MeasurementVisual] LINE CREATED");
        }

        // =========================================================
        // DESTROY LINE
        // =========================================================

        private void DestroyLine()
        {
            if (measurementLine != null)
            {
                Destroy(
                    measurementLine
                );
            }

            measurementLine = null;
            lineRenderer = null;
        }

        // =========================================================
        // DESTROY ALL VISUALS
        // =========================================================

        private void DestroyVisuals()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i] != null)
                {
                    if (markers[i].transform.parent != null && markers[i].transform.parent.GetComponent<ARAnchor>() != null)
                    {
                        Destroy(markers[i].transform.parent.gameObject);
                    }
                    else
                    {
                        Destroy(markers[i]);
                    }
                }
            }

            markers.Clear();

            DestroyLine();
        }

        // =========================================================
        // MODE TITLE
        // =========================================================

        private string GetModeTitle()
        {
            switch (currentMode)
            {
                case MeasurementMode.Distance:
                    return "Distance";

                case MeasurementMode.Continuous:
                    return "Continuous";

                case MeasurementMode.Area:
                    return "Area";

                case MeasurementMode.Height:
                    return "Height";
            }

            return "Measurement";
        }

        // =========================================================
        // HISTORY SAVE
        // =========================================================

        private void SaveHistory()
        {
            MeasurementHistoryWrapper wrapper =
                new MeasurementHistoryWrapper();

            wrapper.records =
                historyRecords;

            string json =
                JsonUtility.ToJson(
                    wrapper
                );

            PlayerPrefs.SetString(
                "SmartARMeasure_History",
                json
            );

            PlayerPrefs.Save();
        }

        // =========================================================
        // HISTORY LOAD
        // =========================================================

        private void LoadHistory()
        {
            string json =
                PlayerPrefs.GetString(
                    "SmartARMeasure_History",
                    ""
                );

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                MeasurementHistoryWrapper wrapper =
                    JsonUtility.FromJson<
                        MeasurementHistoryWrapper
                    >(json);

                if (
                    wrapper != null &&
                    wrapper.records != null
                )
                {
                    historyRecords =
                        wrapper.records;
                }
            }
            catch
            {
                historyRecords =
                    new List<MeasurementRecord>();
            }
        }

        // =========================================================
        // PUBLIC HELPERS
        // =========================================================

        public float GetCurrentMeasurement()
        {
            return currentValue;
        }

        public string GetCurrentUnit()
        {
            if (currentMode ==
                MeasurementMode.Area)
            {
                return currentUnit ==
                    MeasurementUnit.Metric
                    ? "m²"
                    : "ft²";
            }

            return currentUnit ==
                MeasurementUnit.Metric
                ? "m"
                : "ft";
        }
    }
}