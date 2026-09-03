using UnityEditor;
using UnityEngine;

public class BuildSetup
{
    [MenuItem("Tools/Prepare and Build APK")]
    public static void PrepareAndBuild()
    {
        // 1. Set Application Identifier
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.alenkr.smartarmeasure");
        
        // 2. Set Keystore
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = "release.keystore";
        PlayerSettings.Android.keystorePass = "SmartAR123";
        PlayerSettings.Android.keyaliasName = "release";
        PlayerSettings.Android.keyaliasPass = "SmartAR123";

        // 3. Set Architecture to ARM64 and Scripting Backend to IL2CPP
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // 4. Set Version
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        Debug.Log("Player Settings Configured for Release!");

        // 5. Build the APK
        string[] scenes = {
            "Assets/Scenes/IntroScene.unity",
            "Assets/Scenes/MainScene.unity"
        };

        System.IO.Directory.CreateDirectory("Builds");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/SmartARMeasure-v1.0.0.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        Debug.Log("Starting Build...");
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Build failed! " + report.summary.totalErrors + " errors.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
