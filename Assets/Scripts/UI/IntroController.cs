using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// Handles the custom startup intro screen animation and scene transition.
    /// Fades the provided logo in, holds it, fades it out, and loads the main AR scene.
    /// </summary>
    public class IntroController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup logoCanvasGroup;
        
        [Header("Timing Configuration")]
        [SerializeField] private float initialDelay = 0.2f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Header("Target Scene")]
        [SerializeField] private string targetSceneName = "MainScene";

        private void Start()
        {
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = 0f;
                StartCoroutine(IntroSequenceRoutine());
            }
            else
            {
                Debug.LogWarning("[IntroController] Logo CanvasGroup not assigned. Skipping animation.");
                LoadNextScene();
            }
        }

        private IEnumerator IntroSequenceRoutine()
        {
            // Initial delay (0.0s to 0.2s)
            yield return new WaitForSeconds(initialDelay);
            
            // Fade In (0.2s to 0.7s)
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                logoCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            logoCanvasGroup.alpha = 1f;
            
            // Hold (0.7s to 1.7s)
            yield return new WaitForSeconds(holdDuration);
            
            // Fade Out (1.7s to 2.2s)
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                logoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            logoCanvasGroup.alpha = 0f;
            
            // Transition to AR Scene
            LoadNextScene();
        }

        private void LoadNextScene()
        {
            // Ensuring the AR Scene is loaded cleanly without any Intro objects lingering
            Debug.Log($"[IntroController] Intro finished. Loading {targetSceneName}...");
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }
    }
}
