#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SmartARMeasure.EditorTools
{
    public static class BuildScript
    {
        [MenuItem("Tools/Smart AR Measure/Build Android APK")]
        public static void BuildAndroidAPK()
        {
            string outputDir = "Buildss";
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string apkPath = Path.Combine(outputDir, "SmartARMeasurev1.apk");
            string[] scenes = new string[] { "Assets/Scenes/MainScene.unity" };

            Debug.Log($"[BuildScript] Starting Android build targeting: {apkPath}");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Android Build SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s at {apkPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[BuildScript] Android Build FAILED with {summary.totalErrors} errors.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
#endif
