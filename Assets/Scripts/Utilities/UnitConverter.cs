using System;
using SmartARMeasure.Models;
using UnityEngine;

namespace SmartARMeasure.Utilities
{
    /// <summary>
    /// Utility class for distance and area unit conversions and localized string formatting.
    /// Uses authoritative SI base units (meters / square meters) and converts mathematically.
    /// </summary>
    public static class UnitConverter
    {
        // Exact Double-Precision Physical Conversion Constants
        public const double METERS_TO_FEET_D = 3.28083989501312;
        public const double METERS_TO_INCHES_D = 39.3700787401575;
        public const double FEET_TO_METERS_D = 0.3048;
        public const double INCHES_TO_METERS_D = 0.0254;

        public const double SQM_TO_SQFT_D = 10.7639104167097;
        public const double SQM_TO_SQIN_D = 1550.0031000062;
        public const double SQFT_TO_SQM_D = 0.09290304;
        public const double SQIN_TO_SQM_D = 0.00064516;

        // Legacy float constants for compatibility
        public const float METERS_TO_CENTIMETERS = 100f;
        public const float METERS_TO_MILLIMETERS = 1000f;
        public const float METERS_TO_INCHES = 39.3700787401575f;
        public const float METERS_TO_FEET = 3.28083989501312f;
        public const float SQM_TO_SQFT = 10.7639104167097f;
        public const float SQM_TO_SQIN = 1550.0031000062f;

        /// <summary>
        /// Formats distance value (given in meters) based on user's active unit settings.
        /// </summary>
        public static string FormatDistance(
            float distanceInMeters,
            MeasurementUnit unitSystem,
            MetricDisplayUnit metricUnit = MetricDisplayUnit.Meters,
            ImperialDisplayUnit imperialUnit = ImperialDisplayUnit.Feet)
        {
            var (val, unit) = FormatValueAndUnit(distanceInMeters, MeasurementMode.Distance, unitSystem, metricUnit, imperialUnit);
            return $"{val} {unit}";
        }

        /// <summary>
        /// Deconstructs measurement into formatted numerical value and unit label.
        /// Guaranteed: The input rawValue in meters/m² is never mutated or re-converted.
        /// </summary>
        public static (string value, string unit) FormatValueAndUnit(
            float rawValueInSI,
            MeasurementMode mode,
            MeasurementUnit unitSystem,
            MetricDisplayUnit metricUnit = MetricDisplayUnit.Meters,
            ImperialDisplayUnit imperialUnit = ImperialDisplayUnit.Feet)
        {
            double raw = (double)rawValueInSI;

            if (unitSystem == MeasurementUnit.Metric)
            {
                if (mode == MeasurementMode.Area)
                {
                    if (metricUnit == MetricDisplayUnit.Meters)
                    {
                        return (raw.ToString("F2"), "m²");
                    }
                    else
                    {
                        double sqCm = raw * 10000.0;
                        string val = (Math.Abs(sqCm - Math.Round(sqCm)) < 0.05)
                            ? Math.Round(sqCm).ToString("F0")
                            : sqCm.ToString("F1");
                        return (val, "cm²");
                    }
                }
                else // Distance, Continuous, Height
                {
                    if (metricUnit == MetricDisplayUnit.Meters)
                    {
                        return (raw.ToString("F2"), "m");
                    }
                    else
                    {
                        double cm = raw * 100.0;
                        string val = (Math.Abs(cm - Math.Round(cm)) < 0.05)
                            ? Math.Round(cm).ToString("F0")
                            : cm.ToString("F1");
                        return (val, "cm");
                    }
                }
            }
            else // Imperial
            {
                if (mode == MeasurementMode.Area)
                {
                    if (imperialUnit == ImperialDisplayUnit.Feet)
                    {
                        double sqFt = raw * SQM_TO_SQFT_D;
                        return (sqFt.ToString("F2"), "ft²");
                    }
                    else
                    {
                        double sqIn = raw * SQM_TO_SQIN_D;
                        string val = (Math.Abs(sqIn - Math.Round(sqIn)) < 0.05)
                            ? Math.Round(sqIn).ToString("F0")
                            : sqIn.ToString("F1");
                        return (val, "in²");
                    }
                }
                else // Distance, Continuous, Height
                {
                    if (imperialUnit == ImperialDisplayUnit.Feet)
                    {
                        double feet = raw * METERS_TO_FEET_D;
                        return (feet.ToString("F2"), "ft");
                    }
                    else
                    {
                        double inches = raw * METERS_TO_INCHES_D;
                        return (inches.ToString("F2"), "in");
                    }
                }
            }
        }

        /// <summary>
        /// Formats area value (given in square meters) based on user's active unit settings.
        /// </summary>
        public static string FormatArea(
            float areaInSqMeters,
            MeasurementUnit unitSystem,
            MetricDisplayUnit metricUnit = MetricDisplayUnit.Meters,
            ImperialDisplayUnit imperialUnit = ImperialDisplayUnit.Feet)
        {
            var (val, unit) = FormatValueAndUnit(areaInSqMeters, MeasurementMode.Area, unitSystem, metricUnit, imperialUnit);
            return $"{val} {unit}";
        }

        /// <summary>
        /// Retrieves short label suffix for unit display.
        /// </summary>
        public static string GetUnitSuffix(
            MeasurementUnit unitSystem,
            MetricDisplayUnit metricUnit = MetricDisplayUnit.Meters,
            ImperialDisplayUnit imperialUnit = ImperialDisplayUnit.Feet)
        {
            if (unitSystem == MeasurementUnit.Metric)
            {
                return metricUnit == MetricDisplayUnit.Meters ? "m" : "cm";
            }
            else
            {
                return imperialUnit == ImperialDisplayUnit.Feet ? "ft" : "in";
            }
        }
    }
}
