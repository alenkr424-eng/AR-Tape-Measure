using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Captures AR view screenshots, saves PNG files to gallery storage, and invokes native share.
    /// </summary>
    public class ScreenshotUtility : MonoBehaviour
    {
        public static ScreenshotUtility Instance { get; private set; }

        public event Action<string> OnScreenshotCaptured;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Captures current frame screenshot after waiting for end of frame.
        /// </summary>
        public void CaptureScreenshot(Canvas uiCanvasToHide = null)
        {
            StartCoroutine(CaptureRoutine(uiCanvasToHide));
        }

        private IEnumerator CaptureRoutine(Canvas uiCanvasToHide)
        {
            // Hide UI canvas briefly if specified
            if (uiCanvasToHide != null)
            {
                uiCanvasToHide.enabled = false;
            }

            yield return new WaitForEndOfFrame();

            int width = Screen.width;
            int height = Screen.height;
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            // Restore UI canvas
            if (uiCanvasToHide != null)
            {
                uiCanvasToHide.enabled = true;
            }

            // Save image
            byte[] bytes = UnityEngine.ImageConversion.EncodeToPNG(screenshot);
            Destroy(screenshot);

            bool success = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
                using (AndroidJavaObject externalStorageDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", environment.GetStatic<string>("DIRECTORY_PICTURES")))
                {
                    string picturesPath = externalStorageDir.Call<string>("getAbsolutePath");
                    string appDir = Path.Combine(picturesPath, "SmartARMeasure");
                    if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
                    
                    string filename = $"SmartAR_Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string filePath = Path.Combine(appDir, filename);
                    File.WriteAllBytes(filePath, bytes);

                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (AndroidJavaClass mediaScanner = new AndroidJavaClass("android.media.MediaScannerConnection"))
                    {
                        mediaScanner.CallStatic("scanFile", currentActivity, new string[] { filePath }, new string[] { "image/png" }, null);
                    }
                    
                    Debug.Log($"Screenshot successfully saved to gallery at: {filePath}");
                    success = true;
                    OnScreenshotCaptured?.Invoke(filePath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save to gallery: " + e.Message);
            }
#else
            // Fallback for Editor / iOS (if applicable)
            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "Screenshots");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string filename = $"SmartAR_Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(folder, filename);
                File.WriteAllBytes(filePath, bytes);

                Debug.Log($"Screenshot captured and saved to internal storage: {filePath}");
                success = true;
                OnScreenshotCaptured?.Invoke(filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save internally: " + e.Message);
            }
#endif

            HapticFeedback.TriggerMedium();

            if (success)
            {
                SmartARMeasure.UI.NotificationToastController.Instance?.ShowToast("Saved!", 5.0f);
            }
            else
            {
                SmartARMeasure.UI.NotificationToastController.Instance?.ShowToast("Save failed", 5.0f);
            }
        }
    }
}
