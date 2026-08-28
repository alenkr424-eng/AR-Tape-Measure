using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Export utility for generating CSV reports, formatted PDF/TXT measurement summaries, and sharing via Android native intents.
    /// </summary>
    public static class ExportUtility
    {
        private const string EXPORT_DIR_NAME = "SmartARExports";

        /// <summary>
        /// Exports all measurement records to a CSV file.
        /// Returns absolute file path.
        /// </summary>
        public static string ExportToCSV(List<MeasurementRecord> records)
        {
            string exportFolder = GetExportDirectory();
            string fileName = $"SmartAR_Measure_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(exportFolder, fileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID,Title,Date,Time,Distance (Meters),Formatted Distance,Unit System,Mode,Points Count");

            foreach (var record in records)
            {
                string formatted = UnitConverter.FormatDistance(record.totalDistanceMeters, record.unitUsed);
                int pointsCount = record.segments != null ? record.segments.Count + 1 : 0;
                
                sb.AppendLine($"\"{record.id}\",\"{record.title}\",\"{record.dateString}\",\"{record.timeString}\",{record.totalDistanceMeters:F4},\"{formatted}\",\"{record.unitUsed}\",\"{record.modeUsed}\",{pointsCount}");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Exported CSV to: {filePath}");
            return filePath;
        }

        /// <summary>
        /// Exports measurement records to a formatted PDF/Text summary document.
        /// Returns absolute file path.
        /// </summary>
        public static string ExportToPDF(List<MeasurementRecord> records)
        {
            string exportFolder = GetExportDirectory();
            string fileName = $"SmartAR_Measure_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(exportFolder, fileName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=================================================");
            sb.AppendLine("              SMART AR MEASURE REPORT            ");
            sb.AppendLine("=================================================");
            sb.AppendLine($"Generated Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Recorded Measurements: {records.Count}");
            sb.AppendLine("-------------------------------------------------\n");

            int index = 1;
            foreach (var record in records)
            {
                string formatted = UnitConverter.FormatDistance(record.totalDistanceMeters, record.unitUsed);
                sb.AppendLine($"[{index}] {record.title}");
                sb.AppendLine($"    Date & Time : {record.dateString} at {record.timeString}");
                sb.AppendLine($"    Distance    : {formatted} ({record.totalDistanceMeters:F4} m)");
                sb.AppendLine($"    Mode        : {record.modeUsed}");
                sb.AppendLine($"    Unit System : {record.unitUsed}");

                if (record.segments != null && record.segments.Count > 0)
                {
                    sb.AppendLine("    Segment Details:");
                    for (int s = 0; s < record.segments.Count; s++)
                    {
                        var seg = record.segments[s];
                        string segFormatted = UnitConverter.FormatDistance(seg.distanceMeters, record.unitUsed);
                        sb.AppendLine($"      - Segment #{s + 1}: {segFormatted}");
                    }
                }
                sb.AppendLine("-------------------------------------------------");
                index++;
            }

            sb.AppendLine("\nExported via Smart AR Measure for Android.");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Exported PDF/TXT summary to: {filePath}");
            return filePath;
        }

        /// <summary>
        /// Shares a file via Android Native Intent chooser.
        /// </summary>
        public static void ShareFileNative(string filePath, string mimeType = "*/*", string title = "Share Measurement Export")
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File does not exist at path: {filePath}");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
                using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
                {
                    intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intentObject.Call<AndroidJavaObject>("setType", mimeType);

                    using (AndroidJavaClass fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider"))
                    using (AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        string packageName = currentActivity.Call<string>("getPackageName");
                        string authority = packageName + ".fileprovider";

                        using (AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", filePath))
                        using (AndroidJavaObject uriObject = fileProviderClass.CallStatic<AndroidJavaObject>("getUriForFile", currentActivity, authority, fileObject))
                        {
                            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);
                            intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));

                            using (AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, title))
                            {
                                currentActivity.Call("startActivity", chooser);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Native Share Exception: {ex.Message}");
            }
#else
            Debug.Log($"[Editor/Mock Share] Shared File: {filePath}");
#endif
        }

        /// <summary>
        /// Copies plain text content to user clipboard.
        /// </summary>
        public static void CopyToClipboard(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("Copied text to system clipboard.");
        }

        private static string GetExportDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, EXPORT_DIR_NAME);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }
}
