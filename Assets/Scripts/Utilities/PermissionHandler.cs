using System;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Handles runtime Android permissions for Camera and External Storage.
    /// </summary>
    public class PermissionHandler : MonoBehaviour
    {
        public static PermissionHandler Instance { get; private set; }

        public event Action OnCameraPermissionGranted;
        public event Action OnCameraPermissionDenied;

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

        private void Start()
        {
            RequestCameraPermission();
        }

        /// <summary>
        /// Checks if Camera permission is granted, requesting it if necessary.
        /// </summary>
        public void RequestCameraPermission()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += (permissionName) =>
                {
                    Debug.Log("Camera permission granted.");
                    OnCameraPermissionGranted?.Invoke();
                    if (Core.AppManager.Instance != null && Core.AppManager.Instance.CurrentState == Core.AppScreenState.ARMeasuring)
                    {
                        Core.ARManager.Instance?.EnsureARCameraActive();
                    }
                };
                callbacks.PermissionDenied += (permissionName) =>
                {
                    Debug.LogWarning("Camera permission denied.");
                    OnCameraPermissionDenied?.Invoke();
                };
                callbacks.PermissionDeniedAndDontAskAgain += (permissionName) =>
                {
                    Debug.LogWarning("Camera permission denied with Don't Ask Again.");
                    OnCameraPermissionDenied?.Invoke();
                };

                Permission.RequestUserPermission(Permission.Camera, callbacks);
            }
            else
            {
                OnCameraPermissionGranted?.Invoke();
                if (Core.AppManager.Instance != null && Core.AppManager.Instance.CurrentState == Core.AppScreenState.ARMeasuring)
                {
                    Core.ARManager.Instance?.EnsureARCameraActive();
                }
            }
#else
            OnCameraPermissionGranted?.Invoke();
            if (Core.AppManager.Instance != null && Core.AppManager.Instance.CurrentState == Core.AppScreenState.ARMeasuring)
            {
                Core.ARManager.Instance?.EnsureARCameraActive();
            }
#endif
        }

        /// <summary>
        /// Helper to check if Camera permission is currently authorized.
        /// </summary>
        public bool HasCameraPermission()
        {
#if UNITY_ANDROID
            return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
            return true;
#endif
        }
    }
}
