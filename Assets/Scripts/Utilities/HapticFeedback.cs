using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Provides Android native haptic feedback triggers for tap confirmations, marker placements, and button presses.
    /// </summary>
    public static class HapticFeedback
    {
        private static bool isAndroidPlatform = Application.platform == RuntimePlatform.Android;

        /// <summary>
        /// Triggers a brief light haptic click (e.g. marker placed, button tapped).
        /// </summary>
        public static void TriggerLight()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.HapticFeedback)
                return;

            if (isAndroidPlatform)
            {
                VibrateAndroid(30);
            }
            else
            {
                #if !UNITY_EDITOR
                Handheld.Vibrate();
                #endif
            }
        }

        /// <summary>
        /// Triggers a medium haptic pulse (e.g. line completed, measurement recorded).
        /// </summary>
        public static void TriggerMedium()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.HapticFeedback)
                return;

            if (isAndroidPlatform)
            {
                VibrateAndroid(60);
            }
            else
            {
                #if !UNITY_EDITOR
                Handheld.Vibrate();
                #endif
            }
        }

        /// <summary>
        /// Triggers a double-pulse vibration for warnings or resets.
        /// </summary>
        public static void TriggerWarning()
        {
            if (AppSettings.Instance != null && !AppSettings.Instance.HapticFeedback)
                return;

            if (isAndroidPlatform)
            {
                VibrateAndroidPattern(new long[] { 0, 40, 60, 40 });
            }
            else
            {
                #if !UNITY_EDITOR
                Handheld.Vibrate();
                #endif
            }
        }

        private static void VibrateAndroid(long milliseconds)
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"HapticFeedback Android error: {ex.Message}");
            }
        }

        private static void VibrateAndroidPattern(long[] pattern)
        {
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        vibrator.Call("vibrate", pattern, -1);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"HapticFeedback Pattern error: {ex.Message}");
            }
        }
    }
}
