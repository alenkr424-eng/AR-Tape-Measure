# AR Tape Measure (SmartARMeasure)

A high-precision, professional Augmented Reality measurement application for Android, built with **Unity 6**, **AR Foundation 6.x**, and **Google ARCore**.

---

## Features

- 📏 **Real-Time 3D Distance Measurement**: Point-to-point Euclidean measurement with high stability and millimeter accuracy.
- 📐 **3D Surface Area Measurement**: Calculate surface areas with Newell's 3D Stokes polygon integration.
- 🏢 **Vertical Height Measurement**: Rapid two-point baseline-to-ceiling elevation tracking.
- 🔄 **Unit Systems**: Instant live switching between **Metric** (`m`, `cm`, `mm`, `m²`, `cm²`) and **Imperial** (`ft`, `in`, `ft²`, `in²`) with double-precision conversion.
- 🎨 **Material Design 3 Theming**: Curated AR accent palette (`Cyan`, `Magenta`, `Amber`, `Emerald Green`) dynamically tints 3D markers, line renderers, mode highlights, and HUD UI elements.
- 〰️ **Configurable Line Thickness**: Real-time adjustable world-space line width slider (`1.0 mm` to `8.0 mm`).
- 🌐 **Non-Destructive Plane Visualization**: Toggle surface mesh rendering on/off without interrupting AR raycasts.
- 🔊 **Audio & Haptic Feedback**: Procedural audio engine and Android native haptic triggers on marker placement, mode selection, and reset actions.
- 📜 **Measurement History & Export**: Save measurements, view history log, and export directly to **CSV** and **PDF** with native Android sharing.
- 📸 **In-App AR Screenshot Capture**: Capture clean high-resolution AR measurement snapshots.

---

## Tech Stack & Architecture

- **Engine**: Unity 6 (6000.5.6f1)
- **AR Framework**: AR Foundation 6.x + Google ARCore XR Plugin
- **Render Pipeline**: Universal Render Pipeline (URP)
- **UI Framework**: Unity UI + TextMesh Pro (Material Design 3 Dark Theme)
- **Language**: C# 9.0 (.NET Standard 2.1)

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # AppManager, ARManager, MeasurementManager
│   ├── Models/         # AppSettings, MeasurementData
│   ├── UI/             # Overlay UI, Settings, History, Home, About, Toast
│   ├── Visuals/        # MarkerObject, MeasurementLine, FloatingDistanceLabel, PlaneVisualizer
│   └── Utilities/      # AudioFeedback, HapticFeedback, UnitConverter, ExportUtility, PermissionHandler
├── Shaders/            # MeasurementOverlay custom anti-aliased shaders
├── TextMesh Pro/       # High-fidelity typography & SDF font assets
└── XR/                 # ARCore loader & XR Simulation configurations
Packages/               # Package manifest and dependency locks
ProjectSettings/        # Quality, Graphics, URP, Tag, and Input settings
```

---

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/alenkr424-eng/AR-Tape-Measure.git
   ```
2. Open with **Unity 6 (6000.5.x or newer)**.
3. Switch build target to **Android** (API Level 29+ recommended with ARCore support).
4. Open scene `Assets/MainScene.unity` or build and deploy APK to an ARCore-supported Android device.
