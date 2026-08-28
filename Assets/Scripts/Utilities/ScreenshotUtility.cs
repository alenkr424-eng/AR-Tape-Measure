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

            string folder = Path.Combine(Application.persistentDataPath, "Screenshots");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string filename = $"SmartAR_Capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(folder, filename);
            File.WriteAllBytes(filePath, bytes);

            Debug.Log($"Screenshot captured and saved to: {filePath}");
            HapticFeedback.TriggerMedium();

            OnScreenshotCaptured?.Invoke(filePath);

            // Share natively
            ExportUtility.ShareFileNative(filePath, "image/png", "Share AR Measurement Screenshot");
        }
    }
}
