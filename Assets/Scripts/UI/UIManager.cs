using System.Collections;
using System.Collections.Generic;
using SmartARMeasure.Core;
using UnityEngine;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// UIManager coordinates CanvasGroup fade transitions between screens and enforces Material Design 3 UI layer depth.
    /// Includes automatic fallback panel discovery to guarantee UI visibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels (CanvasGroups)")]
        [SerializeField] private CanvasGroup homePanelGroup;
        [SerializeField] private CanvasGroup arOverlayPanelGroup;
        [SerializeField] private CanvasGroup settingsPanelGroup;
        [SerializeField] private CanvasGroup historyPanelGroup;
        [SerializeField] private CanvasGroup aboutPanelGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.2f;

        private Dictionary<AppScreenState, CanvasGroup> screenDictionary;
        private CanvasGroup currentActiveGroup;
        private Coroutine activeFadeCoroutine;

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

            AutoDiscoverPanels();
            InitializeScreenMap();
        }

        private void AutoDiscoverPanels()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform canvasTransform = canvas.transform;

            if (homePanelGroup == null) homePanelGroup = FindCanvasGroup(canvasTransform, "HomePanel");
            if (arOverlayPanelGroup == null) arOverlayPanelGroup = FindCanvasGroup(canvasTransform, "AROverlayPanel");
            if (settingsPanelGroup == null) settingsPanelGroup = FindCanvasGroup(canvasTransform, "SettingsPanel");
            if (historyPanelGroup == null) historyPanelGroup = FindCanvasGroup(canvasTransform, "HistoryPanel");
            if (aboutPanelGroup == null) aboutPanelGroup = FindCanvasGroup(canvasTransform, "AboutPanel");
        }

        private CanvasGroup FindCanvasGroup(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null)
            {
                CanvasGroup cg = t.GetComponent<CanvasGroup>();
                if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
                return cg;
            }
            return null;
        }

        private void InitializeScreenMap()
        {
            screenDictionary = new Dictionary<AppScreenState, CanvasGroup>
            {
                { AppScreenState.Home, homePanelGroup },
                { AppScreenState.ARMeasuring, arOverlayPanelGroup },
                { AppScreenState.Settings, settingsPanelGroup },
                { AppScreenState.History, historyPanelGroup },
                { AppScreenState.About, aboutPanelGroup }
            };

            // Initialize all panels to hidden except Home
            foreach (var kvp in screenDictionary)
            {
                if (kvp.Value != null)
                {
                    bool isHome = (kvp.Key == AppScreenState.Home);
                    SetGroupState(kvp.Value, isHome ? 1f : 0f, isHome);
                    if (isHome) currentActiveGroup = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Transitions to target screen canvas group with smooth alpha fade.
        /// </summary>
        public void ShowScreen(AppScreenState targetState)
        {
            if (screenDictionary == null) InitializeScreenMap();

            if (activeFadeCoroutine != null)
            {
                StopCoroutine(activeFadeCoroutine);
            }

            activeFadeCoroutine = StartCoroutine(TransitionScreenRoutine(targetState));
        }

        private IEnumerator TransitionScreenRoutine(AppScreenState targetState)
        {
            CanvasGroup targetGroup = screenDictionary.ContainsKey(targetState) ? screenDictionary[targetState] : null;

            // Fade out current screen
            if (currentActiveGroup != null && currentActiveGroup != targetGroup)
            {
                float elapsed = 0f;
                float startAlpha = currentActiveGroup.alpha;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    currentActiveGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                    yield return null;
                }

                SetGroupState(currentActiveGroup, 0f, false);
            }

            // Fade in target screen
            if (targetGroup != null)
            {
                targetGroup.gameObject.SetActive(true);
                targetGroup.interactable = true;
                targetGroup.blocksRaycasts = true;
                targetGroup.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    targetGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }

                SetGroupState(targetGroup, 1f, true);
                currentActiveGroup = targetGroup;
            }
        }

        private void SetGroupState(CanvasGroup group, float alpha, bool interactable)
        {
            if (group == null) return;
            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
            group.gameObject.SetActive(interactable || alpha > 0f);
        }
    }
}
