using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartARMeasure.UI;
using System.Linq;

public class IntroSceneGenerator
{
    [MenuItem("Tools/Generate Intro Scene")]
    public static void GenerateIntroScene()
    {
        string scenePath = "Assets/Scenes/IntroScene.unity";
        string logoPath = "Assets/Textures/IntroLogo.png";

        // Import settings for the logo
        TextureImporter importer = AssetImporter.GetAtPath(logoPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError($"Logo not found at {logoPath}");
            return;
        }

        Sprite logoSprite = null;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(logoPath);
        foreach (Object obj in assets)
        {
            if (obj is Sprite sprite)
            {
                logoSprite = sprite;
                break;
            }
        }

        if (logoSprite == null)
        {
            Debug.LogError("Failed to load logo as Sprite. Make sure it's a valid image file.");
            return;
        }

        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "IntroScene";

        // Setup Main Camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cameraObj.tag = "MainCamera";

        // Setup Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Background Panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Logo Image
        GameObject logoObj = new GameObject("LogoImage");
        logoObj.transform.SetParent(canvasObj.transform, false);
        Image logoImg = logoObj.AddComponent<Image>();
        logoImg.sprite = logoSprite;
        logoImg.preserveAspect = true;
        
        RectTransform logoRect = logoObj.GetComponent<RectTransform>();
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(logoSprite.texture.width, logoSprite.texture.height);
        
        // Scale down if it's too large, ensuring it fits well within a typical screen width
        float maxWidth = 800f; 
        if (logoRect.sizeDelta.x > maxWidth)
        {
            float ratio = maxWidth / logoRect.sizeDelta.x;
            logoRect.sizeDelta = new Vector2(maxWidth, logoRect.sizeDelta.y * ratio);
        }

        CanvasGroup canvasGroup = logoObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Controller handles fade

        // Intro Controller
        GameObject controllerObj = new GameObject("IntroController");
        IntroController controller = controllerObj.AddComponent<IntroController>();
        
        // Use SerializedObject to assign the private serialized field logoCanvasGroup
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("logoCanvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("targetSceneName").stringValue = "MainScene";
        so.ApplyModifiedProperties();

        // Save Scene
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log("IntroScene created and saved at " + scenePath);

        // Update Build Settings
        EditorBuildSettingsScene[] originalBuildSettings = EditorBuildSettings.scenes;
        var existingIntro = originalBuildSettings.FirstOrDefault(s => s.path == scenePath);
        var existingMain = originalBuildSettings.FirstOrDefault(s => s.path == "Assets/Scenes/MainScene.unity");

        EditorBuildSettingsScene[] newBuildSettings = new EditorBuildSettingsScene[2];
        newBuildSettings[0] = new EditorBuildSettingsScene(scenePath, true);
        newBuildSettings[1] = new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true);

        EditorBuildSettings.scenes = newBuildSettings;
        Debug.Log("Build Settings updated.");
        
        // Reopen Intro Scene just in case
        EditorSceneManager.OpenScene(scenePath);
    }
}
