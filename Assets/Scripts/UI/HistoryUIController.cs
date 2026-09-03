using System.Collections.Generic;
using SmartARMeasure.Core;
using SmartARMeasure.Models;
using SmartARMeasure.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SmartARMeasure.UI
{
    /// <summary>
    /// Measurement History screen UI controller.
    /// Populates recorded measurements list, supports item deletion, clipboard copying, native sharing, CSV and PDF document exports.
    /// </summary>
    public class HistoryUIController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button clearAllButton;

        [Header("Export Actions")]
        [SerializeField] private Button exportCsvButton;
        [SerializeField] private Button exportPdfButton;

        [Header("List Container")]
        [SerializeField] private Transform scrollContentContainer;
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private GameObject emptyHistoryStatePanel;

        private void OnEnable()
        {
            RefreshHistoryList();
        }

        private void Start()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    AudioFeedback.PlayClick();
                    HapticFeedback.TriggerLight();
                    AppManager.Instance?.ReturnToHome();
                });
            }

            if (clearAllButton != null)
            {
                clearAllButton.onClick.AddListener(() =>
                {
                    if (MeasurementManager.Instance != null && MeasurementManager.Instance.HistoryRecords.Count > 0)
                    {
                        AudioFeedback.PlayWarning();
                        HapticFeedback.TriggerWarning();
                        MeasurementManager.Instance.ClearAllHistory();
                        RefreshHistoryList();
                        NotificationToastController.Instance?.ShowToast("All history records cleared");
                    }
                });
            }

            if (exportCsvButton != null)
            {
                exportCsvButton.onClick.AddListener(() =>
                {
                    if (MeasurementManager.Instance != null && MeasurementManager.Instance.HistoryRecords.Count > 0)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerMedium();
                        ExportUtility.ExportToCSVSAF(MeasurementManager.Instance.HistoryRecords);
                    }
                    else
                    {
                        NotificationToastController.Instance?.ShowToast("No measurements to export.");
                    }
                });
            }

            if (exportPdfButton != null)
            {
                exportPdfButton.onClick.AddListener(() =>
                {
                    if (MeasurementManager.Instance != null && MeasurementManager.Instance.HistoryRecords.Count > 0)
                    {
                        AudioFeedback.PlayClick();
                        HapticFeedback.TriggerMedium();
                        string path = ExportUtility.ExportToPDF(MeasurementManager.Instance.HistoryRecords);
                        ExportUtility.ShareFileNative(path, "application/pdf", "Share AR Measurements Report");
                        NotificationToastController.Instance?.ShowToast("Report exported & ready to share");
                    }
                    else
                    {
                        NotificationToastController.Instance?.ShowToast("No history entries to export");
                    }
                });
            }
        }

        /// <summary>
        /// Re-populates history entries in the scroll view list.
        /// </summary>
        public void RefreshHistoryList()
        {
            if (scrollContentContainer == null) return;

            // Clear existing list items
            foreach (Transform child in scrollContentContainer)
            {
                Destroy(child.gameObject);
            }

            List<MeasurementRecord> records = MeasurementManager.Instance != null
                ? MeasurementManager.Instance.HistoryRecords
                : new List<MeasurementRecord>();

            if (emptyHistoryStatePanel != null)
            {
                emptyHistoryStatePanel.SetActive(records.Count == 0);
            }

            // Create list items in reverse chronological order
            for (int i = records.Count - 1; i >= 0; i--)
            {
                MeasurementRecord rec = records[i];
                CreateHistoryItemUI(rec);
            }
        }

        private void CreateHistoryItemUI(MeasurementRecord rec)
        {
            GameObject itemObj;
            if (historyItemPrefab != null)
            {
                itemObj = Instantiate(historyItemPrefab, scrollContentContainer);
            }
            else
            {
                itemObj = CreateDefaultHistoryItemObject();
                itemObj.transform.SetParent(scrollContentContainer, false);
            }

            // Setup card text elements
            TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
            string formattedDist = UnitConverter.FormatDistance(rec.totalDistanceMeters, rec.unitUsed);

            if (texts.Length > 0) texts[0].text = $"{rec.title} - {formattedDist}";
            if (texts.Length > 1) texts[1].text = $"{rec.dateString} at {rec.timeString} | Mode: {rec.modeUsed}";

            // Setup buttons
            Button[] buttons = itemObj.GetComponentsInChildren<Button>();
            if (buttons.Length > 0) // Copy Button
            {
                buttons[0].onClick.AddListener(() =>
                {
                    HapticFeedback.TriggerLight();
                    string copyText = $"{rec.title}: {formattedDist} (Recorded on {rec.dateString})";
                    ExportUtility.CopyToClipboard(copyText);
                    NotificationToastController.Instance?.ShowToast("Copied distance to clipboard");
                });
            }

            if (buttons.Length > 1) // Delete Button
            {
                buttons[1].onClick.AddListener(() =>
                {
                    HapticFeedback.TriggerLight();
                    MeasurementManager.Instance?.DeleteHistoryRecord(rec.id);
                    RefreshHistoryList();
                    NotificationToastController.Instance?.ShowToast("Record deleted");
                });
            }
        }

        private GameObject CreateDefaultHistoryItemObject()
        {
            GameObject item = new GameObject("HistoryItemCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            
            Image bg = item.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.9f); // Material 3 Surface Dark
            bg.raycastTarget = false;

            LayoutElement layoutElem = item.GetComponent<LayoutElement>();
            layoutElem.minHeight = 110f;
            layoutElem.preferredHeight = 110f;

            HorizontalLayoutGroup layout = item.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Title & Subtitle text container
            GameObject textCol = new GameObject("TextContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            textCol.transform.SetParent(item.transform, false);

            VerticalLayoutGroup vGroup = textCol.GetComponent<VerticalLayoutGroup>();
            vGroup.spacing = 6;
            vGroup.childControlWidth = true;
            vGroup.childControlHeight = true;

            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0f, 0.9f, 1f); // Cyan

            GameObject subtitleObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subtitleObj.transform.SetParent(textCol.transform, false);
            TextMeshProUGUI subText = subtitleObj.GetComponent<TextMeshProUGUI>();
            subText.fontSize = 14;
            subText.color = new Color(0.75f, 0.8f, 0.85f, 1f);

            return item;
        }
    }
}
