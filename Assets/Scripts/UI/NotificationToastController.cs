using System.Collections;
using TMPro;
using UnityEngine;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// Displays Material Design toast popups and status messages across all screens.
    /// </summary>
    public class NotificationToastController : MonoBehaviour
    {
        public static NotificationToastController Instance { get; private set; }

        [Header("Toast Components")]
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.3f;

        private Coroutine activeToastCoroutine;

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

            if (toastCanvasGroup != null)
            {
                toastCanvasGroup.alpha = 0f;
                toastCanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Shows a temporary toast message banner.
        /// </summary>
        public void ShowToast(string message)
        {
            if (activeToastCoroutine != null)
            {
                StopCoroutine(activeToastCoroutine);
            }

            activeToastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            if (toastText != null)
            {
                toastText.text = message;
            }

            if (toastCanvasGroup != null)
            {
                // Fade in
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }
                toastCanvasGroup.alpha = 1f;

                // Wait display duration
                yield return new WaitForSeconds(displayDuration);

                // Fade out
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                toastCanvasGroup.alpha = 0f;
            }
        }
    }
}
