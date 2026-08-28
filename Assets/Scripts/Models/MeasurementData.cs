using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartARMeasure.Models
{
    /// <summary>
    /// Supported unit systems in the application.
    /// </summary>
    public enum MeasurementUnit
    {
        Metric,   // Centimeters (cm) and Meters (m)
        Imperial  // Inches (in) and Feet (ft)
    }

    /// <summary>
    /// Active metric unit display resolution (toggled directly via HUD unit label).
    /// </summary>
    public enum MetricDisplayUnit
    {
        Meters,
        Centimeters
    }

    /// <summary>
    /// Active imperial unit display resolution (toggled directly via HUD unit label).
    /// </summary>
    public enum ImperialDisplayUnit
    {
        Feet,
        Inches
    }

    /// <summary>
    /// Measuring modes available in the application.
    /// </summary>
    public enum MeasurementMode
    {
        Distance,   // Point-to-Point distance measuring
        Continuous, // Connected polyline measuring
        Area,       // Surface area calculation (Future ready)
        Height      // Vertical height measurement (Future ready)
    }

    /// <summary>
    /// Represents a single 3D marker coordinate placed in world space.
    /// </summary>
    [Serializable]
    public class PointMarkerData
    {
        public float x;
        public float y;
        public float z;

        public PointMarkerData() { }

        public PointMarkerData(Vector3 position)
        {
            x = position.x;
            y = position.y;
            z = position.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Represents a single line segment between two markers.
    /// </summary>
    [Serializable]
    public class MeasurementSegment
    {
        public PointMarkerData startPoint;
        public PointMarkerData endPoint;
        public float distanceMeters;

        public MeasurementSegment() { }

        public MeasurementSegment(Vector3 start, Vector3 end)
        {
            startPoint = new PointMarkerData(start);
            endPoint = new PointMarkerData(end);
            distanceMeters = Vector3.Distance(start, end);
        }
    }

    /// <summary>
    /// Complete saved measurement entry recorded in history.
    /// </summary>
    [Serializable]
    public class MeasurementRecord
    {
        public string id;
        public string title;
        public string dateString;
        public string timeString;
        public float totalDistanceMeters;
        public MeasurementUnit unitUsed;
        public MeasurementMode modeUsed;
        public List<MeasurementSegment> segments = new List<MeasurementSegment>();

        public MeasurementRecord()
        {
            id = Guid.NewGuid().ToString();
            DateTime now = DateTime.Now;
            dateString = now.ToString("yyyy-MM-dd");
            timeString = now.ToString("HH:mm:ss");
        }
    }

    /// <summary>
    /// Serializable list wrapper for PlayerPrefs persistence.
    /// </summary>
    [Serializable]
    public class MeasurementHistoryWrapper
    {
        public List<MeasurementRecord> records = new List<MeasurementRecord>();
    }
}
