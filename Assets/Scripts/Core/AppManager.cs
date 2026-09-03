using System;
using SmartARMeasure.UI;
using SmartARMeasure.Utilities;
using UnityEngine;

namespace SmartARMeasure.Core
{
    public enum AppScreenState
    {
        Home,
        ARMeasuring,
        History,
        Settings,
        About
    }

    /// <summary>
    /// Master application coordinator.
    /// Manages top-level navigation states, screen transitions, and initialization sequence.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        private AppScreenState currentState = AppScreenState.Home;
        public AppScreenState CurrentState => currentState;

        public event Action<AppScreenState> OnScreenStateChanged;

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
                return;
            }

            // Set screen orientation to portrait for mobile AR experience
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            NavigateToScreen(AppScreenState.Home);
        }

        /// <summary>
        /// Navigates to target screen state with smooth canvas cross-fade transition.
        /// </summary>
        public void NavigateToScreen(AppScreenState targetState)
        {
            if (targetState == AppScreenState.ARMeasuring)
            {
                // Ensure camera permissions before entering AR mode
                if (PermissionHandler.Instance != null && !PermissionHandler.Instance.HasCameraPermission())
                {
                    PermissionHandler.Instance.RequestCameraPermission();
                }
                else
                {
                    // Explicitly activate AR Camera, AR Camera Background, and AR Session
                    if (ARManager.Instance != null)
                    {
                        ARManager.Instance.EnsureARCameraActive();
                    }
                }

                Debug.Log("Entering AR Measurement Mode - Live Camera & AR Session Active");
            }
            else
            {
                // Hide AR reticle when leaving ARMeasuring screen
                if (ARManager.Instance != null)
                {
                    // ARManager.Instance.SetReticleVisible(false);
                }
            }

            currentState = targetState;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(targetState);
            }

            OnScreenStateChanged?.Invoke(currentState);
        }

        public void StartMeasuring()
        {
            NavigateToScreen(AppScreenState.ARMeasuring);
        }

        public void OpenHistory()
        {
            NavigateToScreen(AppScreenState.History);
        }

        public void OpenSettings()
        {
            NavigateToScreen(AppScreenState.Settings);
        }

        public void OpenAbout()
        {
            NavigateToScreen(AppScreenState.About);
        }

        public void ReturnToHome()
        {
            NavigateToScreen(AppScreenState.Home);
        }

        public void QuitApplication()
        {
            Debug.Log("Exiting Smart AR Measure application...");
            Application.Quit();
        }
    }
}
