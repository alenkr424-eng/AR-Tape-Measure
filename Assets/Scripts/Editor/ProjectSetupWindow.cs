#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;

namespace SmartARMeasure.EditorTools
{
    /// <summary>
    /// Master Unity Editor Tool for building the complete Smart AR Measure application hierarchy.
    /// Creates AR Foundation 6.x components, managers, and neat Material Design 3 UI controls for all 5 screens.
    /// </summary>
    public class ProjectSetupWindow : EditorWindow
    {
        [MenuItem("Tools/Smart AR Measure/Build Complete Scene & Project Hierarchy")]
        public static void GenerateProjectHierarchy()
        {
            if (EditorUtility.DisplayDialog("Setup Smart AR Measure Project",
                "This will build the complete AR Foundation 6.x Scene Hierarchy with fully populated UI controls for all screens. Continue?",
                "Build Complete Project", "Cancel"))
            {
                BuildScene();
            }
        }

        private static void BuildScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- 1. CORE MANAGERS ----
            GameObject coreRoot = new GameObject("--- CORE MANAGERS ---");
            Add<Core.AppManager>(coreRoot, "AppManager");
            Add<Models.AppSettings>(coreRoot, "AppSettings");
            Add<Utilities.PermissionHandler>(coreRoot, "PermissionHandler");
            Add<Utilities.ScreenshotUtility>(coreRoot, "ScreenshotUtility");
            Add<UI.NotificationToastController>(coreRoot, "NotificationToastController");
            Add<Core.MeasurementManager>(coreRoot, "MeasurementManager");

            // ---- 2. AR FOUNDATION 6.X ----
            GameObject arRoot = new GameObject("--- AR FOUNDATION 6.X ---");
            GameObject arSessionObj = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
            arSessionObj.transform.SetParent(arRoot.transform);

            GameObject xrOriginObj = new GameObject("XR Origin", typeof(XROrigin), typeof(ARPlaneManager), typeof(ARRaycastManager), typeof(ARAnchorManager));
            xrOriginObj.transform.SetParent(arRoot.transform);
            XROrigin xrOrigin = xrOriginObj.GetComponent<XROrigin>();

            GameObject cameraObj = new GameObject("AR Camera", typeof(Camera), typeof(ARCameraManager), typeof(ARCameraBackground));
            cameraObj.transform.SetParent(xrOriginObj.transform);
            Camera arCam = cameraObj.GetComponent<Camera>();
            arCam.tag = "MainCamera";
            arCam.clearFlags = CameraClearFlags.Color;
            arCam.backgroundColor = Color.black;
            arCam.nearClipPlane = 0.1f;
            arCam.farClipPlane = 1000f;
            xrOrigin.Camera = arCam;

            ARPlaneManager planeMgr = xrOriginObj.GetComponent<ARPlaneManager>();
            planeMgr.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal | UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;

            Add<Core.ARManager>(arRoot, "ARManager");

            // ---- 3. UI CANVAS SYSTEM ----
            GameObject uiRoot = new GameObject("--- UI CANVAS SYSTEM ---");
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)).transform.SetParent(uiRoot.transform);

            GameObject canvasObj = new GameObject("Main Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(uiRoot.transform);
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject uiMgrObj = Add<UI.UIManager>(uiRoot, "UIManager");

            // Create 5 Screen Panels
            GameObject homePanel    = MakePanel(canvasObj, "HomePanel",       typeof(UI.HomeScreenController),    visible: true);
            GameObject arPanel      = MakePanel(canvasObj, "AROverlayPanel",   typeof(UI.AROverlayUIController),   visible: false);
            GameObject settingsPanel= MakePanel(canvasObj, "SettingsPanel",    typeof(UI.SettingsUIController),    visible: false);
            GameObject historyPanel = MakePanel(canvasObj, "HistoryPanel",     typeof(UI.HistoryUIController),     visible: false);
            GameObject aboutPanel   = MakePanel(canvasObj, "AboutPanel",       typeof(UI.AboutUIController),       visible: false);

            // ---- BUILD RICH UI CONTENT ----
            BuildHomeUI(homePanel);
            BuildAROverlayUI(arPanel);
            BuildSettingsUI(settingsPanel);
            BuildHistoryUI(historyPanel);
            BuildAboutUI(aboutPanel);

            // Wire UIManager
            var uiMgrSO = new SerializedObject(uiMgrObj.GetComponent<UI.UIManager>());
            uiMgrSO.FindProperty("homePanelGroup").objectReferenceValue        = homePanel.GetComponent<CanvasGroup>();
            uiMgrSO.FindProperty("arOverlayPanelGroup").objectReferenceValue   = arPanel.GetComponent<CanvasGroup>();
            uiMgrSO.FindProperty("settingsPanelGroup").objectReferenceValue    = settingsPanel.GetComponent<CanvasGroup>();
            uiMgrSO.FindProperty("historyPanelGroup").objectReferenceValue     = historyPanel.GetComponent<CanvasGroup>();
            uiMgrSO.FindProperty("aboutPanelGroup").objectReferenceValue       = aboutPanel.GetComponent<CanvasGroup>();
            uiMgrSO.ApplyModifiedProperties();

            // Save Scene
            string scenePath = "Assets/Scenes/MainScene.unity";
            if (!System.IO.Directory.Exists("Assets/Scenes"))
                System.IO.Directory.CreateDirectory("Assets/Scenes");

            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00E5FF><b>[Smart AR Measure]</b> Scene built successfully at: {scenePath}</color>");
            EditorUtility.DisplayDialog("Complete", $"Smart AR Measure scene built with full UI at:\n{scenePath}", "OK");
        }

        static GameObject Add<T>(GameObject parent, string name) where T : Component
        {
            GameObject go = new GameObject(name, typeof(T));
            go.transform.SetParent(parent.transform);
            return go;
        }

        static GameObject MakePanel(GameObject canvas, string name, System.Type ctrl, bool visible)
        {
            var p = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), ctrl);
            p.transform.SetParent(canvas.transform, false);
            var r = p.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var cg = p.GetComponent<CanvasGroup>();
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
            return p;
        }

        // ==================== HOME SCREEN UI ====================
        static void BuildHomeUI(GameObject panel)
        {
            var so = new SerializedObject(panel.GetComponent<UI.HomeScreenController>());

            // 1. Full-Screen Dashboard Background Container (UI background scoped to HomePanel)
            var bgObj = MakeBox(panel, "DashboardBackground", Vector2.zero, new Vector2(1080, 2400), new Color(0.03f, 0.05f, 0.08f, 1f));
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            // Subtle Tech Grid Lines Accent
            MakeBox(bgObj, "GridLineH", new Vector2(0, 0), new Vector2(960, 2), new Color(0f, 0.9f, 1f, 0.12f));
            MakeBox(bgObj, "GridLineV", new Vector2(0, 0), new Vector2(2, 1600), new Color(0f, 0.9f, 1f, 0.12f));

            // Viewfinder Corner Brackets
            MakeBox(bgObj, "CornerTL", new Vector2(-420, 850), new Vector2(80, 4), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerTL_V", new Vector2(-458, 812), new Vector2(4, 80), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerTR", new Vector2(420, 850), new Vector2(80, 4), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerTR_V", new Vector2(458, 812), new Vector2(4, 80), new Color(0f, 0.9f, 1f, 0.35f));

            MakeBox(bgObj, "CornerBL", new Vector2(-420, -850), new Vector2(80, 4), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerBL_V", new Vector2(-458, -812), new Vector2(4, 80), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerBR", new Vector2(420, -850), new Vector2(80, 4), new Color(0f, 0.9f, 1f, 0.35f));
            MakeBox(bgObj, "CornerBR_V", new Vector2(458, -812), new Vector2(4, 80), new Color(0f, 0.9f, 1f, 0.35f));

            // Status Pill Badge
            var statusPill = MakeBox(bgObj, "StatusBadge", new Vector2(0, 720), new Vector2(360, 60), new Color(0.06f, 0.15f, 0.22f, 0.8f));
            MakeText(statusPill, "BadgeText", "SYSTEM ACTIVE • ARCORE", 20, FontStyles.Bold, Vector2.zero, new Vector2(340, 50), new Color(0f, 0.9f, 1f));

            // Title Header Card
            var headerCard = MakeBox(panel, "HeaderCard", new Vector2(0, 450), new Vector2(920, 320), new Color(0.06f, 0.09f, 0.14f, 0.92f));
            var titleObj   = MakeText(headerCard, "TitleText", "Smart AR Measure", 64, FontStyles.Bold, new Vector2(0, 50), new Vector2(850, 120), new Color(0f, 0.9f, 1f));
            var subObj     = MakeText(headerCard, "SubtitleText", "Measure the real world with precision", 30, FontStyles.Normal, new Vector2(0, -60), new Vector2(850, 80), new Color(0.75f, 0.85f, 0.95f));

            so.FindProperty("appTitleText").objectReferenceValue    = titleObj.GetComponent<TextMeshProUGUI>();
            so.FindProperty("appSubtitleText").objectReferenceValue = subObj.GetComponent<TextMeshProUGUI>();

            // Main Primary Action
            var startBtn = MakeButton(panel, "StartButton", "START MEASURING", new Vector2(0, 0), new Vector2(760, 160), new Color(0f, 0.65f, 0.9f));
            so.FindProperty("startMeasuringButton").objectReferenceValue = startBtn.GetComponent<Button>();

            // Navigation Row
            var histBtn = MakeButton(panel, "HistoryButton", "MEASUREMENT HISTORY", new Vector2(0, -220), new Vector2(760, 125), new Color(0.15f, 0.18f, 0.24f));
            so.FindProperty("historyButton").objectReferenceValue = histBtn.GetComponent<Button>();

            var setBtn = MakeButton(panel, "SettingsButton", "SETTINGS", new Vector2(-200, -390), new Vector2(360, 120), new Color(0.12f, 0.15f, 0.2f));
            so.FindProperty("settingsButton").objectReferenceValue = setBtn.GetComponent<Button>();

            var abtBtn = MakeButton(panel, "AboutButton", "ABOUT", new Vector2(200, -390), new Vector2(360, 120), new Color(0.12f, 0.15f, 0.2f));
            so.FindProperty("aboutButton").objectReferenceValue = abtBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        // ==================== AR OVERLAY UI ====================
        static void BuildAROverlayUI(GameObject panel)
        {
            var so = new SerializedObject(panel.GetComponent<UI.AROverlayUIController>());

            // Top Bar Card
            var topBar   = MakeBox(panel, "TopBar", new Vector2(0, 1020), new Vector2(1040, 160), new Color(0.04f, 0.06f, 0.1f, 0.88f));
            var homeBtn  = MakeButton(topBar, "HomeBtn",       "HOME",    new Vector2(-420, 0), new Vector2(150, 100), new Color(0.18f, 0.22f, 0.28f));
            var saveBtn  = MakeButton(topBar, "SaveBtn",       "SAVE",    new Vector2(-250, 0), new Vector2(150, 100), new Color(0.18f, 0.65f, 0.4f));
            var undoBtn  = MakeButton(topBar, "UndoBtn",       "UNDO",    new Vector2(-80, 0),  new Vector2(150, 100), new Color(0.18f, 0.22f, 0.28f));
            var resetBtn = MakeButton(topBar, "ResetBtn",      "CLEAR",   new Vector2( 90, 0),  new Vector2(150, 100), new Color(0.75f, 0.2f, 0.28f));
            var shotBtn  = MakeButton(topBar, "ScreenshotBtn", "SNAP",    new Vector2( 260, 0), new Vector2(150, 100), new Color(0.18f, 0.55f, 0.6f));
            var histBtn  = MakeButton(topBar, "HistoryBtn",    "LOGS",    new Vector2( 420, 0), new Vector2(150, 100), new Color(0.18f, 0.22f, 0.28f));

            so.FindProperty("homeButton").objectReferenceValue       = homeBtn.GetComponent<Button>();
            so.FindProperty("saveButton").objectReferenceValue       = saveBtn.GetComponent<Button>();
            so.FindProperty("resetButton").objectReferenceValue      = resetBtn.GetComponent<Button>();
            so.FindProperty("undoButton").objectReferenceValue       = undoBtn.GetComponent<Button>();
            so.FindProperty("screenshotButton").objectReferenceValue = shotBtn.GetComponent<Button>();
            so.FindProperty("historyButton").objectReferenceValue    = histBtn.GetComponent<Button>();

            // Live Measurement HUD Card
            var hudCard = MakeBox(panel, "HUDCard", new Vector2(0, 770), new Vector2(850, 220), new Color(0f, 0f, 0f, 0.75f));
            var valText = MakeText(hudCard, "DistanceValue", "0.00", 96, FontStyles.Bold, new Vector2(-90, 10), new Vector2(420, 150), Color.cyan);
            var unitText= MakeText(hudCard, "UnitLabel",     "m",    48, FontStyles.Bold, new Vector2(230, 0), new Vector2(140, 80), Color.white);
            var modeText= MakeText(hudCard, "ModeTitle",     "Distance Mode", 26, FontStyles.Italic, new Vector2(0, -75), new Vector2(800, 50), new Color(0.7f, 0.85f, 1f));

            so.FindProperty("liveDistanceHUDText").objectReferenceValue = valText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("unitLabelText").objectReferenceValue       = unitText.GetComponent<TextMeshProUGUI>();
            so.FindProperty("modeTitleHUDText").objectReferenceValue    = modeText.GetComponent<TextMeshProUGUI>();

            // Surface Hint Banner
            var hintBanner = MakeBox(panel, "TrackingHint", new Vector2(0, 500), new Vector2(860, 85), new Color(0.85f, 0.65f, 0f, 0.85f));
            var hintText   = MakeText(hintBanner, "HintText", "Point camera at surfaces and move slowly...", 30, FontStyles.Italic, Vector2.zero, new Vector2(830, 75), Color.white);
            so.FindProperty("trackingHintBanner").objectReferenceValue = hintBanner;
            so.FindProperty("trackingHintText").objectReferenceValue   = hintText.GetComponent<TextMeshProUGUI>();

            // Bottom Mode Selector Toolbar
            var botBar   = MakeBox(panel, "BottomModeBar", new Vector2(0, -940), new Vector2(980, 165), new Color(0.04f, 0.06f, 0.1f, 0.88f));
            var distBtn  = MakeButton(botBar, "DistMode",   "Distance",   new Vector2(-310, 0), new Vector2(280, 120), new Color(0f, 0.6f, 0.8f));
            var areaBtn  = MakeButton(botBar, "AreaMode",   "Area",       new Vector2(   0, 0), new Vector2(280, 120), new Color(0.18f, 0.22f, 0.28f));
            var heightBtn= MakeButton(botBar, "HeightMode", "Height",     new Vector2( 310, 0), new Vector2(280, 120), new Color(0.18f, 0.22f, 0.28f));

            so.FindProperty("distanceModeButton").objectReferenceValue = distBtn.GetComponent<Button>();
            so.FindProperty("areaModeButton").objectReferenceValue     = areaBtn.GetComponent<Button>();
            so.FindProperty("heightModeButton").objectReferenceValue   = heightBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        // ==================== SETTINGS UI (NEAT & FULLY POPULATED) ====================
        static void BuildSettingsUI(GameObject panel)
        {
            var so = new SerializedObject(panel.GetComponent<UI.SettingsUIController>());

            // Title
            MakeText(panel, "Title", "SETTINGS", 56, FontStyles.Bold, new Vector2(0, 950), new Vector2(800, 100), Color.white);

            // Card 1: Unit System Selection
            var unitCard = MakeBox(panel, "UnitCard", new Vector2(0, 720), new Vector2(920, 260), new Color(0.08f, 0.11f, 0.16f, 0.9f));
            MakeText(unitCard, "Label", "MEASUREMENT UNIT", 32, FontStyles.Bold, new Vector2(0, 70), new Vector2(850, 60), new Color(0f, 0.9f, 1f));

            var metricToggleObj   = MakeToggle(unitCard, "MetricToggle",   "Metric (m, cm, mm)", new Vector2(-210, -30), new Vector2(400, 90), true);
            var imperialToggleObj = MakeToggle(unitCard, "ImperialToggle", "Imperial (ft, in)",  new Vector2(210, -30),  new Vector2(400, 90), false);

            so.FindProperty("metricToggle").objectReferenceValue   = metricToggleObj.GetComponent<Toggle>();
            so.FindProperty("imperialToggle").objectReferenceValue = imperialToggleObj.GetComponent<Toggle>();

            // Card 2: Marker & Line Color Palette
            var colorCard = MakeBox(panel, "ColorCard", new Vector2(0, 390), new Vector2(920, 260), new Color(0.08f, 0.11f, 0.16f, 0.9f));
            MakeText(colorCard, "Label", "MARKER & LINE COLOR", 32, FontStyles.Bold, new Vector2(0, 70), new Vector2(850, 60), new Color(0f, 0.9f, 1f));

            var cyanBtn    = MakeButton(colorCard, "CyanBtn",    "Cyan",    new Vector2(-315, -30), new Vector2(190, 100), new Color(0f, 0.9f, 1f));
            var magentaBtn = MakeButton(colorCard, "MagentaBtn", "Magenta", new Vector2(-105, -30), new Vector2(190, 100), new Color(1f, 0.25f, 0.5f));
            var amberBtn   = MakeButton(colorCard, "AmberBtn",   "Amber",   new Vector2( 105, -30), new Vector2(190, 100), new Color(1f, 0.76f, 0.03f));
            var greenBtn   = MakeButton(colorCard, "GreenBtn",   "Green",   new Vector2( 315, -30), new Vector2(190, 100), new Color(0f, 0.9f, 0.46f));

            so.FindProperty("markerColorCyanBtn").objectReferenceValue    = cyanBtn.GetComponent<Button>();
            so.FindProperty("markerColorMagentaBtn").objectReferenceValue = magentaBtn.GetComponent<Button>();
            so.FindProperty("markerColorAmberBtn").objectReferenceValue   = amberBtn.GetComponent<Button>();
            so.FindProperty("markerColorGreenBtn").objectReferenceValue   = greenBtn.GetComponent<Button>();

            // Card 3: Line Thickness Slider
            var thickCard = MakeBox(panel, "ThickCard", new Vector2(0, 60), new Vector2(920, 260), new Color(0.08f, 0.11f, 0.16f, 0.9f));
            MakeText(thickCard, "Label", "LINE THICKNESS", 32, FontStyles.Bold, new Vector2(-220, 70), new Vector2(400, 60), new Color(0f, 0.9f, 1f));
            var thickValText = MakeText(thickCard, "ValText", "3.2 mm", 32, FontStyles.Bold, new Vector2(250, 70), new Vector2(300, 60), Color.yellow);

            var sliderObj = MakeSlider(thickCard, "ThicknessSlider", new Vector2(0, -30), new Vector2(800, 60), 0.001f, 0.008f, 0.0032f);
            so.FindProperty("lineThicknessSlider").objectReferenceValue    = sliderObj.GetComponent<Slider>();
            so.FindProperty("lineThicknessValueText").objectReferenceValue = thickValText.GetComponent<TextMeshProUGUI>();

            // Card 4: Audio, Haptics & Mesh Toggles
            var optsCard = MakeBox(panel, "OptsCard", new Vector2(0, -300), new Vector2(920, 320), new Color(0.08f, 0.11f, 0.16f, 0.9f));
            MakeText(optsCard, "Label", "PREFERENCES", 32, FontStyles.Bold, new Vector2(0, 100), new Vector2(850, 60), new Color(0f, 0.9f, 1f));

            var soundToggleObj = MakeToggle(optsCard, "SoundToggle", "Sound Effects", new Vector2(-230, 20), new Vector2(380, 80), true);
            var haptToggleObj  = MakeToggle(optsCard, "HapticToggle", "Vibration",     new Vector2(230, 20),  new Vector2(380, 80), true);
            var planeToggleObj = MakeToggle(optsCard, "MeshToggle",   "Show Surfaces", new Vector2(0, -70),   new Vector2(420, 80), true);

            so.FindProperty("soundFxToggle").objectReferenceValue            = soundToggleObj.GetComponent<Toggle>();
            so.FindProperty("hapticToggle").objectReferenceValue             = haptToggleObj.GetComponent<Toggle>();
            so.FindProperty("planeVisualizationToggle").objectReferenceValue = planeToggleObj.GetComponent<Toggle>();

            // Reset Defaults Button
            var resetBtn = MakeButton(panel, "ResetDefaultsBtn", "RESET TO DEFAULTS", new Vector2(0, -620), new Vector2(720, 120), new Color(0.7f, 0.2f, 0.25f));
            so.FindProperty("resetDefaultsButton").objectReferenceValue = resetBtn.GetComponent<Button>();

            // Back Button
            var backBtn = MakeButton(panel, "BackButton", "BACK TO HOME", new Vector2(0, -820), new Vector2(720, 130), new Color(0.25f, 0.28f, 0.35f));
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        // ==================== HISTORY UI ====================
        static void BuildHistoryUI(GameObject panel)
        {
            var so = new SerializedObject(panel.GetComponent<UI.HistoryUIController>());

            MakeText(panel, "Title", "MEASUREMENT HISTORY", 52, FontStyles.Bold, new Vector2(0, 950), new Vector2(950, 100), Color.white);

            // Container Scroll View Box
            var scrollContainerObj = new GameObject("HistoryScrollRect", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollContainerObj.transform.SetParent(panel.transform, false);
            var scrollRect = scrollContainerObj.GetComponent<ScrollRect>();
            var scrollImg = scrollContainerObj.GetComponent<Image>();
            scrollImg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);
            scrollImg.raycastTarget = false;

            var scrollRT = scrollContainerObj.GetComponent<RectTransform>();
            scrollRT.anchoredPosition = new Vector2(0, 100);
            scrollRT.sizeDelta = new Vector2(920, 1300);

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollContainerObj.transform, false);
            var vpRT = viewportObj.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0, 0);

            var vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
            vGroup.padding = new RectOffset(16, 16, 16, 16);
            vGroup.spacing = 16;
            vGroup.childControlWidth = true;
            vGroup.childControlHeight = false;

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            scrollRect.viewport = vpRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var emptyText = MakeText(scrollContainerObj, "EmptyText", "No measurements recorded yet.\nStart measuring to log results here!", 34, FontStyles.Italic, Vector2.zero, new Vector2(800, 200), Color.gray);

            so.FindProperty("scrollContentContainer").objectReferenceValue = contentObj.transform;
            so.FindProperty("emptyHistoryStatePanel").objectReferenceValue = emptyText;

            // Action Row
            var exportBtn = MakeButton(panel, "ExportCsvBtn", "EXPORT CSV", new Vector2(-220, -730), new Vector2(400, 120), new Color(0.18f, 0.6f, 0.4f));
            var clearBtn  = MakeButton(panel, "ClearAllBtn",  "CLEAR ALL",  new Vector2(220, -730),  new Vector2(400, 120), new Color(0.7f, 0.2f, 0.25f));

            so.FindProperty("exportCsvButton").objectReferenceValue = exportBtn.GetComponent<Button>();
            so.FindProperty("clearAllButton").objectReferenceValue  = clearBtn.GetComponent<Button>();

            var backBtn = MakeButton(panel, "BackButton", "BACK TO HOME", new Vector2(0, -900), new Vector2(720, 130), new Color(0.25f, 0.28f, 0.35f));
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        // ==================== ABOUT UI ====================
        static void BuildAboutUI(GameObject panel)
        {
            var so = new SerializedObject(panel.GetComponent<UI.AboutUIController>());

            MakeText(panel, "Title", "ABOUT", 60, FontStyles.Bold, new Vector2(0, 850), new Vector2(800, 110), Color.cyan);

            var infoCard = MakeBox(panel, "InfoCard", new Vector2(0, 150), new Vector2(900, 950), new Color(0.08f, 0.11f, 0.16f, 0.9f));
            var verText  = MakeText(infoCard, "VersionText", "Smart AR Measure v1.0.0\n\nBuilt with Unity 6 & AR Foundation 6.x\nPowered by Google ARCore\n\nFeatures:\n• Real-Time Surface Plane Detection\n• Point-to-Point Distance Meter\n• Continuous Multi-Point Chain & Area\n• Metric (m/cm) & Imperial (ft/in)\n• Material Design 3 Dark Theme UI", 32, FontStyles.Normal, Vector2.zero, new Vector2(820, 850), Color.white);

            so.FindProperty("appNameVersionText").objectReferenceValue = verText.GetComponent<TextMeshProUGUI>();

            var backBtn = MakeButton(panel, "BackButton", "BACK TO HOME", new Vector2(0, -850), new Vector2(720, 130), new Color(0.25f, 0.28f, 0.35f));
            so.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();

            so.ApplyModifiedProperties();
        }

        // ==================== PRIMITIVE UI CONSTRUCTORS ====================
        static GameObject MakeBox(GameObject parent, string name, Vector2 pos, Vector2 size, Color color, bool raycastTarget = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchoredPosition = pos; r.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycastTarget;
            return go;
        }

        static GameObject MakeText(GameObject parent, string name, string text, float size, FontStyles style, Vector2 pos, Vector2 dims, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var r = go.GetComponent<RectTransform>();
            r.anchoredPosition = pos; r.sizeDelta = dims;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return go;
        }

        static GameObject MakeButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = MakeBox(parent, name, pos, size, color, raycastTarget: true);
            go.AddComponent<Button>();
            MakeText(go, "Label", label, 30, FontStyles.Bold, Vector2.zero, size, Color.white);
            return go;
        }

        static GameObject MakeToggle(GameObject parent, string name, string label, Vector2 pos, Vector2 size, bool isOn)
        {
            var bg = MakeBox(parent, name, pos, size, new Color(0.15f, 0.18f, 0.25f), raycastTarget: true);
            var tog = bg.AddComponent<Toggle>();
            var check = MakeBox(bg, "Checkmark", new Vector2(-size.x / 2f + 40f, 0), new Vector2(36, 36), Color.cyan);
            MakeText(bg, "Label", label, 28, FontStyles.Normal, new Vector2(30, 0), new Vector2(size.x - 100f, size.y), Color.white);

            tog.graphic = check.GetComponent<Image>();
            tog.isOn = isOn;
            return bg;
        }

        static GameObject MakeSlider(GameObject parent, string name, Vector2 pos, Vector2 size, float minVal, float maxVal, float curVal)
        {
            var bg = MakeBox(parent, name, pos, size, new Color(0.18f, 0.22f, 0.3f, 1f), raycastTarget: true);
            var slider = bg.AddComponent<Slider>();

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(bg.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.5f);
            faRect.anchorMax = new Vector2(1, 0.5f);
            faRect.offsetMin = new Vector2(18f, -6f);
            faRect.offsetMax = new Vector2(-18f, 6f);
            faRect.sizeDelta = new Vector2(-36f, 12f);

            var fill = MakeBox(fillArea, "Fill", Vector2.zero, Vector2.zero, Color.cyan);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0, 0);
            fRect.anchorMax = new Vector2(0, 1);
            fRect.offsetMin = Vector2.zero;
            fRect.offsetMax = Vector2.zero;
            fRect.sizeDelta = Vector2.zero;

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(bg.transform, false);
            var haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = new Vector2(0, 0);
            haRect.anchorMax = new Vector2(1, 1);
            haRect.offsetMin = new Vector2(18f, 0);
            haRect.offsetMax = new Vector2(-18f, 0);

            var handle = MakeBox(handleArea, "Handle", Vector2.zero, new Vector2(36, 36), Color.white, raycastTarget: true);
            var hRect = handle.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 0.5f);
            hRect.anchorMax = new Vector2(0, 0.5f);
            hRect.sizeDelta = new Vector2(36, 36);

            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;
            slider.value = curVal;
            return bg;
        }

        static void MakeReticleGraphic(GameObject panel)
        {
            var reticleRoot = new GameObject("CenterReticleGraphic", typeof(RectTransform));
            reticleRoot.transform.SetParent(panel.transform, false);
            var r = reticleRoot.GetComponent<RectTransform>();
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(80, 80);

            var outerRing = MakeBox(reticleRoot, "OuterRing", Vector2.zero, new Vector2(60, 60), new Color(0f, 0.9f, 1f, 0.6f));
            var innerDot  = MakeBox(reticleRoot, "InnerDot",  Vector2.zero, new Vector2(18, 18), Color.white);
        }
    }
}
#endif
